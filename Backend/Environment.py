from __future__ import annotations
from threading import Event
from collections import deque
import objectsync_server
from typing import Dict
from objectsync_server import server
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

class Env(objectsync_server.Space):

    def __init__(self,name, obj_classes,base_obj_class):
        super(Env, self).__init__(name, obj_classes,base_obj_class)
        self.globals=globals()
        self.locals={}
        self.node_stack = MyDeque()

        # If a node produces a backward signal, prevent its sibling to be activated by setting lock_deque to True
        self.lock_deque = False
        self.running_node : Optional[node.Node] = None
        
    def get_deque_lock(self):
        return DequeLock(self)

    def update_demo_nodes(self):
        for node_class in self.node_classes.values():
            self.send_direct_message({'command':'new','info':node_class.get_class_info()})

    def add_to_deque(self,node):
        if not self.lock_deque:
            self.node_stack.append(node)

    def main_loop(self):
        self.flag_exit = 0
        while not self.flag_exit:
            self.running_node = self.node_stack.pop()
            if self.running_node == 'EXIT_SIGNAL':
                return

            self.lock_deque = False
            self.running_node.run()

from objectsync_server.server import set_space_class
set_space_class(Env)