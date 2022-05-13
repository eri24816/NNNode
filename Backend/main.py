'''
The entry point of NNNode server
'''
from objects import RootObject
import objectsync_server as objsync
import Environment
from node import node_class_dict
import objects

if __name__ == '__main__':

    # Object classes that can be instantiated in the space
    class_dict = {}

    # Add all node classes to the class_dict
    class_dict.update(node_class_dict.copy())

    # Collect all other Object classes into class_dict
    import inspect
    for name, obj in inspect.getmembers(objects):
        if inspect.isclass(obj):
            if issubclass(obj,objsync.Object):
                class_dict[name]=obj

    # Remove abstract classes. They can't be instantiated.
    del class_dict['Object']
    del class_dict['Node']
    del class_dict['RootObject']

    # Start the objectsync server.
    objsync.start(Environment.Env,class_dict,RootObject)