from __future__ import annotations
from threading import Event
from collections import deque
import objectsync_server
from typing import Dict
from objectsync_server.command import *
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
                