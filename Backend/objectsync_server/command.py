from __future__ import annotations
from abc import ABC, abstractmethod
from ast import Str
from typing import TYPE_CHECKING, Optional, Any
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time
from space import get_co_ancestor


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
        self.history_obj = get_co_ancestor([c.history_obj for c in self.commands]).id
        
    def execute(self):
        super().execute()
        for command in self.commands:
            command.execute()
    
    def redo(self):
        super().redo()
        for command in self.commands:
            command.redo()

    def undo(self):
        super().undo()
        for command in reversed(self.commands):
            command.undo()

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
        self.space.create(self.d, is_new = True, parent=self.parent)

    def redo(self):
        self.space.create(self.d, is_new = False, parent=self.parent)

    def undo(self):
        ''' 
        serialize the object before destroying for redo.
        '''
        self.d = self.space[self.d['id']].serialize()
        self.space.destroy(self.d['id'])

class CommandDestroy(Command):
    
    def __init__(self,space:Space,id:str):
        super().__init__(space)
        self.id = id
        assert self.space != None

        self.history_obj = self.space[self.id].id

    def execute(self):
        self.parent = self.space[self.id].parent_id.value
        self.d = self.space[self.id].serialize()
        self.space.destroy(self.id)

    def redo(self):
        self.parent = self.space[self.id].parent_id.value
        self.d = self.space[self.id].serialize()
        self.space.destroy(self.id)

    def undo(self):
        self.space.create(self.space[self.id].serialize(), is_new = False, parent=self.parent)

class CommandAttribute(Command):

    def __init__(self,space:Space,obj:str,name:str,value,history_obj:Optional[str] = None):
        super().__init__()
        self.name = name                                                                                      
        self.value = value
        self.space = space
        self.obj = obj
        self.time = time.time()

        self.history_obj = history_obj if history_obj != None else self.obj

    def execute(self):
        super().execute()
        self.space[self.obj].attributes[self.name].set(self.value)

    def redo(self):
        super().redo()
        self.space[self.obj].attributes[self.name].set(self.value)

    def undo(self):
        super().undo()
        self.space[self.obj].attributes[self.name].set(self.value)

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

        if command.history_obj == "parent":
            storage_obj = self.space[command.obj].parent_id.value
        elif command.history_obj == "self":
            storage_obj = command.obj
        else:
            storage_obj = self.space[command.history_obj]

        # don't repeat atr history within 2 seconds 
        last_command = storage_obj.history.current.command
        if isinstance(command ,CommandAttribute) and isinstance(last_command,CommandAttribute) and command.obj == last_command.obj and command.name == last_command.name:
            if (command.time - last_command.time)<2:
                last_command.value = command.value
                return

        while 1:
            if not storage_obj.catches_command:
                continue
            storage_obj.history.push(command)
            if not storage_obj.forwards_command:
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

 
