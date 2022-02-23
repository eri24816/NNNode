from __future__ import annotations
from abc import ABC, abstractmethod
from tkinter import E
from typing import TYPE_CHECKING, Union
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time

class Command(ABC):
    '''
    If a action from the user is expected able to undo
    '''
    def __init__(self,space:Space,obj:Object):
        self.space = space
        self.node = obj
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
    
    def __init__(self, space:Space, obj: Object, commands:list[Command]):
        super().__init__(space,obj)
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
    A null Command represents the first HistoryItem in the History linked list.
    '''
    def execute(self):
        raise NotImplementedError()

    def redo(self):
        raise NotImplementedError()

    def undo(self):
        raise NotImplementedError()

class CommandCreate(Command):

    def __init

class CommandAttribute(Command):

    def __init__(self,space:Space,obj:Object,name:str,value):
        super().__init__(space,obj)
        self.name = name
        self.value = value

    def execute(self):
        super().execute()
        self.node.attributes[self.name].set(self.value)

    def redo(self):
        super().redo()
        self.node.attributes[self.name].set(self.value)

    def undo(self):
        super().undo()
        self.node.attributes[self.name].set(self.value)

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
        self.current : HistoryItem = HistoryItem(CommandHead(self.space,object.id))

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

 
