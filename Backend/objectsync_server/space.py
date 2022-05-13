from __future__ import annotations
from typing import Dict
from .object import Object
from objectsync_server.command import CommandManager, CommandCreate, CommandDestroy

import json
from itertools import count

class Space():
    '''
    In ObjectSync, `Object`s must be created in a `Space`. A `Space` has a collection of `Object`s. It assigns an unique id to each `Object` created in it, so `Object`s in the same `Space` can access each other with the ids.
    '''
    obj_classes = {}
    def __init__(self,name, obj_classes:Dict[str,type], root_obj_class:type = Object):
        self.name = name
        self.thread=None
        self.id_iter = count(1)
        self.ws_clients = []
        self.command_manager = CommandManager(self)

        self.obj_classes = obj_classes
        
        self.objs = {}   
        self.root_obj : Object = root_obj_class(self,{'id':'0'},is_new = True)
        self.send_message({'command':'create','d':self.root_obj.serialize()})  

        self.flush = True

    def __getitem__(self,key):
        return self.objs[key]

    def recieve_message(self,message,ws):

        m=json.loads(message) # message is in Json
        
        print('-- client:\t',m)
        command=m['command']


        # Create an Object in the space
        if command == "create":
            d = m['d']
            d['id'] = str(self.id_iter.__next__())
            CommandCreate(self,d,m['parent']).execute()
            self.send_message(f"msg {m['d']['type']} {d['id']} created",ws)

        # Destroy an Object in the space
        elif command == "destroy":
            CommandDestroy(self,m['id']).execute()
            self.send_message("msg  %s destroyed" % (m['id']),ws)

        elif command == "flush on":
            self.flush = True
        elif command == "flush off":
            self.flush = False

        #TODO
        # Save the graph to disk
        elif command == "save":
            pass

        # Load the graph from disk
        elif command == "load":
            pass
        
        # Let the object handle other messages
        else:
            self.objs[m['id']].recieve_message(m,ws)

        temp = len(self.command_manager.collected_commands)
        if self.flush:
            self.command_manager.flush()

        if temp>0 and self.flush or command == 'undo' or command == 'redo':
            print(self.root_obj.history)

    def send_message(_, message,ws = None, exclude_ws = None):
        '''
        This will be overwritten by server.py
        '''

    def OnClientConnection(self,ws):
        self.ws_clients.append(ws)
        self.send_message({
            'command':'space_metadata',
            'types':list(self.obj_classes.keys()),
            },ws)
        self.send_message({
            'command':'load',
            'root_object':self.root_obj.serialize(),
            },ws)

    def GetMetadata(self):
        pass

    def create(self,d,is_new=True,parent = None,send = True):
        """        
        Create a new object in the space

        Args:
            - d (dict): serialization of the object
            - is_new (bool): whether the object is first time created. If not, it may be created by undoing, redoing or loading from a file. 
            - parent ([type], optional): the id of its parent. Must provide if is_new == True.
        """
        type,id = d['type'],d['id']
        c = self.obj_classes[type]

        # Instantiate the object
        # Note that the constructor may automatically create more objects (usually as children)
        new_instance : Object
        if is_new:
            assert parent is not None
            new_instance = c(self,d,is_new,parent)
        else:
            new_instance = c(self,d,is_new)
            parent = d['attributes']['parent_id']['value']

        self.objs[parent].OnChildCreated(new_instance)

        if send:
            self.send_message({'command':'create','d':new_instance.serialize()})

        return new_instance

    def destroy(self,id):
        self.send_message({'command':'destroy','id':id})
        parent_id = self.objs[id].parent_id.value
        self.objs[parent_id].OnChildDestroyed(self[id])
        self.objs[id].OnDestroy()
        self.objs.pop(id)
    
    def main_loop(self):
        raise NotImplementedError()



    