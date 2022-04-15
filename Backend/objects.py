import objectsync_server as objsync
import node
from objectsync_server.object import Object

class RootObject(objsync.Object):
    frontend_type = 'RootObject'

    def build(self):
        self.add_child('NodeList')

class NodeList(objsync.Object):
    frontend_type = "VerticalLayoutGroup"
    
    def build(self):
        self.heirarchy = self.add_child('Heirarchy')
        for name,t in node.node_class_dict.items():
            new_demo_node = self.heirarchy.add_child(name,{'attributes':{
                'is_demo':{'type':'Boolean','value':True}
                }})
            self.heirarchy.add_item(new_demo_node)

class Hierarchy(objsync.Object):
    frontend_type = "Hierarchy"
    
    def add_item(self,item : Object):
        if item.parent != self:
            return
        
        self.send_message({'command':'add item','item_id':item.id})

class VerticalLayoutGroup(objsync.Object):
    frontend_type = 'VerticalLayoutGroup'