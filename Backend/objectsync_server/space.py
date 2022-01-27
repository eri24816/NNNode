from __future__ import annotations
from asyncio.queues import Queue
from typing import Dict
from history import *
from collections import deque
from threading import Event
from object import Object

# from server import Send_all
# Above line causes circular import error so just define Send_all() again here.
import json


class num_iter:
    def __init__(self,start=-1):
        self.i=start
    def next(self):
        self.i+=1
        return self.i
# the environment to run the code in
class Space():

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
        self.objs = {0:self.base_obj}       
    
    def send_all(ws_list,message):
        for ws in ws_list:
            ws.send(json.dumps(message))

    def recieve_message(self,message,ws):
        m=json.loads(message) # message is in Json
        command=m['command']
        print('-- client:\t',m)
        if command == "new":
            self.create(m)
            ws.send("msg %s %s created" % (m['info']['type'],m['info']['id']))

        elif command == "rmv":
            self.remove(m)
            self.space.send_all("msg %s removed" % m['id'])
            
        elif command == "udo":
            '''
            undo  
                {command:"udo",id}
            '''
            id= m['id']
            if id in self.objs:
                if self.objs[id].Undo():
                    ws.send("msg obj %s undone" % id)
                else:
                    ws.send("msg obj %s noting to undo" % id)
            else:
                ws.send("err no such obj %s" % id)
                
        elif command == "rdo":
            '''
            redo
                {command:"rdo",id}
            '''
            id= m['id']
            if id in self.objs:
                if self.objs[id].Redo():
                    ws.send("msg obj %s redone" % id)
                else:
                    ws.send("msg obj %s noting to redo" % id)
            else:
                ws.send("err no such obj %s" % id)

        
        elif command == "gid":
            '''
            give client an unused id
            '''
            ws.send(json.dumps({'command':"gid",'id':self.id_iter.next()}))
        #TODO
        elif command == "sav":
            '''
            save the graph to disk
            '''
       
        elif command == "lod":
            '''
            load the graph from disk
            '''
        else:
            self.objs[m['id']].recive_message(m)

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

    def create(self,m):
        d = m['info']
        type,id = d['type'],d['id']

        c = self.obj_classes[type]
        new_instance = c(d,self)
        self.objs.update({id:new_instance})

        parent_id = m['parent'] if 'parent' in m else '0'
        self.objs[parent_id].add_child(m['info'])

    def destroy(self,m):
        parent_id = self.objs[m['id']].parent_id.value
        self.objs[parent_id].remove_child({"id" : m['id']})
        self.objs.pop(m['id'])

    # run in another thread from the main thread (server.py)
    def run(self):
        raise NotImplementedError()