'''
But I still don't know what is this file for.

OUO.

I just want those files defining nodes to be in a folder.
And it seems to become a package (?)
'''

from .node import CodeNode, EvalAssignNode, Port

from .function import *
from .test import *
del FunctionNode,Component,Attribute,Port,Node