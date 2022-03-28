from .node import CodeNode, EvalAssignNode, Port

from .function import *
from .test import *
from .user_defined import *
from .visual import *
from .procedural import *
from .data import *
from .torch import *

del FunctionNode,Component,Attribute,Port

node_class_list = [TestNode1]
import sys, inspect
def print_classes():
    for name, obj in inspect.getmembers(sys.modules[__name__]):
        if inspect.isclass(obj):
            if isinstance(obj,Node):
                node_class_list.append(obj)

del Node