from __future__ import annotations
from asyncio.queues import Queue
from math import inf
from typing import Dict
#rom objectsync_server.command import *
from .object import Object
from objectsync_server.command import CommandManager, CommandCreate, CommandDestroy

import json
from itertools import count

# the environment to run the code in
class Space():
    obj_classses = {}
    def __init__(self,name, obj_classses:Dict[str,type], base_obj_class:type = Object):
        self.name = name
        self.thread=None
        self.id_iter = count(0)
        self.ws_clients = []
        self.command_manager = CommandManager(self)

        # unlike history, some types of changes aren't necessary needed to be updated sequentially on client, like code in a node or whether the node is running.
        # one buffer per client
        # format:[ { "<command>/<node id>": <value> } ]
        # it's a dictionary so replicated updates will overwrite
        # self.message_buffer = {}

        self.obj_classses = obj_classses
        self.base_obj : Object = base_obj_class(self,{'id':'0'})
        self.objs = {'0':self.base_obj}       

    def __getitem__(self,key):
        return self.objs[key]

    def send_all(self,ws_list,message):
        for ws in ws_list:
            ws.send(json.dumps(message))

    def recieve_message(self,message,ws):

        m=json.loads(message) # message is in Json
        command=m['command']

        print('-- client:\t',m)

        # Create an Object in the space
        if command == "create":
            d = m['d']
            d['id'] = self.id_iter.__next__()
            CommandCreate(self,d,m['parent']).execute()
            ws.send(f"msg {m['d']['type']} {d['id']} created")

        # Destroy an Object in the space
        elif command == "destroy":
            CommandDestroy(self,m['d']['id']).execute()
            ws.send("msg %s %s destroyed" % (m['info']['type'],m['info']['id']))
        
        # Give client an unused id
        elif command == "gid":
            ws.send(json.dumps({'command':"gid",'id':next(self.id_iter)}))

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

    def send_direct_message(_, message):
        '''
        This will be overwritten by server.py
        '''

    '''
    def send_buffered_message(self,id,command,content:Any = ''):

        priority = 5 # the larger the higher, 0~9
        if command == 'create':
            priority = 8
        
        k=str(9-priority)+command+"/"+str(id)
        if command == 'create':
            k+='/'+content['type']
        if command == 'attribute':
            k+='/'+content
    '''

    def create(self,d,is_new=False,parent = None):
        """        
        Create a new object in the space

        Args:
            - d (dict): serialization of the object
            - is_new (bool): whether the object is first time created. If not, it may be created by undoing, redoing or loading from a file. 
            - parent ([type], optional): the id of its parent. Must provide if is_new == True.
        """
        type,id = d['type'],d['id']
        c = self.obj_classses[type]

        # Instantiate the object
        # Note that the constructor may automatically create more objects (usually as children)
        new_instance : Object
        if is_new:
            assert parent is not None
            new_instance = c(self,d,is_new,parent)
        else:
            new_instance = c(self,d,is_new)
            parent = d['attributes']['parent_id'].value

        self.objs.update({id:new_instance})
        self.send_direct_message({'command':'create','d':d})

        self.objs[parent].OnChildCreated(new_instance)

    def destroy(self,d):
        self.send_direct_message({'command':'destroy','id':d['id']})
        self.objs.pop(d['id'])
        parent_id = self.objs[d['id']].parent_id.value
        self.objs[parent_id].OnChildDestroyed(d['id'])
        self.objs[d['id']].OnDestroy()
    
    def main_loop(self):
        raise NotImplementedError()



    