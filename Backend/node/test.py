from .node import Component, Attribute, Port, FunctionNode, Node

class TestNode1(FunctionNode):
    display_name = 'CNN block'
    category = 'function'
    frontend_type = 'GeneralNode'

    in_names = ['in','bn']
    out_names = ['out']
    max_in_data = [64,64]

class TestNode2(FunctionNode):
    display_name = 'KLD loss'
    category = 'function'
    frontend_type = 'GeneralNode'

    in_names = ['mu','covariance']
    out_names = ['KLD']
    max_in_data = [64,64]

class TestNode3(FunctionNode):
    display_name = 'sample'
    category = 'function'
    frontend_type = 'GeneralNode'

    in_names = ["input"]
    out_names = ['value','mu','var']
    max_in_data = [64]

class IsEvenNode(FunctionNode):
    display_name = 'is_even'
    in_names = ['n']
    out_names = ['out']
    frontend_type = 'RoundNode'

    @staticmethod
    def function(n):
        return not (n%2)