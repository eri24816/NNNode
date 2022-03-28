from .node import Node, Port, Component
from objectsync_server import Attribute
class ForNode(Node):
    shape = 'General'
    category = 'procedural'
    display_name = 'for'

    def initialize(self):
        super().initialize()

        Port(self,'DataPort',True,1,name='start',on_edge_activate=self.On_double_click)

        self.iterator_port = Port(self,'DataPort',True,1,name='iterable')
        self.iterator = None

        self.item_port = Port(self,'DataPort',False,name='item')

    def is_ready(self):
        
        return False

    def On_double_click(self):
        if (len(self.iterator_port.flows)==1 and self.iterator_port.flows[0].is_ready()):
            self.set_iterator()
            self.activate()

    def _run(self):
        
        for port in self.port_list:
            if port.isInput:
                for flow in port.flows:
                    flow.deactivate()
        flag_stop = True
        try:
            item = next(self.iterator)
            flag_stop = False
        except StopIteration:
            pass
        self.deactivate()

        if not flag_stop:
            self.activate()# Recursively activate

            for edge in self.item_port.flows:
                edge.recive_value(item)
    
    def set_iterator(self):
        self.iterator = iter(self.iterator_port.flows[0].get_value())

    def running_finished(self, success = True):
        if not success:
            self.deactivate()

class WhileNode(Node):
    shape = 'General'
    category = 'procedural'
    display_name = 'while'

    def initialize(self):
        super().initialize()

        Port(self,'DataPort',True,1,name='start',on_edge_activate=self.activate)

        self.condition_port = Port(self,'DataPort',True,1,name='condition')

        self.item_port = Port(self,'DataPort',False,name='do')


    def is_ready(self):
        return False

    def On_double_click(self):
        if (len(self.condition_port.flows)==1 and self.condition_port.flows[0].is_ready()):
            self.activate()

    def _run(self):

        for port in self.port_list:
            if port.isInput:
                for flow in port.flows:
                    flow.deactivate()
        self.deactivate()

        if len(self.condition_port.flows)==1 and self.condition_port.flows[0].is_ready() and self.condition_port.flows[0].get_value():
        
            self.activate()# Recursively activate

            for edge in self.item_port.flows:
                edge.activate()

    def running_finished(self, success = True):
        if not success:
            self.deactivate()