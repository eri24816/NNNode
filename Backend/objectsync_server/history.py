from __future__ import annotations
from abc import ABC, abstractmethod
from tkinter import E, N
from typing import TYPE_CHECKING, Union, Any
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time

def get_co_ancestor(self,objs) -> Object:
    '''
    return the nearest common ancestor of multiple objects
    '''
    min_len = 10000000000000000
    parent_lists = []
    for o in objs:
        parent_list = []
        parent_list.append(o)
        while o.id != 0:
            o = self.objs[o.parent_id.value]
            parent_list.append(o)
        parent_lists.append(reversed(parent_list))
        min_len = min(len(parent_list), min_len)

    last = self.base_obj
    for i in range(min_len):
        current = parent_lists[0][i]
        for j in range(1,len(parent_lists)):
            if current != j:
                return last
        last = current

    return last

class Command(ABC):
    '''
    If a action from the user is expected able to undo
    '''
    def __init__(self,space = None, history_in =):
        self.done = True
        self.space : Union[Space,None] = space

    @abstractmethod
    def execute(self):
        self.done = True
        if self.space != None:
            self.space.command_manager.push(self)

    @abstractmethod
    def redo(self):
        self.done = True

    @abstractmethod
    def undo(self):
        self.done = False

    def get_history_obj(self):
        raise NotImplementedError()

class CommandSequence(Command):
    
    def __init__(self, commands:list[Command]):
        super().__init__()
        self.commands = commands
        
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

    def get_history_obj(self):
        return get_co_ancestor([c.get_history_obj() for c in self.commands])

class CommandHead(Command):
    '''
    A null Command that represents the first HistoryItem in the History linked list.
    '''
    def execute(self):
        raise NotImplementedError()

    def redo(self):
        raise NotImplementedError()

    def undo(self):
        raise NotImplementedError()

class CommandCreate(Command):

    def __init__(self,space:Space,d:dict[str,Any],parent:str):
        self.space = space
        self.d = d
        self.parent = parent

    def execute(self):
        self.space.create(self.d, is_new = True, parent=self.parent)

    def redo(self):
        self.space.create(self.d, is_new = False, parent=self.parent)

    def undo(self):
        ''' 
        serialize the object before destroying for redo.
        '''
        self.d = self.space.objs[self.d['id']].serialize()
        self.space.destroy(self.d['id'])

    def get_history_obj(self):
        return self.space.objs[self.parent]

class CommandDestroy(Command):
    
        def __init__(self,space:Space,id:int):
            self.space = space
            self.id = id
            
    
        def execute(self):
            self.parent = self.space.objs[self.id].parent_id.value
            self.d = self.space.objs[self.id].serialize()
            self.space.destroy(self.id)
    
        def redo(self):
            self.space.create(self.d, is_new = False, parent=self.parent)
    
        def undo(self):
            self.space.create(self.space.objs[self.id].serialize(), is_new = False, parent=self.space.objs[self.id].parent_id.value)
    
        def get_history_obj(self):
            return self.space.objs[self.id]

class CommandAttribute(Command):

    def __init__(self,space:Space,obj:str,name:str,value):
        super().__init__()
        self.name = name                                                                                      
        self.value = value
        self.space = space
        self.obj = obj

    def execute(self):
        super().execute()
        self.space.objs[self.obj].attributes[self.name].set(self.value)

    def redo(self):
        super().redo()
        self.space.objs[self.obj].attributes[self.name].set(self.value)

    def undo(self):
        super().undo()
        self.space.objs[self.obj].attributes[self.name].set(self.value)

    def get_history_obj(self):
        return self.space.objs[self.obj]

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
        storage_obj = command.get_history_obj()

        while 1:
            if not storage_obj.catches_command:
                continue
            storage_obj.history.push(command)
            if not storage_obj.forwards_command:
                break
            storage_obj = storage_obj.parent

        self.collected_commands = []

class HistoryItem:
    '''
    Works as a linked list.
    '''
    def __init__(self,command:Command,last=None):
        self.command = command
        self.last : Union[HistoryItem,None] = last
        self.next : Union[HistoryItem,None] = None
        
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

 
