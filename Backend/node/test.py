from .node import Component, Port, FunctionNode, Node
from objectsync_server import  Attribute
class TestNode1(Node):
    display_name = 'CNN block'
    category = 'function'
    frontend_type = 'TestNode1'

    def build(self):
        d = {}
        d['type'] = 'TestNode2'
        d['id'] = str(self.space.id_iter.__next__())
        self.space.create(d,parent = self.id,send = False)


class TestNode2(Node):
    display_name = 'KLD loss'
    category = 'function/aaa'
    frontend_type = 'TestNode1'

    in_names = ['mu','covariance']
    out_names = ['KLD']
    max_in_data = [64,64]


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