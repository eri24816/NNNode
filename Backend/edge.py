from __future__ import annotations
from typing import TYPE_CHECKING, TypedDict
import inspect
if TYPE_CHECKING:
    from node import Node

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
        
        # for client --------------------------------
        self.info = info  # for remove history
        self.type=info['type']
        self.id=info['id']
        self.env.Update_history("new", info)
        self.env.Add_direct_message({'command':'new','info':self.get_info()})

        # For a data flow, active == True means it has data currently (stored in self.value)
        # For a signal flow, active == True means the signal is active
        self.active = False
        self.value=None
        #self.is_ready = self.tail_port.is_ready

        # Call this to check if get_value() is ready to be called
        self.is_ready = lambda : self.active or self.tail.is_ready()

        # Stay active after get_value() being called
        self.retain_value = False #TODO: enable client change this


    def get_info(self):
        return self.info

    def recive_value(self,value):
        self.value = value
        self.activate()

    def get_value(self):
        assert self.is_ready()
        
        if not self.active:
            self.tail.require_value()
            assert self.active # Node.require_value() must provide a value to the flow
            
        if not self.retain_value:
            self.deactivate()
        return self.value

    def activate(self):
        if not self.active:
            self.active = True
            self.env.Add_buffered_message(self.id, 'act', '2')
        if self.head_port.on_edge_activate.__code__.co_argcount == 2:
            self.head_port.on_edge_activate(self.head_port) # Imform head node
        else:
            self.head_port.on_edge_activate()

    def deactivate(self):
        if self.active:
            self.active = False
            self.env.Add_buffered_message(self.id, 'act', '0')
    def remove(self):
        self.tail_port.flows.remove(self)
        self.head_port.flows.remove(self)

        self.env.edges.pop(self.id)
        self.env.Update_history("rmv", self.info)
        self.env.Add_direct_message({'command':'rmv','id':self.id})
        del self  ##?


class DataFlow(Edge):
    def __init__(self, info : Edge.Info, env):
        super().__init__(info,env)
        self.active = False
        
    
