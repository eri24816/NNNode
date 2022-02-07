from __future__ import annotations
import copy
from typing import Dict
from history import History
import time

from objectsync_server.space import Space

class Attribute:
    '''
    A node can have 0, 1, or more attributes and components. 

    Attributes are states of nodes, they can be string, float or other types. Once an attribute is modified (whether in client or server),
    cilent (or server) will send "atr" command to server (or client) to update the attribute.

    Components are UI components that each controls an attribute, like slider or input field.

    Not all attributes are controlled by components, like attribute "pos". 
    '''
    def __init__(self, obj : Object,name,type, value,history_in = '-1'):
        obj.attributes[name]=self
        self.obj = obj
        self.name = name
        self.type = type # string, float, etc.
        self.value = value

        # History is for undo/redo. Every new changes of an attribute creates an history item.
        self.history_in = history_in 
    
    def set(self,value, store_history:bool = True):

        # Update history
        if store_history:
            if self.history_in != '-1':
                self.obj.space.objs[self.history_in].Update_history("atr",{"id":self.node.id,"name": self.name,"old":self.value,"new":value})

        self.value = value

        # Send to client
        #self.node.space.Add_buffered_message(self.node.id,'atr',self.name)
        self.obj.space.Add_direct_message({'command':'atr','id':self.node.id,'name':self.name,'value':value})

    def serialize(self):
        return {'name' : self.name, 'type' : self.type, 'value' : self.value, 'h' : self.history_in}
    

class Object:
    '''
    Base class for ObjectSync objects.
    '''
    def __init__(self,space : Space,d):
        self.id = d['id']
        self.space = space
        self.history = History()
        self.attributes : Dict[str,Attribute] = {}
        self.children_ids = Attribute(self,'children_ids','list',[],self.id)
        self.parent_id = Attribute(self,'children_ids','str',d['parent_id'],'0')

        if 'attr' in d:
            for attr_dict in d['attr']:
                if attr_dict['name'] in self.objsync.Attributes:
                    self.attributes[attr_dict['name']].set(attr_dict['value'])
                else:
                    Attribute(self,attr_dict['name'],attr_dict['type'],attr_dict['value'],attr_dict['h']).set(attr_dict['value'])


    def serialize(self) -> Dict[str]:
        d = dict()
        d.update({
            "id":self.id,
            "type":type(self).__name__
            })
        return d

    def send_direct_message(_, message): pass
    def send_buffered_message(self,id,command,content = ''): pass

    def recive_message(self,m,ws):
        command = m['command']

        # Update an attribute
        if command =='atr':
            self.attributes[m['name']].set(m['value'])
        
        # Add new attribute
        if command == 'nat':
            if m['name'] not in self.attributes:
                Attribute(self,m['name'],m['type'],m['value'],m['h']).set(m['value'],False) # Set initial value

        # Undo
        if command == "udo":
            if self.Undo():
                ws.send(f"msg obj {self.id} undone")
            else:
                ws.send("msg obj {self.id} noting to undo")

        # Redo
        if command == "rdo":
            if self.Redo():
                ws.send("msg obj {self.id} redone")
            else:
                ws.send("msg obj {self.id} noting to redo")

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

        # undo
        type=self.history.current.type
        content=self.history.current.content

        with self.history.lock():
            if type == "new":
                if content['id'] in self.nodes:
                    self.history.current.content=self.nodes[content['id']].get_info()
                self.Remove(content)
            elif type=="rmv":
                self.Create(content)
            elif type=="atr":
                self.nodes[content['id']].attributes[content['name']].set(content['old'])

        seq_id_a = self.history.current.sequence_id
        
        self.history.current=self.history.current.last

        seq_id_b = self.history.current.sequence_id
        
        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Undo() # Continue undo backward through the action sequence

        return 1
    
    def Redo(self):
        if self.history.current.next==None:
            return 0 # nothing to redo

        self.history.current=self.history.current.next
        self.history.current.direction=1
        type=self.history.current.type
        content=self.history.current.content

        with self.history.lock():
            if type=="new":
                self.Create(content)
            elif type=="rmv":
                self.Remove(content)
            elif type=="atr":
                self.nodes[content['id']].attributes[content['name']].set(content['new'])

        seq_id_a = self.history.current.sequence_id

        seq_id_b =  self.history.current.next.sequence_id if self.history.current.next!=None else -1

        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Redo() # Continue redo forword through the action sequence

        return 1

    def add_child(self,new_instance,d): 
        
        self.objs.update({d['id']:new_instance})

    def remove_child(self,info):
        with self.history.sequence(): # removing a node may cause some edges also being removed. When undoing and redoing, these multiple actions should be done in a sequence.
            self.objs[info['id']].remove()

    def destroy(self):
        for port in self.port_list:
            for flow in copy.copy(port.flows):
                flow.remove()
        self.space.Update_history('des',self.get_info())
        self.send_direct_message({'command':'des','id':self.id})
        del self  #*?    