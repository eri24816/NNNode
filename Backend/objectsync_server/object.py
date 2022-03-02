from __future__ import annotations
import copy
from typing import Any, Dict
from objectsync_server.command import History
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
    def __init__(self, obj : Object,name,type, value,history_in = 'self',callback=None):
        obj.attributes[name]=self
        self.obj = obj
        self.name = name
        self.type = type # string, float, etc.
        self.value = value

        # History is for undo/redo. Every new changes of an attribute creates an history item.
        self.history_in = history_in 

        self.callback = callback
    
    def set(self,value, store_history:bool = True, specific_history_in = None):

        if self.callback != None:
            self.callback(self.value,value)
        
        if callable(value):
            value(self.value)
        else:
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
    catches_command = True
    forwards_command = True

    def __init__(self,space : Space, d, is_new=False, parent = None):
        self.space = space
        self.id = d['id']
        self.history = History(self)
        self.attributes : Dict[str,Attribute] = {}

        self.parent_id = Attribute(self,'children_ids','str',d['parent_id'],'0',history_in='none',callback=self.OnParentChanged) # Set history_in to 'none' because OnParentChanged will save history

        self.children_ids = [] # It's already sufficient that parent_id be an attribute.

        # Child classes can override this function ( instead of __init__() ) to add attributes or anything the class needs.
        self.initialize()
        
        # After the object's server-side attributes and fields are initialized, we can set their values accroding to d.
        self.deserialize(d)

        if is_new:
            # Called at the end of __init__() to ensure child objects are created after this object is completely created.
            self.initialize_first_time(parent)


    def initialize(self):
        '''
        Add attributes here.
        This function will be called before deserialize() to make sure server-side attributes are initialized before being set.
        '''
        pass

    def initialize_first_time(self,parent):
        '''
        Add default child objects here.
        This function will be called only on 'create' command, but not on 'undo', 'redo' or 'copy' command.
        Called at the end of __init__() to ensure child objects are created after this object is completely created.
        '''
        self.parent_id.set(parent)

    def deserialize(self,d):
        if 'attr' in d:
            for attr_dict in d['attr']:
                if attr_dict['name'] in self.objsync.Attributes:
                    self.attributes[attr_dict['name']].set(attr_dict['value'])
                else:
                    Attribute(self,attr_dict['name'],attr_dict['type'],attr_dict['value'],attr_dict['h']).set(attr_dict['value'])

    def serialize(self) -> Dict[str,Any]:
        # Do we need to serialize history ?
        d = dict()
        d.update({
            "id":self.id,
            "type":type(self).__name__
            })
        return d

    def send_direct_message(_, message): pass
    def send_buffered_message(self,id,command,content = ''): pass

    # Attribute callbacks 
    def OnParentChanged(self,old,new):
        self.space.objs[old].children_ids.remove(self.id)
        self.space.objs[new].children_ids.append(self.id)

        co_ancestor = self.space.get_co_ancestor(old,new)

        # Manually update history to specify saving history to the co-ancestor.
        self.space.objs[co_ancestor].catch_history("atr",{"id":self.id,"name": 'parent_id',"old":old,"new":new})

    # --------------------------

    def recieve_message(self,m,ws):
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

    def throw_history(self,type,content):
        self.space.objs[self.parent_id.value].catch_history(type,content)

    def catch_history(self,type,content):
        if self.catches_history.value:
            self.update_history(type,content)
        else:
            self.throw_history(type,content)

    def add_history(self, type, content):
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
            if type=="create":
                self.Create(content)
            elif type=="destroy":
                self.Remove(content)
            elif type=="attribute":
                self.nodes[content['id']].attributes[content['name']].set(content['new'])

        seq_id_a = self.history.current.sequence_id

        seq_id_b =  self.history.current.next.sequence_id if self.history.current.next!=None else -1

        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Redo() # Continue redo forword through the action sequence

        return 1

    def OnDestroy(self):
        #self.send_direct_message({'command':'destroy','id':self.id})
        pass

    def OnChildCreated(self,m):
        pass

    def OnChildDestroyed(self,m):
        pass
