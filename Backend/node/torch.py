from .node import Component, Attribute, Port, FunctionNode, Node
import torch

class ConcatNode(FunctionNode):
    shape = 'round'
    category = 'torch/tensor'
    display_name = 'c'

    in_names = ['input']
    out_names = ['output']
    max_in_data = [64]

    def initialize(self):
        super().initialize()
        for port in self.in_data:
            port.with_order = True
        self.function = torch.cat

class StackNode(FunctionNode):
    shape = 'round'
    category = 'torch/tensor'
    display_name = 's'

    in_names = ['input']
    out_names = ['output']
    max_in_data = [64]

    def initialize(self):
        super().initialize()
        for port in self.in_data:
            port.with_order = True
        self.function = torch.stack

