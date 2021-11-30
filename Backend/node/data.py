from .node import Node, FunctionNode, Port, Attribute, Component

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

        Port(self,'DataPort',False,64,'get')

        self.mode = Attribute(self,'mode','dropdown:once,accumulate','once')

        self.display = Attribute(self,'display','string','[]')
        Component(self,'display','Text','display')

        self.data = []

    def set(self,port : Port):
        self.data = port.flows[0].data.copy()
        port.flows[0].deactivate()
        self.display.set(repr(self.data))

    def append(self,port : Port):
        for flow in port.flows:
            if flow.active:
                self.data.append(flow.data)
                flow.deactivate()
        self.display.set(repr(self.data))