import objectsync_server as objsync
import node

class RootObject(objsync.Object):
    frontend_type = 'RootObject'

    def build(self):
        self.add_child('NodeList')

class NodeList(objsync.Object):
    frontend_type = "NodeList"

    def build(self):
        for name,t in node.node_class_dict.items():
            self.add_child(name,{'attributes':[
                {'name':'is_demo','type':'Boolean','value':True}
                ]})