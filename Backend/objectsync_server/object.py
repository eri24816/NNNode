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
    def __init__(self, obj : Object,name,type, value:Union[str,function], history_obj:Optional[str] = None,callback=None):
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
        CommandAttribute(self.obj.space, self.obj.id, self.name, value, self.history_obj).execute()

    def set(self,value):
        '''
        Recommand to use set_com(). Direct calling this method does not add command into history
        '''
        if self.callback != None:
            self.callback(self.value,value)
        
        if callable(value):
            value(self.value)
        else:
            self.value = value

        self.obj.space.send_direct_message({'command':'attribute','id':self.obj.id,'value':self.value})

    def serialize(self):
        d = {'name' : self.name, 'type' : self.type, 'value' : self.value,'history_obj':self.history_obj}
        return d
    

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

        self.parent_id = Attribute(self,'children_ids','str',d['parent_id'],history_obj='none',callback=self.OnParentChanged) # Set history_in to 'none' because OnParentChanged will save history
        import types
        def parent_attribute_set_com_decorator(self,value):
            self.history_obj = get_co_ancestor([self.value,value])
            self.set_com(value)
        self.parent_id.set_com = types.MethodType(Attribute,parent_attribute_set_com_decorator)

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
                if attr_dict['name'] in self.attributes:
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
                Attribute(self,m['name'],m['type'],m['value'],m['history_obj']).set_com(m['value']) # Set initial value

        # Undo
        if command == "undo":
            if self.history.undo():
                ws.send(f"msg obj {self.id} undone")
            else:
                ws.send("msg obj {self.id} noting to undo")

        # Redo
        if command == "redo":
            if self.history.redo():
                ws.send("msg obj {self.id} redone")
            else:
                ws.send("msg obj {self.id} noting to redo")

    def OnDestroy(self):
        pass

    def OnChildCreated(self,m):
        pass

    def OnChildDestroyed(self,m):
        pass
