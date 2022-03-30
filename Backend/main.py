import objectsync_server as objsync
import Environment
from node import object_class_dict

objsync.start(Environment.Env,object_class_dict)