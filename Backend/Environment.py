from __future__ import annotations
from threading import Event
from collections import deque
import objectsync_server
from typing import Dict
from history import *
import edge
import node
import inspect

class MyDeque(deque):
    # https://stackoverflow.com/questions/56821682/having-the-deque-with-the-advantages-of-queue-in-a-thread
    def __init__(self):
        super().__init__()
        self.not_empty = Event()
        #self.not_empty.set()

    def append(self, elem):
        super().append(elem)
        self.not_empty.set()

    def appendleft(self, elem):
        super().appendleft(elem)
        self.not_empty.set()

    def pop(self):
        self.not_empty.wait()  # Wait until not empty, or next append call
        if not (len(self) - 1):
            self.not_empty.clear()
        return super().pop()

class DequeLock:
    def __init__(self,env:Env):
        self.env = env
    def __enter__(self):
        self.env.lock_deque = True
    def __exit__(self, type, value, traceback):
        self.env.lock_deque = False

# the environment to run the code in
class Env(objectsync_server.Object):

    node_classes = {}
    for m in inspect.getmembers(node, inspect.isclass):
        node_classes.update({m[0]:m[1]})

    def __init__(self,name):
        super(Env, self).__init__(name)
        self.nodes: Dict[str,node.Node] = {} # {id : Node}
        self.edges={} # {id : Edge}
        self.globals=globals()
        self.locals={}

        # for run() thread

        self.node_stack = MyDeque()

        # If a node produces a backward signal, prevent its sibling to be activated by setting lock_deque to True
        self.lock_deque = False
        self.running_node : node.Node = None
        
    def get_deque_lock(self):
        return DequeLock(self)
    
    def Create(self,info:node.Node.Info): 
        '''
        Create any type of node or edge in the environment
        '''
        # info: {id, type, ...}
        type = info['type']
        if type == 'ControlFlow':
            c = edge.ControlFlow
        elif type == 'DataFlow':
            c = edge.DataFlow
        else:
            c = self.node_classes[type]
        new_instance = c(info,self)

        id=info['id']
        if info['type']=="DataFlow" or info['type']=="ControlFlow":
            assert id not in self.edges
            self.edges.update({id:new_instance})
        else:
            assert id not in self.nodes
            self.nodes.update({id:new_instance})

    def Remove(self,info): # remove any type of node or edge
        if info['id'] in self.edges:
            self.edges[info['id']].remove()
            
        else:
            with self.history.sequence(): # removing a node may cause some edges also being removed. When undoing and redoing, these multiple actions should be done in a sequence.
                self.nodes[info['id']].remove()

    def Add_buffered_message(self,id,command,content = ''):
        '''
        new - create a demo node
        out - output
        clr - clear output
        atr - set attribute
        '''

        priority = 5 # the larger the higher, 0~9
        if command == 'new':
            priority = 8
        
        k=str(9-priority)+command+"/"+str(id)
        if command == 'new':
            k+='/'+content['type']
        if command == 'atr':
            k+='/'+content

        if command == 'out':
            if k not in self.message_buffer:
                self.message_buffer[k] = ''
            self.message_buffer[k]+=content
        elif command == 'clr':
            self.message_buffer[str(9-priority)+"out/"+id] = ''
            self.message_buffer[k]=content
        else:
            self.message_buffer[k]=content

    def Undo(self):
        if self.history.current.last==None:
            return 0 # noting to undo

        # undo
        self.history.current.direction = -1
        type=self.history.current.type
        content=self.history.current.content

        with self.history.lock():
            if type == "new":
                if content['id'] in self.nodes:
                    self.history.current.content=self.nodes[content['id']].get_info()
                self.Remove(content)
            elif type=="rmv":
                self.Create(content)
            elif type=="atr":
                self.nodes[content['id']].attributes[content['name']].set(content['old'])

        seq_id_a = self.history.current.sequence_id
        
        self.history.current=self.history.current.last

        seq_id_b = self.history.current.sequence_id
        
        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Undo() # Continue undo backward through the action sequence

        return 1

    def Redo(self):
        if self.history.current.next==None:
            return 0 # noting to redo

        self.history.current=self.history.current.next
        self.history.current.direction=1
        type=self.history.current.type
        content=self.history.current.content

        with self.history.lock():
            if type=="new":
                self.Create(content)
            elif type=="rmv":
                self.Remove(content)
            elif type=="atr":
                self.nodes[content['id']].attributes[content['name']].set(content['new'])

        seq_id_a = self.history.current.sequence_id

        seq_id_b =  self.history.current.next.sequence_id if self.history.current.next!=None else -1

        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Redo() # Continue redo forword through the action sequence

        return 1

    def update_demo_nodes(self):
        # In a regular create node process, we call self.Create() to generate a history item (command = "new"), 
        # which will later be sent to client.
        # However, demo nodes creation should not be undone, so here we put the message "new" in update_message buffer.
        for node_class in self.node_classes.values():
            self.Add_direct_message({'command':'new','info':node_class.get_class_info()})

    def add_to_deque(self,node):
        if not self.lock_deque:
            self.node_stack.append(node)

    # run in another thread from the main thread (server.py)
    def run(self):
        self.flag_exit = 0
        while not self.flag_exit:
            self.running_node = self.node_stack.pop()
            if self.running_node == 'EXIT_SIGNAL':
                return

            self.lock_deque = False
            self.running_node.run()
                