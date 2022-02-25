from __future__ import annotations
from asyncio.queues import Queue
from typing import Dict
from objectsync_server.history import *
from collections import deque
from threading import Event
from object import Object

import json

class num_iter:
    def __init__(self,start=-1):
        self.i=start
    def next(self):
        self.i+=1
        return self.i
# the environment to run the code in
class Space():
    obj_classses = {}
    def __init__(self,name,base_obj_class:type):
        self.name = name
        self.thread=None
        self.id_iter = num_iter(0)
        self.ws_clients = []

        # unlike history, some types of changes aren't necessary needed to be updated sequentially on client, like code in a node or whether the node is running.
        # one buffer per client
        # format:[ { "<command>/<node id>": <value> } ]
        # it's a dictionary so replicated updates will overwrite
        self.message_buffer = {}
        self.base_obj : Object = base_obj_class(self,{'id':'0'})
        self.objs = {'0':self.base_obj}       
    
    def send_all(self,ws_list,message):
        for ws in ws_list:
            ws.send(json.dumps(message))

    def recieve_message(self,message,ws):

        m=json.loads(message) # message is in Json
        command=m['command']

        print('-- client:\t',m)

        # Create an Object in the space
        if command == "create":
            self.create(m,True)
            ws.send("msg %s %s created" % (m['info']['type'],m['info']['id']))

        # Destroy an Object in the space
        elif command == "destroy":
            self.destroy(m)
            ws.send("msg %s %s destroyed" % (m['info']['type'],m['info']['id']))

        # Add an existing Object to another Object's child
        elif command == "add":
            self.add(m)
            ws.send("msg %s %s added" % (m['info']['type'],m['info']['id']))

        # Remove an existing Object from its parent
        elif command == "remove":
            self.remove(m)
            self.send_all("msg %s removed" % m['id'])
        
        # Give client an unused id
        elif command == "gid":
            ws.send(json.dumps({'command':"gid",'id':self.id_iter.next()}))

        #TODO
        # Save the graph to disk
        elif command == "save":
            pass

        # Load the graph from disk
        elif command == "load":
            pass

        # Let the object handle other messages
        else:
            self.objs[m['id']].recive_message(m,ws)

    def send_direct_message(_, message):
        '''
        This will be overwritten by server.py
        '''

    def send_buffered_message(self,id,command,content = ''):
        '''
        new - create a demo node
        out - output
        clr - clear output
        atr - set attribute
        '''

        priority = 5 # the larger the higher, 0~9
        if command == 'new':
            priority = 8
        
        k=str(9-priority)+command+"/"+str(id)
        if command == 'new':
            k+='/'+content['type']
        if command == 'atr':
            k+='/'+content

        if command == 'out':
            if k not in self.message_buffer:
                self.message_buffer[k] = ''
            self.message_buffer[k]+=content
        elif command == 'clr':
            self.message_buffer[str(9-priority)+"out/"+id] = ''
            self.message_buffer[k]=content
        else:
            self.message_buffer[k]=content

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
            new_instance = c(d,self,is_new,parent)
        else:
            new_instance = c(d,self,is_new)
            parent = d['attributes']['parent_id']

        self.objs.update({id:new_instance})
        self.objs[parent].catch_history('create',new_instance.serialize())

        self.objs[parent].OnChildCreated(new_instance)

    def destroy(self,d):
        parent_id = self.objs[d['id']].parent_id.value
        self.objs[parent_id].OnChildDestroyed(d['id'])
        self.objs[d['id']].OnDestroy()
        self.objs.pop(d['id'])

    def get_co_ancestor(self,objs) -> Object:
        '''
        return the nearest common ancestor of multiple objects
        '''
        min_len = 10000000000000000
        parent_lists = []
        for o in objs:
            parent_list = []
            parent_list.append(o)
            while o.id != 0:
                o = self.objs[o.parent_id.value]
                parent_list.append(o)
            parent_lists.append(reversed(parent_list))
            min_len = min(len(parent_list), min_len)

        last = self.base_obj
        for i in range(min_len):
            current = parent_lists[0][i]
            for j in range(1,len(parent_lists)):
                if current != j:
                    return last
            last = current

        return last
    # run in another thread from the main thread (server.py)
    def run(self):
        raise NotImplementedError()