from __future__ import annotations
import copy
from typing import Dict
from history import History
import time

class Attribute:
    '''
    A node can have 0, 1, or more attributes and components. 

    Attributes are states of nodes, they can be string, float or other types. Once an attribute is modified (whether in client or server),
    cilent (or server) will send "atr" command to server (or client) to update the attribute.

    Components are UI components that each controls an attribute, like slider or input field.

    Not all attributes are controlled by components, like attribute "pos". 
    '''
    def __init__(self, obj : Object,name,type, value,history_in = 'node'):
        obj.attributes[name]=self
        self.obj = obj
        self.name = name
        self.type = type # string, float, etc.
        self.value = value

        # History is for undo/redo. Every new changes of an attribute creates an history item.
        # self.history_in = '' -> no storing history
        # self.history_in = 'node' -> store history in the node, like most of the attributes
        # self.history_in = 'space' -> store history in the space, like node position
        self.history_in = history_in 
    
    def set(self,value, store_history:bool = True):

        # Update history
        if store_history:
            if self.history_in == 'node':
                self.node.Update_history("atr",{"id":self.node.id,"name": self.name,"old":self.value,"new":value})
            elif self.history_in == 'space':
                self.node.space.Update_history("atr",{"id":self.node.id,"name": self.name,"old":self.value,"new":value})

        self.value = value

        # Send to client
        #self.node.space.Add_buffered_message(self.node.id,'atr',self.name)
        self.node.space.Add_direct_message({'command':'atr','id':self.node.id,'name':self.name,'value':value})

    def dict(self):
        return {'name' : self.name, 'type' : self.type, 'value' : self.value, 'h' : self.history_in}
    

class Object:
    '''
    Base class for ObjectSync objects.
    '''
    def __init__(self,id,space):

        self.id = id
        self.space = space
        self.history = History()
        self.attributes : Dict[str,Attribute] = {}

        self.children : dict[str, Object] = dict()

    def send_direct_message(_, message): pass
    def send_buffered_message(self,id,command,content = ''): pass

    def recive_message(self,m):
        command = m['command']

        if command == "new":

            self.Create(m['info'])
            self.space.send_all("msg %s %s created" % (m['info']['type'],m['info']['id']))

        elif command == "rmv":
            
            self.Remove({"id" : m['id']})
            self.space.send_all("msg %s removed" % m['id'])

        if command =='atr':
            self.objsync.Attributes[m['name']].set(m['value'])
            
        if command == 'nat':
            if m['name'] not in self.objsync.Attributes:
                Attribute(self,m['name'],m['type'],m['value'],m['h']).set(m['value'],False) # Set initial value

    def Update_history(self, type, content):
        '''
        type, content:
        stt, None - create the node
        atr, {name, old, new} - change objsync.Attribute
        '''
        # don't repeat atr history within 2 seconds 
        if type=="atr" and self.history.current.type=="atr" and content['id'] == self.history.current.content['id']:
            if (time.time() - self.history.current.time)<2:
                self.history.current.content['new']=content['new']
                self.history.current.version+=1
                return

        # add an item to the linked list
        self.history.Update(type,content)         

    def Undo(self):
        if self.history.current.last==None:
            return 0 # nothing to undo

        if self.history.current.type=="atr":
            self.objsync.Attributes[self.history.current.content['name']].set(self.history.current.content['old'],False)
        
        self.history.current.head_direction=-1
        self.history.current=self.history.current.last
        self.history.current.head_direction = 0

        return 1
    
    def Redo(self):
        if self.history.current.next==None:
            return 0 # nothing to redo
        self.history.current.head_direction=1
        self.history.current=self.history.current.next
        self.history.current.head_direction=0

        if self.history.current.type=="atr":
            self.attributes[self.history.current.content['name']].set(self.history.current.content['new'],False)

        return 1

    def create_child(self,info): 
        '''
        Create an object
        '''
        # info: {id, type, ...}
        type,id = info['type'],info['id']
        assert id not in self.objs

        c = self.obj_classes[type]
        new_instance = c(info,self)
        self.objs.update({id:new_instance})

    def remove_child(self,info): # remove object
        with self.history.sequence(): # removing a node may cause some edges also being removed. When undoing and redoing, these multiple actions should be done in a sequence.
            self.objs[info['id']].remove()

    def remove(self):
        for port in self.port_list:
            for flow in copy.copy(port.flows):
                flow.remove()
        self.space.nodes.pop(self.id)
        self.space.Update_history('rmv',self.get_info())
        self.send_direct_message({'command':'rmv','id':self.id})
        del self  #*?    