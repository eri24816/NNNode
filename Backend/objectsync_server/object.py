from __future__ import annotations
import copy
from typing import Any, Dict, Union, Optional
from objectsync_server.command import History, CommandAttribute
import time
from typing import TYPE_CHECKING
if TYPE_CHECKING:
    from objectsync_server.space import Space
from objectsync_server.command import get_co_ancestor

class Attribute:
    '''
    A node can have 0, 1, or more attributes and components. 

    Attributes are states of nodes, they can be string, float or other types. Once an attribute is modified (whether in client or server),
    cilent (or server) will send "atr" command to server (or client) to update the attribute.

    Components are UI components that each controls an attribute, like slider or input field.

    Not all attributes are controlled by components, like attribute "pos". 
    '''
    def __init__(self, obj : Object,name,type, value, history_obj:Optional[str] = None,callback=None):
        obj.attributes[name]=self
        self.obj = obj
        self.name = name
        self.type = type # string, float, etc.
        self.value = value

        self.history_obj = history_obj if history_obj != None else obj.id

        self.callback = callback
    
    def set_com(self,value):
        '''
        Call self.set from a CommandAttribute, to enable undo and redo
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

    def set(self,value):
        '''
        Recommand to use set_com(). Direct calling this method does not add command into history
        '''
        if value == self.value: 
            return
        if self.callback != None:
            self.callback(self.value,value)
        
        if callable(value):
            value(self.value)
        else:
            self.value = value

        self.obj.space.send_direct_message({'command':'attribute','id':self.obj.id,'name':self.name,'value':self.value})

    def serialize(self):
        d = {'name' : self.name, 'type' : self.type, 'value' : self.value,'history_object':self.history_obj}
        return d
    

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
        self.attributes : Dict[str,Attribute] = {}
        if 'attributes' in d:
            for attr in d['attributes']:
                if attr['name']== 'parent_id':
                    parent = attr['value']
                break
        self.parent_id = Attribute(self,'parent_id','String', parent,history_obj='none',callback=self.OnParentChanged) # Set history_in to 'none' because OnParentChanged will save history
        

        if self.id != '0':
            import types
            def parent_attribute_set_com_overwrite(self,value):
                self.history_obj = get_co_ancestor([self.value,value])
                self.set_com(value)
            self.parent_id.set_com = types.MethodType(Attribute,parent_attribute_set_com_overwrite)
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
            for attr_dict in d['attributes']:
                if attr_dict['name'] in self.attributes:
                    self.attributes[attr_dict['name']].value = (attr_dict['value'])
                else:
                    Attribute(self,attr_dict['name'],attr_dict['type'],attr_dict['value'],attr_dict['history_object'])
        if 'children' in d:
            for child_dict in d['children']:
                self.space.create(child_dict,parent=self,is_new = False,send=False)

    def serialize(self) -> Dict[str,Any]:
        # Do we need to serialize history ?
        d = dict()
        d.update({
            "id":self.id,
            "type":type(self).__name__,
            'frontend_type' : self.frontend_type,
            "attributes":[attr.serialize() for attr in self.attributes.values()],
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

    def recieve_message(self,m,ws):
        command = m['command']

        # Update an attribute
        if command =='attribute':
            self.attributes[m['name']].set_com(m['value'])
        
        # Add new attribute
        if command == 'new attribute':
            if m['name'] not in self.attributes:
                Attribute(self,m['name'],m['type'],m['value'],m['history_object']).set_com(m['value']) # Set initial value

        # Undo
        if command == "undo":
            if self.history.undo():
                self.space.send_direct_message(f"msg obj {self.id} undone",ws)
            else:
                self.space.send_direct_message(f"msg obj {self.id} noting to undo",ws)

        # Redo
        if command == "redo":
            if self.history.redo():
                self.space.send_direct_message(f"msg obj {self.id} redone",ws)
            else:
               self.space.send_direct_message(f"msg obj {self.id} noting to redo",ws)

    def OnDestroy(self):
        pass

    def OnChildCreated(self,child:Object):
        self.children_ids.append(child.id)

    def OnChildDestroyed(self,child:Object):
        self.children_ids.remove(child.id)
