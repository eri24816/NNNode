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
def send_all(ws_list,message):
    for ws in ws_list:
        ws.send(json.dumps(message))

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
        self.id_iter = num_iter(-1)
        self.ws_clients = []

        # unlike history, some types of changes aren't necessary needed to be updated sequentially on client, like code in a node or whether the node is running.
        # one buffer per client
        # format:[ { "<command>/<node id>": <value> } ]
        # it's a dictionary so replicated updates will overwrite
        self.message_buffer = {}
        
        self.base_obj : Object = base_obj_class()
        
    def recive_message(self,message):
        m=json.loads(message) # message is in Json
        command=m['command']
        print('-- client:\t',m)
        
        if command == "udo":
            '''
            undo  
            {command:"udo",id}
            if id=='', apply undo on space
            '''
            id= m['id']
            if id=='':
                if self.Undo():
                    send_all("msg space undone" )
                else:
                    send_all("msg  noting to undo" )
            else:
                if id in self.objs:
                    with Space.History_lock(self.objs[id]):
                        if self.objs[id].Undo():
                            send_all("msg obj %s undone" % id)
                        else:
                            send_all("msg obj %s noting to undo" % id)
                else:
                    send_all("err no such obj %s" % id)
                
        elif command == "rdo":
            '''
            redo
            if id=='', apply redo on space
                {command:"rdo",id}
            '''
            id= m['id']
            if id=='':
                if self.Redo():
                    send_all("msg space redone" )
                else:
                    send_all("msg  noting to redo" )
            else:
                if id in self.objs:
                    with Space.History_lock(self.objs[id]):
                        if self.objs[id].Redo():
                            send_all("msg obj %s redone" % id)
                        else:
                            send_all("msg obj %s noting to redo" % id)
                else:
                    send_all("err no such obj %s" % id)

        
        elif command == "gid":
            '''
            give client an unused id
            '''
            send_all(json.dumps({'command':"gid",'id':self.id_iter.next()}))
        #TODO
        elif command == "sav":
            '''
            save the graph to disk
            '''
       
        elif command == "lod":
            '''
            load the graph from disk
            '''

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
            
    def Update_history(self,type,content):
        '''
        When an attribute changes or something is created or removed, this will be called.
        If such action is caused by undo/redo, history.lock() prevents history.Update() working.
        '''
        # don't repeat atr history within 2 seconds 
        if type=="atr" and self.history.current.type=="atr" and content['id'] == self.history.current.content['id']:
            if (time.time() - self.history.current.time)<2:
                self.history.current.content['new']=content['new']
                self.history.current.version+=1
                return

        self.history.Update(type,content)

    def Undo(self):
        if self.history.current.last==None:
            return 0 # noting to undo

        # undo
        self.history.current.direction = -1
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
            return 0 # noting to redo

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
        

    # run in another thread from the main thread (server.py)
    def run(self):
        raise NotImplementedError()