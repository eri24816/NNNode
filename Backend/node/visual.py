from .node import Node, Port, Attribute, Component

class DisplayNode(Node):
    shape = 'General'
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


    def in_data_active(self):
        self.in_data.flows[0].deactivate()
        if self.mode.value == '__str__':
            self.display_content.set(self.in_data.flows[0].data.__str__())
        elif self.mode.value == 'image':
            pass
        elif self.mode.value == 'stat':
            pass
