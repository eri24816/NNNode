from .node import Node, Port,  Component
from objectsync_server import Attribute
class DisplayNode(Node):
    shape = 'Simple'
    category = 'visual'
    display_name = 'display'

    def initialize(self):
        super().initialize()

        self.in_data = Port(self,'DataPort',True,1,name='data',on_edge_activate=self.in_data_active)

        self.display_content = Attribute(self, 'display_content', 'string', '',history_in='')
        self.textDisplay = Component(self, 'text_display', 'Text', 'display_content')
        #self.imageDisplay = Component(self, 'image_display', 'Text', 'display_content') 

        # I don't use components but the inspector to control these attributes.
        self.mode = Attribute(self, 'mode','dropdown:__str__,image,stat','__str__')
        self.stat_mode = Attribute(self, 'stat/mode','dropdown:min,max,sum,mean,std,L1,L2','mean')


    def is_ready(self):
        if len(self.in_data.flows)>0 and self.in_data.flows[0].is_ready():
            return True
        return False

    def in_data_active(self,port):
        self.attempt_to_activate()

    def _run(self):
        if self.mode.value == '__str__':
            self.display_content.set(str(self.in_data.flows[0].get_value()))
        elif self.mode.value == 'image':
            pass
        elif self.mode.value == 'stat':
            pass