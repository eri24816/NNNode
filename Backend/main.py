from objects import RootObject
import objectsync_server as objsync
import Environment
from node import node_class_dict
import objects

class_dict = node_class_dict.copy()

import inspect

for name, obj in inspect.getmembers(objects):
    
    if inspect.isclass(obj):
        if issubclass(obj,objsync.Object):
            class_dict[name]=obj

objsync.start(Environment.Env,class_dict,RootObject)