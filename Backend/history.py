import datetime
class History_item:
    '''
    works as a linked list
    '''
    def __init__(self,type,content={},last=None,sequence_id=-1):
        self.type=type
        self.content=content
        self.last=last
        self.next=None
        self.head_direction=0 
        self.version=0 # sometimes we can increase version instead of create a new history_item to save memory
        '''
        head_direction is for client to find the head (server/upd)
        it changes when edit/undo/redo
        -1: backward, 0: self is head, 1: forward
        '''
        
        self.sequence_id = sequence_id
        self.time=datetime.datetime.now()
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

class History_lock():
    def __init__(self,o):
        self.o=o
    def __enter__(self):
        self.o.lock_history=True
    def __exit__(self, type, value, traceback):
        self.o.lock_history = False
        