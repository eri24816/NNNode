from __future__ import annotations
from abc import ABC, abstractmethod
from tkinter import E
from typing import TYPE_CHECKING, Union, Any
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time

class Command(ABC):
    '''
    If a action from the user is expected able to undo
    '''
    def __init__(self):
        self.done = True

    @abstractmethod
    def execute(self):
        '''
        This method is called when the command is executed or redone.
        '''
        self.done = True

    @abstractmethod
    def redo(self):
        '''
        This method is called when the command is executed or redone.
        '''
        self.done = True

    @abstractmethod
    def undo(self):
        '''
        This method is called when the command is undone.
        '''
        self.done = False

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

class CommandManager():

    def __init__(self,space:Space):
        self.space = space
        self.collected_commands : list[Command] = []
        self.collected_storage_objs : list[Object] = []

    def push(self,command:Command,storage_obj:Object):
        self.collected_commands.append(command)
        self.collected_storage_objs.append(storage_obj)

    def flush(self):
        if len(self.collected_commands) == 0:
            return
        if len(self.collected_commands) == 1:
            command = self.collected_commands[0]
            storage_obj = self.collected_storage_objs[0]
        else:
            command = CommandSequence(self.collected_commands)
            storage_obj = self.space.get_co_ancestor(self.collected_storage_objs)
        
        while 1:
            if not storage_obj.has_history:
                continue
            storage_obj.history.push(command)
            if not storage_obj.forwards_command:
                break
            storage_obj = storage_obj.parent
        


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

 
