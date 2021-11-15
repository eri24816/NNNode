from .node import Node, Port, Attribute, Component

class ForNode(Node):
    shape = 'General'
    category = 'procedural'
    display_name = 'for'

    def initialize(self):
        super().initialize()

        Port(self,'DataPort',True,1,name='start',on_edge_activate=self.activate)

        self.iterator_port = Port(self,'DataPort',True,1,name='iterable',on_edge_activate=self.set_iterator)
        self.iterator = None

        self.item_port = Port(self,'DataPort',False,name='item')

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
        self.iterator = iter(self.iterator_port.flows[0].data)

class WhileNode(Node):
    shape = 'General'
    category = 'procedural'
    display_name = 'while'

    def initialize(self):
        super().initialize()

        Port(self,'DataPort',True,1,name='start',on_edge_activate=self.activate)

        self.condition_port = Port(self,'DataPort',True,1,name='condition',on_edge_activate=self.set_condition)
        self.condition = False

        self.item_port = Port(self,'DataPort',False,name='do')

    def _run(self):
        for port in self.port_list:
            if port.isInput:
                for flow in port.flows:
                    flow.deactivate()
        self.deactivate()

        if self.condition:
            self.activate()# Recursively activate

            for edge in self.item_port.flows:
                edge.activate()
    
    def set_condition(self):
        self.condition = self.condition_port.flows[0].data