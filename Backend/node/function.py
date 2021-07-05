from .node import Component,Attribute, FunctionNode

class AddFunctionNode(FunctionNode):
    '''
    Add two or more items together
    ''' 

    display_name = '+'
    category = 'function'
    frontend_type = 'RoundNode'
    
    in_names = ["items"]
    out_names = ["sum"]
    max_in_data = [64]

    @staticmethod
    def function(items):
        if len(items) == 0:
            return 0
        t = items[0]
        for i in range(1,len(items)):
            t += items[i]
        return t

class MultiplyFunctionNode(FunctionNode):
    '''
    Multiply two or more items together
    '''

    display_name = '×'
    category = 'function'
    frontend_type = 'RoundNode'

    in_names = ["items"]
    out_names = ["prod"]
    max_in_data = [64]

    @staticmethod
    def function(items):
        if len(items) == 0:
            return 1
        t = items[0]
        for i in range(1,len(items)):
            t *= items[i]
        return t

class SubstractFunctionNode(FunctionNode):
    '''
    Calculate the difference
    '''

    display_name = '-'
    category = 'function'
    frontend_type = 'RoundNode'

    in_names = ["minuend","subtrahend"]
    out_names = ["difference"]
    max_in_data = [64,64]

    @staticmethod
    def function(minuend,subtrahend):
        return AddFunctionNode.function(minuend)-AddFunctionNode.function(subtrahend)

class FractionFunctionNode(FunctionNode):
    '''
    Calculate a fraction
    '''

    display_name = '÷'
    category = 'function'
    frontend_type = 'RoundNode'

    in_names = ["numerator","denominator"]
    out_names = ["result"]
    max_in_data = [64,64]

    @staticmethod
    def function(numerator,denominator):
        return MultiplyFunctionNode.function(numerator)/MultiplyFunctionNode.function(denominator)
    
class TestNode(FractionFunctionNode):

    display_name = 'test node'
    category = 'test'
    frontend_type = 'FunctionNode'

    def initialize(self):
        super().initialize()
        Attribute(self,'test_attr','string','')
        Component(self, 'test_slider','slider','test_attr')