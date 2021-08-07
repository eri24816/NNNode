from __future__ import annotations
import time

class History_item:
    '''
    works as a linked list
    '''
    def __init__(self,type,content={},last=None,sequence_id=-1):
        self.type=type
        self.content=content
        self.last=last
        self.next=None
        self.direction = 1 # -1 means this action has been undone, else direction is 1
        self.version=0 # sometimes we can increase version by 1 instead of create a new history_item to save memory
        '''
        head_direction is for client to find the head (server/upd)
        it changes when edit/undo/redo
        -1: backward, 0: self is head, 1: forward
        '''
        
        self.sequence_id = sequence_id
        self.time=time.time()
        if self.last !=None:
            self.last.next=self
            self.last.head_direction = 1  # self is head
    def __str__(self):
        result = '\nHistory:\n' if self.last == None else ''
        result += '->\t' if self.head_direction == 0 else '\t'
        result += self.type+", "+str(self.content)+'\n'
        if self.next:
            result += str(self.next)
        return result

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

    def __enter__(self):
        self.history.current_history_sequence_id = self.next_history_sequence_id
        self.next_history_sequence_id += 1

    def __exit__(self, type, value, traceback):
        self.history.current_history_sequence_id = -1   
        
class History:
    def __init__(self):
        self.current = History_item('stt')
        self.locked = False
        self.current_history_sequence_id = -1

    def lock(self): # when undoing or redoing, lock will be set to True to avoid unwanted history change
        return History_lock(self)

    def sequence(self):
        return History_sequence(self)

    def Update(self,type,content):
        if self.locked:
            return
        # add an item to the linked list
        self.current=History_item(type,content,self.current,self.current_history_sequence_id)

 