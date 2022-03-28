from .node import Node, FunctionNode, Port, Component
from objectsync_server import Attribute

class ListNode(Node):
    '''
    Add two or more items together
    ''' 

    display_name = 'list'
    category = 'data'
    shape = 'General'

    def initialize(self):
        super().initialize()

        Port(self,'DataPort',True,1,'set',on_edge_activate = self.set)

        Port(self,'DataPort',True,64,'append',on_edge_activate = self.append,with_order= True)

        self.get_port = Port(self,'DataPort',False,64,'get')

        self.mode = Attribute(self,'mode','dropdown:once,accumulate','once')

        self.display = Attribute(self,'display','string','[]')
        Component(self,'display','Text','display')

        self.data = []

    def is_ready(self):
        return True

    def require_value(self):
        for flow in self.get_port.flows:
            flow.recive_value(self.data)

    def set(self,port : Port):
        self.data = port.flows[0].get_value().copy()
        self.display.set(repr(self.data))

    def append(self,port : Port):
        for flow in port.flows:
            if flow.active:
                self.data.append(flow.get_value())
        self.display.set(repr(self.data))