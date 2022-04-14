from __future__ import annotations
from abc import ABC, abstractmethod
from ast import Str
from typing import TYPE_CHECKING, Optional, Any
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time

class Command(ABC):
    '''
    Undoable changes
    '''
    def __init__(self,space :Optional[Space] = None):
        '''
        history_in: Where to push the command in history
            'self' - the object's history
            'parent' - the object's parent's history
            'none' - no history
        '''
        self.done = False
        self.space = space

        self.history_obj : str = "none"

    @abstractmethod
    def execute(self, push = True):
        self.done = True
        if self.space != None and push and self.history_obj != "none":
            self.space.command_manager.push(self)

    @abstractmethod
    def redo(self):
        self.done = True

    @abstractmethod
    def undo(self):
        self.done = False

class CommandSequence(Command):
    
    def __init__(self, commands:list[Command]):
        super().__init__()
        self.commands = commands
        space = commands[0].space
        self.history_obj = get_co_ancestor([space[c.history_obj] for c in self.commands]).id
        self.done = True
        
    def execute(self):
        super().execute()
        for command in self.commands:
            command.execute()
        self.done = True
    
    def redo(self):
        super().redo()
        for command in self.commands:
            command.redo()
            
        self.done = True

    def undo(self):
        super().undo()
        for command in reversed(self.commands):
            command.undo()
        self.done = False

    def __str__(self):
        res = "Sequence: " 
        for command in self.commands:
            res += f'\n\t\t{str(command)}'
        return res

class CommandHead(Command):
    '''
    A null Command that represents the first HistoryItem in the History linked list.
    '''
    def __init__(self,space = None):
        super(CommandHead,self).__init__(space)
        self.done = True
    def execute(self):
        raise NotImplementedError()

    def redo(self):
        raise NotImplementedError()

    def undo(self):
        raise NotImplementedError()

class CommandCreate(Command):

    def __init__(self,space:Space,d:dict[str,Any],parent:str):
        super().__init__(space)
        self.space = space
        self.d = d
        self.parent = parent

        self.history_obj = self.space[parent].id

    def execute(self):
        super().execute()
        new_instance = self.space.create(self.d, is_new = True, parent=self.parent)
        self.d = new_instance.serialize()

    def redo(self):
        super().redo()
        self.space.create(self.d, is_new = False,parent=self.parent)
        self.d = self.space[self.d['id']].serialize()

    def undo(self):
        ''' 
        serialize the object before destroying for redo.
        '''
        super().undo()
        self.d = self.space[self.d['id']].serialize()
        self.space.destroy(self.d['id'])

    def __str__(self):
        return f"Create {self.d['id']}"

class CommandDestroy(Command):
    
    def __init__(self,space:Space,id:str):
        super().__init__(space)
        self.id = id
        assert self.space != None

        self.history_obj = self.space[self.id].parent_id.value

    def execute(self):
        super().execute()
        self.parent = self.space[self.id].parent_id.value
        self.d = self.space[self.id].serialize()
        self.space.destroy(self.id)

    def redo(self):
        super().redo()
        self.parent = self.space[self.id].parent_id.value
        self.d = self.space[self.id].serialize()
        self.space.destroy(self.id)

    def undo(self):
        super().undo()
        self.space.create(self.d, is_new = False, parent=self.parent)

    def __str__(self):
        return f"Destroy {self.d['id']}"

class CommandAttribute(Command):

    def __init__(self,space:Space,obj:str,name:str,new_value,history_obj:Optional[str] = None):
        super().__init__()
        self.name = name            
        self.new_value = new_value
        self.space = space
        self.obj = obj
        self.time = time.time()

        self.history_obj = history_obj if history_obj != None else self.obj

    def execute(self):
        super().execute()
        self.old_value = self.space[self.obj].attributes[self.name].value
        self.space[self.obj].attributes[self.name].set(self.new_value)

    def redo(self):
        super().redo()
        self.space[self.obj].attributes[self.name].set(self.new_value)

    def undo(self):
        super().undo()
        self.space[self.obj].attributes[self.name].set(self.old_value)

    def __str__(self):
        return f"Attribute {self.obj} {self.name} {self.old_value} {self.new_value}"

class CommandManager():

    def __init__(self,space:Space):
        self.space = space
        self.collected_commands : list[Command] = []

    def push(self,command:Command):
        self.collected_commands.append(command)

    def flush(self):
        if len(self.collected_commands) == 0:
            return
        if len(self.collected_commands) == 1:
            command = self.collected_commands[0]
        else:
            command = CommandSequence(self.collected_commands)

        storage_obj = self.space[command.history_obj]

        # don't repeat atr history within 2 seconds 
        last_command = storage_obj.history.current.command
        if isinstance(command ,CommandAttribute) and isinstance(last_command,CommandAttribute) and command.obj == last_command.obj and command.name == last_command.name:
            if (command.time - last_command.time)<2:
                last_command.new_value = command.new_value
                self.collected_commands = []
                return

        while 1:
            if storage_obj.catches_command:
                storage_obj.history.push(command)
            if not storage_obj.forwards_command:
                break
            if storage_obj.parent_id.value == None:
                break
            storage_obj = self.space[storage_obj.parent_id.value]

        self.collected_commands = []

class HistoryItem:
    '''
    Works as a linked list.
    '''
    def __init__(self,command:Command,last=None):
        self.command = command
        self.last : Optional[HistoryItem] = last
        self.next : Optional[HistoryItem] = None

        if last is not None:
            last.next = self
        
        self.time=time.time()

class History:

    def __init__(self,object:Object):
        self.space = object.space
        self.current : HistoryItem = HistoryItem(CommandHead())

    def push(self,command : Command):
        self.current=HistoryItem(command,self.current)

    def undo(self):
        if self.current.last == None:
            return 0

        while not self.current.command.done:
            # Skip backward until find a command that done == True (not undone).
            self.current=self.current.last
            if self.current.last == None:
                return 0
        
        # Perform the undo
        self.current.command.undo()

        self.current.command.done = False
        self.current=self.current.last
        return 1

    def redo(self):
        if self.current.next == None:
            return 0

        self.current=self.current.next
        while self.current.command.done:
            # Skip forward until find a command that done == False (not redone).
            if self.current.next == None:
                return 0
            self.current=self.current.next
        
        # Perform the redo
        self.current.command.redo()

        self.current.command.done = True
        return 1

    def __str__(self):
        s =  "  ===>\t"+str(self.current.command) +"\n"
        item = self.current.last
        while item != None:
            s = "\t"+str(item.command) + "\n" + s
            item = item.last
        item = self.current.next
        while item != None:
            s += "\t"+str(item.command) + "\n"
            item = item.next
        return f"History:\n" +  s

def get_co_ancestor(objs) -> Object:
    '''
    return the lowest common ancestor of multiple objects
    '''
    space = objs[0].space
    min_len = 1000000000000
    parent_lists = []
    for o in objs:
        parent_list = []
        parent_list.append(o)
        while o.id != "0":
            o = o.parent
            parent_list.append(o)
        parent_lists.append(list(reversed(parent_list)))
        min_len = min(len(parent_list), min_len)

    last = space.root_obj
    for i in range(min_len):
        current = parent_lists[0][i]
        for j in range(1,len(parent_lists)):
            if current != j:
                return last
        last = current

    return last