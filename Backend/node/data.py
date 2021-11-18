from .node import Node, FunctionNode, Port, Attribute, Component

class AppendNode(FunctionNode):
    '''
    Add two or more items together
    ''' 

    display_name = '[]+'
    category = 'function'
    shape = 'General'
    
    in_names = ["list","items"]
    out_names = ["list"]
    max_in_data = [1,64]

    def initialize(self):
        super().initialize()
        mode = Attribute(self,'mode','dropdown:once,accumulate','once')
        Component(self,'mode','Dropdown:once,accumulate','mode')
        self.data = []

    @staticmethod
    def function(list,items):
        list += items
        return list