from abc import abstractmethod
import sys
from io import StringIO
import ast
import traceback

# redirect stdout
class stdoutIO():
    def __enter__(self):
        self.old = sys.stdout
        stdout = StringIO()
        sys.stdout = stdout
        return stdout
    def __exit__(self, type, value, traceback):
        sys.stdout = self.old

# exec that prints correctly
def exec_(script,globals=None, locals=None):
    stmts = list(ast.iter_child_nodes(ast.parse(script)))
    output=''
    if not stmts:
        return
    if isinstance(stmts[-1], ast.Expr):
        if len(stmts) > 1:
            ast_module = ast.parse("")
            ast_module.body=stmts[:-1]
            exec(compile(ast_module, filename="<ast>", mode="exec"), globals, locals)
        last = eval(compile(ast.Expression(body=stmts[-1].value), filename="<ast>", mode="eval"), globals, locals)
        if last:
            print(last)
    else:    
        exec(script, globals, locals)
    return output


        
        
import datetime
class History_item:
    '''
    works as a linked list
    '''
    def __init__(self,type,content={},last=None,sequence_id=-1):
        self.type=type
        self.content=content
        self.last=last
        self.next=None
        self.head_direction=0 
        self.version=0 # sometimes we can increase version instead of create a new history_item to save memory
        '''
        head_direction is for client to find the head (server/upd)
        it changes when edit/undo/redo
        -1: backward, 0: self is head, 1: forward
        '''#*? Is it OK to del the history_items that will no longer be visited while some clients are still there?
        
        self.sequence_id = sequence_id
        self.time=datetime.datetime.now()
        if self.last !=None:
            self.last.next=self
            self.last.head_direction = 1  # self is head
    def __str__(self):
        result=self.type+", "+str(self.content)
        if self.last:
            result = str(self.last) + '\n' + result
        return result

class History_lock():
    def __init__(self,o):
        self.o=o
    def __enter__(self):
        self.o.lock_history=True
    def __exit__(self, type, value, traceback):
        self.o.lock_history = False
        
class Node:
    def __init__(self,info,env):
        '''
        create a node
        '''
        self.type=info['type']
        self.id=info['id']
        self.name=info['name']
        self.pos=info['pos']
        self.env=env
        self.env.Update_history("new", info)
        self.first_history = self.latest_history = History_item("stt")
        self.lock_history=False
        self.running=False

    @abstractmethod
    def get_info(self):
        pass

    def move(self,pos):
        self.env.Update_history("mov",{"id":self.id,"old":self.pos,"new":pos})# node moves are logged in env history
        self.pos=pos

    def Update_history(self, type, content):
        '''
        type, content:
        stt, None - create the node
        cod, {name, old, new} - change code
        '''
        if self.lock_history:
            return
        # don't repeat cod history within 3 seconds 
        if type=="cod" and self.latest_history.type=="cod" and content['id'] == self.latest_history.content['id']:
            if (datetime.datetime.now() - self.latest_history.time).seconds<3:
                self.latest_history.content['new']=content['new']
                self.latest_history.version+=1
                return

        # add an item to the linked list
        self.latest_history=History_item(type,content,self.latest_history)
        

    def Undo(self):
        
        if self.latest_history.last==None:
            return 0 # nothing to undo
        self.latest_history.head_direction=-1
        self.latest_history=self.latest_history.last
        self.latest_history.head_direction = 0
        return 1
    
    def Redo(self):
        if self.latest_history.next==None:
            return 0 # nothing to redo
        self.latest_history.head_direction=1
        self.latest_history=self.latest_history.next
        self.latest_history.head_direction=0
        return 1

    def remove(self):
        self.env.nodes.pop(self.id)
        self.env.Update_history("rmv",self.get_info())
        del self  #*?
    
    @abstractmethod
    def on_input_flow_activated(self):
        # check if the condition is satisfied that the node can be activated (e.g. all its input flows is activated)
        pass

    def activate(self):
        self.activated = True
        #TODO: inform client to play animations

    def deactivate(self):
        self.activated = False
        #TODO: inform client to play animations


