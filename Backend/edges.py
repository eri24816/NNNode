from __future__ import annotations
from typing import TYPE_CHECKING, TypedDict
if TYPE_CHECKING:
    from nodes import Node

class Edge(): # abstract class
    class Info(TypedDict):
        type : str
        id : int
        tail : str # Tail node name
        head : str # Head node name
        
    def __init__(self,info,env):
        '''
        create an edge connecting two nodes
        '''
        self.env=env

        self.tail : Node = env.nodes[info['tail']]
        self.head : Node = env.nodes[info['head']]        

        self.active=False

        # for client --------------------------------
        self.info = info  # for remove history
        self.type=info['type']
        self.id=info['id']
        self.env.Update_history("new", info)

    def get_info(self):
        return self.info

    

    def activate(self):
        self.active = True
        #TODO: inform client to play animations

    def deactivate(self):
        self.active = False
        #TODO: inform client to play animations

def remove(self):
        self.env.edges.pop(self.id)
        self.env.Update_history("rmv", self.info)
        del self  #*?

class ControlFlow(Edge):
    class Info(Edge.info):
        pass
    def __init__(self, info : Info, env):
        '''
        info: {type=ControlFlow,id,tail,head}
        '''
        super().__init__(info,env)

        # Connect to the tail and head node
        self.tail.out_control.append(self)
        self.head.in_control.append(self)

    def remove(self):
        self.tail.out_control.remove(self)
        self.head.in_control.remove(self)
        super().remove()

    def activate(self):
        super().activate()
        # inform the head node
        self.head.in_control_active()

class DataFlow(Edge):
    class Info(Edge.info):
        tail_var : int
        head_var : int
    def __init__(self, info : Info, env):
        '''
        info: {type=DataFlow,id,tail,head,tail_var:int,head_var:int}
        '''
        super().__init__(info,env)
        self.active = False
        self.has_value = False
        self.data=None
        
        self.tail_var=info['tail_var']
        self.head_var=info['head_var']
        self.tail.out_data[self.tail_var].append(self)
        self.head.in_data[self.head_var].append(self)
        
        
    def remove(self):
        self.tail.out_data[self.tail_var].remove(self)
        self.head.in_data[self.head_var]=None 
        super().remove()