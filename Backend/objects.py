import objectsync_server as objsync
from node.node import Node
from objectsync_server.object import Attribute, Object

class RootObject(objsync.Object):
    frontend_type = 'RootObject'
    def initialize(self):
        Attribute(self,'parent_object','String','SpaceClient',history_obj='none')

    def build(self):
        self.add_child('NodeList',{
            'attributes':{
                'parent_object':{'type':'String','value':'NodeListContainer'}
            }
        })

class NodeList(objsync.Object):
    frontend_type = "Hierarchy"
    
    def build(self):
        for name,t in self.space.obj_classes.items():
            if(issubclass(t,Node)):
                new_demo_node = self.add_child(name,{'attributes':{
                    # Tag the demo node with 'is_demo' so that it can be set not interactive in the frontend
                    'tag/is_demo':{'type':'Boolean','value':True}
                    }})

class VerticalLayoutGroup(objsync.Object):
    frontend_type = 'VerticalLayoutGroup'
