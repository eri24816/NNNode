from .node import CodeNode, EvalAssignNode, Port

from .function import *
from .test import *
from .user_defined import *
from .visual import *
from .procedural import *
from .data import *
from .torch import *

del FunctionNode,Component,Attribute,Port

node_class_list = [TestNode1,TestNode2]
import sys, inspect
def print_classes():
    for name, obj in inspect.getmembers(sys.modules[__name__]):
        if inspect.isclass(obj):
            if isinstance(obj,Node):
                node_class_list.append(obj)

node_class_dict=dict()
for c in node_class_list:
    node_class_dict[c.__name__]=c

del Node