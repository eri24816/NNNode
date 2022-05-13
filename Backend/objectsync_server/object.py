from __future__ import annotations
from typing import Any, Dict, Optional
from typing import TYPE_CHECKING
from xml.dom.minidom import Attr
if TYPE_CHECKING:
    from objectsync_server.space import Space

from objectsync_server.command import History, CommandAttribute, get_co_ancestor

class Attribute:
    '''
    An object can have 0 or more attributes with names. 

    Attributes are states of object, they can be string, float or other types. Once an attribute is modified (whether in client or server),
    the server will send "attribute" command to all clients to update the attribute value.
    '''
    def __init__(self, obj : Object,name,type, value, history_obj:Optional[str] = 'none',callback=None):
        obj.attributes[name]=self
        self.obj = obj
        self.name = name
        self.type = type # string, float, etc.
        self.value = value

        self.history_obj = history_obj

        # Called when the attribute is changed.
        self.callback = callback
    
    def set_com(self,value):
        '''
        Call set() with a CommandAttribute, to enable undo and redo
        '''
        if value == self.value: 
            return
        if self.history_obj == 'self':
            history_obj = self.obj.id
        elif self.history_obj == 'parent':
            if self.obj.id != "0":
                history_obj = self.obj.parent.id
            else:
                history_obj = "none"
        else:
            history_obj = self.history_obj
        CommandAttribute(self.obj.space, self.obj.id, self.name, value, history_obj).execute()

    def set(self,value, send = True):
        '''
        Note: Call set_com() instead if you want to record the change in history.
        '''
        if value == self.value: 
            return
        if self.callback != None:
            self.callback(self.value,value)
        
        if callable(value):
            value(self.value)
        else:
            self.value = value

        if send:
            self.obj.space.send_message({'command':'attribute','id':self.obj.id,'name':self.name,'value':self.value})

    def serialize(self):
        d = {'type' : self.type, 'value' : self.value,'history_object':self.history_obj}
        return d
    
class StreamAttribute(Attribute):
    '''
    A special type of attribute that saves traffic
    '''
    def set_com(self,value):
        raise Exception('StreamAttribute does not support history')

    def add(self,string):
        self.obj.space.send_message({'command':'stream add','id':self.obj.id,'name':self.name,'value':string})
        self.set(self.value+string, send=False)

    def clear(self):
        self.obj.space.send_message({'command':'stream clear','id':self.obj.id,'name':self.name})
        self.set('', send=False)

class Object:
    '''
    Base class for ObjectSync objects.
    '''
    frontend_type = 'Object'
    catches_command = True
    forwards_command = True

    def __init__(self,space : Space, d, is_new=False, parent = None):
        self.space = space
        self.id = d['id']
        self.history = History(self)
        self.history_on = True
        self.attributes : Dict[str,Attribute] = {}
        if 'attributes' in d and 'parent_id' in d['attributes']:
            parent = d['attributes']['parent_id']['value']

        self.parent_id = Attribute(self,'parent_id','String', parent,history_obj='none',callback=self.OnParentChanged) # Set history_in to 'none' because OnParentChanged will save history

        if self.id != '0':
            self.parent = self.space[self.parent_id.value]

        
        self.space.objs.update({self.id:self})
        
        self.children_ids = [] # It's already sufficient that parent_id be an attribute.

        # Child classes can override this function ( instead of __init__() ) to add attributes or anything the class needs.
        self.initialize()
        
        self.deserialize(d)
        
        if is_new:
            self.build()   

    def initialize(self):
        '''
        Add attributes here.
        This function will be called before deserialize() to make sure server-side attributes are initialized before being set.
        '''
        pass

    def build(self):
        '''
        Add default child objects here.
        This function will be called only on 'create' command, but not on 'undo', 'redo' or 'copy' command.
        Called at the end of __init__() to ensure child objects are created after this object is completely created.
        '''
        pass

    def deserialize(self,d):
        if 'attributes' in d:
            for name, attr_dict in d['attributes'].items():
                if name in self.attributes:
                    self.attributes[name].value = (attr_dict['value'])
                else:
                    Attribute(self,name,attr_dict['type'],attr_dict['value'],attr_dict['history_object']if 'history_object' in attr_dict else "none")
        if 'children' in d:
            for child_dict in d['children']:
                self.space.create(child_dict,parent=self,is_new = False,send=False)

    def serialize(self) -> Dict[str,Any]:
        # Do we need to serialize history ?
        d = dict()
        attr_dict = {}
        for name, attr in self.attributes.items():
            attr_dict[name] = attr.serialize()
        d.update({
            "id":self.id,
            "type":type(self).__name__,
            'frontend_type' : self.frontend_type,
            "attributes":attr_dict,
            "children":[self.space[c].serialize() for c in self.children_ids]
            })
        return d

    # Attribute callbacks 

    def OnParentChanged(self,old_id,new_id):
        old = self.space[old_id]
        new = self.space[new_id]
        old.children_ids.remove(self.id)
        new.children_ids.append(self.id)

        self.parent = new

    # --------------------------

    def send_message(self,m):
        m['id'] = self.id
        self.space.send_message(m)

    def recieve_message(self,m,ws):
        command = m['command']

        # Update an attribute
        if command =='attribute':
            if m['name'] == 'parent_id':
                self.parent_id.history_obj = get_co_ancestor([self.space[self.parent_id.value],self.space[m['value']]]).id
            if self.history_on:
                self.attributes[m['name']].set_com(m['value'])
            else: 
                self.attributes[m['name']].set(m['value'])
        
        # Add a new attribute
        if command == 'new attribute':
            if m['name'] not in self.attributes:
                Attribute(self,m['name'],m['type'],m['value'],m['history_object'])
                self.space.send_message(m,exclude_ws=ws)

        # Delete an attribute
        if command == 'delete attribute':
            if m['name'] in self.attributes:
                self.attributes.pop(m['name'])
                self.space.send_message(m,exclude_ws=ws)

        # Undo
        if command == "undo":
            if self.history.undo():
                self.space.send_message(f"msg obj {self.id} undone",ws)
            else:
                self.space.send_message(f"msg obj {self.id} noting to undo",ws)

        # Redo
        if command == "redo":
            if self.history.redo():
                self.space.send_message(f"msg obj {self.id} redone",ws)
            else:
               self.space.send_message(f"msg obj {self.id} noting to redo",ws)

        if command == "history on":
            self.history_on = True

        if command == "history off":
            self.history_on = False

    def OnDestroy(self):
        pass

    def OnChildCreated(self,child:Object):
        self.children_ids.append(child.id)

    def OnChildDestroyed(self,child:Object):
        self.children_ids.remove(child.id)

    def add_child(self,type,d={}):
        d['type']=type
        d['id'] = str(self.space.id_iter.__next__())
        return self.space.create(d,parent = self.id,send = False)