from typing import List, Type
from objectsync_server.object import Object
from .node import CodeNode, EvalAssignNode, Port, TestNode
from .function import *
from .test import *
from .user_defined import *
from .visual import *
from .procedural import *
from .data import *
from .torch import *

'''
# Add all node types to the node_list
node_list : List[Type[Node]] = []
import sys, inspect
for name, obj in globals().copy().items():
    if inspect.isclass(obj):
        if issubclass(obj,Node):
            node_list.append(obj)
node_list.remove(Node)
'''

node_list : List[Type[Node]] = [TestNode1,TestNode2,TestNode3]

node_class_dict=dict()
for c in node_list:
    node_class_dict[c.__name__]=c