class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.
    A CodeNode can be invoked either by an input control flow or by client(e.g. double click on the node) .
    After running, it will activate its output control flow (if there is one).
    '''
    def __init__(self,info,env):
        '''
        info: {type=CodeNode,id,name,pos,code,output}
        '''
        super().__init__(info,env) #*? usage of super?
        self.code=info['code']if 'code' in info else ''
        self.output=info['output']if 'output' in info else ''

        self.in_control=None # only 1 in_control is allowed
        self.out_control = []  # there can be more than one out_control
        
        self.already_activated=0

    def get_info(self):
        return {"type":"CodeNode","id":self.id,"name":self.name,"pos":self.pos,"code":self.code,"output":self.output}

    def set_code(self,code):
        # code changes are logged in node history
        self.Update_history("cod",{"id":self.id,"old":self.code,"new":code}) 
        self.env.Write_update_message(self.id,'cod','')
        self.code=code

    def remove(self):
        # remove all conneced edges
        if self.in_control:
            self.in_control.remove()
        for i in self.out_control:
            i.remove()
        super().remove()

    def Undo(self):
        if self.latest_history.last:
            if self.latest_history.type=="cod":
                self.set_code(self.latest_history.content['old'])
        return super().Undo()

    def Redo(self):
        if super().Redo():
            if self.latest_history.type=="cod":
                self.set_code(self.latest_history.content['new'])
            return 1
        return 0

    '''
    graph stuff 
    '''
    def on_input_flow_activated(self):
        # A code node can only have one input flow, so the node is activated as soon as its input flow is activated
        self.activate()
        self.in_control.deactivate()

    def activate(self):
        super().activate()
        if self.already_activated: 
            return # prevent duplication in env.nodes_to_run
        self.env.nodes_to_run.put(self)
        self.env.Write_update_message(self.id, 'act', '1')  # 1 means "pending"
        self.already_activated=1

    def start_running(self):
        self.env.Write_update_message(self.id,'act','2') # 2 means "running". This method is just for UI
    
    def finish_running(self,output):
        self.deactivate()
        for out_control in self.out_control:
            out_control.activate()
        self.env.Write_update_message(self.id, 'act', '0')
        self.env.Write_update_message(self.id, 'out', output)
        self.already_activated=0


class FunctionNode(Node):
    '''
    Similar to CodeNode, but a FunctionNode's code defines a function (start with "def").
    The function is invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows and ControlFlow (if there is one).
    '''
    def __init__(self,info,env):
        '''
        info: {type=FunctionNode,id,name,pos,code,output,in_data,out_data}
        '''
        super().__init__(info,env)
        self.code=info['code']if 'code' in info else ''
        self.output=info['output']if 'output' in info else ''
        self.in_data={}
        if 'in_data' in info:
            for i in info['in_data']:
                self.in_data.update({i:None})
        self.out_data={}
        if 'out_data' in info:
            for i in info['out_data']:
                self.out_data.update({i:[]})
        '''
        in_data,  { <var_name> : <connected dataflow > }
        out_data: { <var_name> : [<connected dataflow 1>,<connected dataflow 2>...] }
        In a graph file, the connections of nodes with dataflows are stored in dataflows,
          so <connected dataflow> is None here.
        '''
        self.in_control=None # only 1 in_control is allowed
        self.out_control=[] # there can be more than one out_control

    def get_info(self):
        in_data=[i for i in self.in_data] # get keys
        out_data=[i for i in self.out_data] # get keys
        return {"type":"FunctionNode","id":self.id,"name":self.name,"pos":self.pos,"code":self.code,"output":self.output,"in_data":in_data,"out_data":out_data}

    def set_code(self,code):
        # code changes are logged in node history
        self.Update_history("cod",{"id":self.id,"old":self.code,"new":code})
        self.code=code

    def remove(self):
        # remove all conneced edges
        if self.in_control:
            self.in_control.remove()
        for i in self.out_control:
            i.remove()
        for i in self.in_data:
            if self.in_data[i]:
                self.in_data[i].remove()
        for i in self.out_data:
            for j in self.out_data[i]:
                j.remove()
        super().remove()


class Edge(): # abstract class
    def __init__(self,info,env):
        '''
        create an edge connecting two nodes
        '''
        self.type=info['type']
        self.id=info['id']
        self.tail=env.nodes[info['tail']]
        self.head=env.nodes[info['head']]
        self.info = info  # for remove history
        self.env=env
        self.env.Update_history("new", info)
        self.activated=True
    
    def get_info(self):
        return self.info

    def remove(self):
        self.env.edges.pop(self.id)
        self.env.Update_history("rmv", self.info)
        del self  #*?


    '''
    graph stuff 
    '''        
    def activate(self):
        self.activated = True
        #TODO: inform client to play animations

    def deactivate(self):
        self.activated = False
        #TODO: inform client to play animations


class DataFlow(Edge):
    def __init__(self,info,env):
        '''
        info: {type=DataFlow,id,tail,head,tail_var,head_var}
        '''
        super().__init__(info,env)
        self.activated=False
        self.tail_var=info['tail_var']
        self.head_var=info['head_var']
        self.tail.out_data[self.tail_var].append(self)
        self.head.in_data[self.head_var]=self
        self.data=None
        

    def remove(self):
        self.tail.out_data[self.tail_var].remove(self)
        self.head.in_data[self.head_var]=None 
        super().remove()      

class ControlFlow(Edge):
    def __init__(self,info,env):
        '''
        info: {type=ControlFlow,id,tail,head}
        '''
        super().__init__(info,env)
        self.activated=False

        # connect to tail and head node
        if self.tail.type=='CodeNode' or self.tail.type=='FunctionNode':
            self.tail.out_control.append(self)
        else:
            raise Exception('node type %s not supported'%self.tail.type)

        if self.head.type=='CodeNode' or self.head.type=='FunctionNode':
            assert self.head.in_control==None
            self.head.in_control=self
        else:
            raise Exception('node type %s not supported'%self.tail.type)

    def remove(self):
        self.tail.out_control.remove(self)
        self.head.in_control=None
        super().remove()

    def activate(self):
        super().activate()
        # inform the head node
        self.head.on_input_flow_activated()


import queue

# the environment to run the code in
class Env():

    def __init__(self,name):
        self.name=name
        self.thread=None
       
        self.nodes={} # {id : Node}
        self.edges={} # {id : Edge}
        self.globals=globals()
        self.locals={}
        self.first_history=self.latest_history=History_item("stt") # first history item
        self.lock_history=False # when undoing or redoing, lock_history set to True to avoid unwanted history change
        self.id_num = 0
        
        self.current_history_sequence_id = -1

        # unlike history, some types of changes aren't necessary needed to be updated sequentially on client, like code in a node or whether the node is running.
        # one buffer per client
        # format:[ { "<command>/<node id>": <value> } ]
        # it's a dictionary so replicated updates will overwrite
        self.update_message_buffers = []

        # for run() thread
        self.nodes_to_run = queue.Queue()

    class History_sequence():
        next_history_sequence_id = 0
        def __init__(self,env):
            self.env=env
        def __enter__(self):
            self.env.current_history_sequence_id = self.next_history_sequence_id
            self.next_history_sequence_id += 1
        def __exit__(self, type, value, traceback):
            self.env.current_history_sequence_id = -1   
    
    def Create(self,info): # create any type of node or edge
        # info: {id, type, ...}
        new_instance=globals()[info['type']](info,self)
        id=info['id']
        
        
        if info['type']=="DataFlow" or info['type']=="ControlFlow":
            assert id not in self.edges
            self.edges.update({id:new_instance})
        else:
            assert id not in self.nodes
            self.nodes.update({id:new_instance})

    def Remove(self,info): # remove any type of node or edge
        if info['id'] in self.edges:
            self.edges[info['id']].remove()
            
        else:
            with self.History_sequence(self): # removing a node may cause some edges also being removed. When undoing and redoing, these multiple actions should be done in a sequence.
                self.nodes[info['id']].remove()


    def Move(self,id,pos):
        self.nodes[id].move(pos)

    
    def Update_history(self,type,content):
        '''
        type, content:
        stt, None - start the environment
        new, info - new node or edge
        mov, {id, old:[oldx,oldy,oldz], new:[newx,newy,newz]} - move node
        rem, info - remove node or edge
        '''
        if self.lock_history:
            return
        # don't repeat mov history
        if type=="mov" and self.latest_history.type=="mov" and content['id'] == self.latest_history.content['id']:
            if (datetime.datetime.now() - self.latest_history.time).seconds<3:
                self.latest_history.content['new']=content['new']
                self.latest_history.version+=1
                return

        # add an item to the linked list
        self.latest_history=History_item(type,content,self.latest_history,self.current_history_sequence_id)

    
    def Write_update_message(self, id, name, v):
        k=name+"/"+id
        for buffer in self.update_message_buffers:
            buffer[k]=v

    def Undo(self):
        if self.latest_history.last==None:
            return 0 # noting to undo
        # undo
        with History_lock(self):
            type=self.latest_history.type
            content=self.latest_history.content

            if type == "new":
                if content['id'] in self.nodes:
                    self.latest_history.content=self.nodes[content['id']].get_info()
                self.Remove(content)
                
            elif type=="rmv":
                self.Create(content)
            elif type=="mov":
                self.Move(content['id'],content['old'])

        self.latest_history.head_direction = -1

        seq_id_a = self.latest_history.sequence_id
        
        self.latest_history=self.latest_history.last
        self.latest_history.head_direction = 0

        seq_id_b = self.latest_history.sequence_id
        
        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Undo()

        return 1

    def Redo(self):
        if self.latest_history.next==None:
            return 0 # noting to redo
        self.latest_history.head_direction=1
        self.latest_history=self.latest_history.next
        self.latest_history.head_direction=0

        with History_lock(self):
            type=self.latest_history.type
            content=self.latest_history.content
            
            if type=="new":
                self.Create(content)
            elif type=="rmv":
                self.Remove(content)
            elif type=="mov":
                self.Move(content['id'],content['new'])

        seq_id_a = self.latest_history.sequence_id

        seq_id_b =  self.latest_history.next.sequence_id if self.latest_history.next!=None else -1

        if seq_id_a !=-1 and seq_id_a == seq_id_b:
            self.Redo()

        return 1

    # run in another thread from the main thread (server.py)
    def run(self):
        self.flag_exit = 0
        self.is_busy=0
        while not self.flag_exit:
            node_to_run = self.nodes_to_run.get()
            node_to_run.start_running()
            self.is_busy = 1
            output=""
            try:
                with stdoutIO() as s:
                    exec_(node_to_run.code,self.globals,self.locals) # run the code in the Node
                    output = s.getvalue()
            except Exception:
                output += traceback.format_exc()
            self.is_busy=0
            node_to_run.output = output
            node_to_run.finish_running(output)
            print(output)
            