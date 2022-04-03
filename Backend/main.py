import objectsync_server as objsync
import Environment
from node import object_class_dict

class RootObject(objsync.Object):
    pass

objsync.start(Environment.Env,object_class_dict,RootObject)