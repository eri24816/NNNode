from .node import Component, Port, FunctionNode, Node
import torch
from objectsync_server import Attribute

class ConcatNode(FunctionNode):
    shape = 'Round'
    category = 'torch/tensor'
    display_name = 'c'

    in_names = ['input']
    out_names = ['output']
    max_in_data = [64]

    def initialize(self):
        super().initialize()
        self.dim = Attribute(self,'dim','float',0)

        for port in self.in_data:
            port.with_order = True
        self.function = lambda x: torch.cat(x,dim=int(self.dim.value))

class StackNode(FunctionNode):
    shape = 'Round'
    category = 'torch/tensor'
    display_name = 's'

    in_names = ['input']
    out_names = ['output']
    max_in_data = [64]

    def initialize(self):
        super().initialize()
        for port in self.in_data:
            port.with_order = True
        self.function = lambda x: torch.stack(x,dim=int(self.dim.value))
