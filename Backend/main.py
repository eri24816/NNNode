import objectsync_server as objsync
import Environment
from node import node_class_list

objsync.start(Environment.Env,node_class_list)