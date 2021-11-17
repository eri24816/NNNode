from .node import Component, Attribute, Port, FunctionNode, Node

class TestNode1(FunctionNode):
    display_name = 'CNN block'
    category = 'function'
    shape = 'General'

    in_names = ['in','bn']
    out_names = ['out']
    max_in_data = [64,64]

class TestNode2(FunctionNode):
    display_name = 'KLD loss'
    category = 'function'
    shape = 'General'

    in_names = ['mu','covariance']
    out_names = ['KLD']
    max_in_data = [64,64]

    def initialize(self):
        super().initialize()
        Attribute(self,'atrname','float',0)
        Component(self,'name','Slider','atrname')

class TestNode3(FunctionNode):
    display_name = 'sample'
    category = 'function'
    shape = 'General'

    in_names = ["input"]
    out_names = ['value','mu','var']
    max_in_data = [64]

class IsEvenNode(FunctionNode):
    display_name = '2'
    in_names = ['n']
    out_names = ['out']
    shape = 'Round'

    @staticmethod
    def function(n):
        return not (n%2)