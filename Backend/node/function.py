from .node import Component,Attribute,Port, FunctionNode, Node

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
    
class SliderNode(Node):

    display_name = 'slider'
    category = 'input'
    frontend_type = 'SimpleNode'

    def initialize(self):
        super().initialize()
        
        self.out_data = Port(self,'DataPort',False,64,pos = [1,0,0])

        self.slider_value = Attribute(self,'slider_value','float',0)

        Component(self, 'slider','Slider','slider_value')

    def running_finished(self, success = True):
        if success:
            for flow in self.out_data.flows:
                flow.recive_value(self.slider_value.value)
        self.deactivate()