from __future__ import annotations
from abc import ABC, abstractmethod
from typing import TYPE_CHECKING
if TYPE_CHECKING:
    from objectsync_server import Space, Object

import time



class Command(ABC):

    def __init__(self,space:Space,node:Object):
        self.space = space
        self.node = node
        self.done = False
        self.history_references : list[HistoryItem] = []

        '''
        On one user action, multiple commands may be executed sequentially.
        '''
        self.sequence_front = None
        self.sequence_back = None

    @abstractmethod
    def forward(self):
        '''
        This method is called when the command is executed or redone.
        '''
        self.done = True

    @abstractmethod
    def backward(self):
        '''
        This method is called when the command is undone.
        '''
        self.done = False

class AttributeCommand(Command):

    def __init__(self,space:Space,node:Object,name:str,value):
        super().__init__(space,node)
        self.name = name
        self.value = value

    def forward(self):
        super().forward()
        self.node.attributes[self.name].set(self.value)

    def backward(self):
        super().backward()
        self.node.attributes[self.name].set(self.value)

class HistoryItem:
    '''
    Works as a linked list.
    '''
    def __init__(self,command:Command,last=None):
        self.command = command
        self.last=last
        self.next=None
        
        self.time=time.time()

class History_lock:
    def __init__(self,history):
        self.history=history

    def __enter__(self):
        self.history.locked=True

    def __exit__(self, type, value, traceback):
        self.history.locked = False

class History_sequence():
    next_history_sequence_id = 0
    def __init__(self,history : History):
        self.history=history
        self.sequence : list[HistoryItem] = []

    def __enter__(self):
        self.history

    def __exit__(self, type, value, traceback):
        self.history.current_history_sequence_id = -1   
        
class History:
    def __init__(self):
        self.current : HistoryItem
        self.locked = False

    def lock(self): # when undoing or redoing, lock will be set to True to avoid unwanted history change
        return History_lock(self)

    def sequence(self):
        return History_sequence(self)

    def Update(self,command : Command):
        if self.locked:
            return
        # add an item to the linked list
        self.current=HistoryItem(command,self.current)

 