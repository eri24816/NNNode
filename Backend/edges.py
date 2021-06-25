from __future__ import annotations
from typing import TYPE_CHECKING, TypedDict
import inspect
if TYPE_CHECKING:
    from nodes import Node

class Edge(): # abstract class
    class Info(TypedDict):
        type : str
        id : int
        tail : str # Tail node name
        head : str # Head node name
        tail_port_id : int
        head_port_id : int
        
    def __init__(self, info, env):
        '''
        create an edge connecting two nodes
        '''
        self.env=env

        self.tail : Node = env.nodes[info['tail']]
        self.head : Node = env.nodes[info['head']]        

        self.tail_port = self.tail.port_list[info['tail_port_id']]
        self.head_port = self.head.port_list[info['head_port_id']]

        self.tail_port.flows.append(self)
        self.head_port.flows.append(self)

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
        self.head_port.on_edge_activate() # Imform head node
        #TODO: inform client to play animations

    def deactivate(self):
        self.active = False
        #TODO: inform client to play animations

    def remove(self):
        self.tail_port.flows.remove(self)
        self.head_port.flows.remove(self)

        self.env.edges.pop(self.id)
        self.env.Update_history("rmv", self.info)
        del self  #*?

class ControlFlow(Edge):
    class Info(Edge.Info):
        pass
    def __init__(self, info : Info, env):
        '''
        info: {type=ControlFlow,id,tail,head}
        '''
        super().__init__(info,env)

    def activate(self):
        super().activate()
        # inform the head node
        self.head_port.on_edge_activate()

class DataFlow(Edge):
    def __init__(self, info : Edge.Info, env):
        super().__init__(info,env)
        self.active = False
        self.has_value = False
        self.data=None
        
    def recive_value(self,value):
        self.data = value
        self.has_value = True
        self.activate()
