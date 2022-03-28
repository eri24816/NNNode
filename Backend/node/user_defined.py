from .node import Component,Port, FunctionNode, Node
from objectsync_server import Attribute
'''
Define your own nodes here!

Example:

class IsEvenNode(FunctionNode):
    display_name = 'is_even'
    in_names = ['n']
    out_names = ['out']
    
    @staticmethod
    def function(n):
        return not (n%2)
'''
