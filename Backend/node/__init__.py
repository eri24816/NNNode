from .node import CodeNode, EvalAssignNode, Port

from .function import *
from .test import *
from .user_defined import *
from .visual import *
from .procedural import *
from .data import *
from .torch import *

del FunctionNode,Component,Attribute,Port

object_class_list = [TestNode1,TestNode2]
import sys, inspect
def print_classes():
    for name, obj in inspect.getmembers(sys.modules[__name__]):
        if inspect.isclass(obj):
            if isinstance(obj,Node):
                object_class_list.append(obj)

object_class_dict=dict()
for c in object_class_list:
    object_class_dict[c.__name__]=c

del Node