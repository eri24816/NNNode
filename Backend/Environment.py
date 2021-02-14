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
        print(eval(compile(ast.Expression(body=stmts[-1].value), filename="<ast>", mode="eval"), globals, locals))
    else:    
        exec(script, globals, locals)
    return output


        
        
import datetime
class History_item:
    '''
    works as a linked list
    '''
    def __init__(self,type,content={},last=None):
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
        
        self.time=datetime.datetime.now()
        if self.last !=None:
            self.last.next=self
            self.last.head_direction=1 # self is head

class Node:
    def __init__(self,info,env):
        '''
        create a node
        '''
        self.type=info['type']
        self.name=info['name']
        self.pos=info['pos']
        self.env=env
        self.env.Update_history("new",info)
        # self.env.latest_history.name=self.name #*?   what is this line for? 
        self.first_history=self.latest_history=History_item("stt")
        # currently there will be only stt and cod in node history

    @abstractmethod
    def get_info(self):
        pass

    def move(self,pos):
        self.env.Update_history("mov",{"name":self.name,"old":self.pos,"new":pos})# node moves are logged in env history
        self.pos=pos

    def Update_history(self,type,content):
        '''
        type, content:
        stt, None - create the node
        cod, {name, old, new} - change code
        '''
        # don't repeat cod history    
        if type=="cod" and self.latest_history.type=="cod" and content['name'] == self.latest_history.content['name']:
            if (datetime.datetime.now() - self.latest_history.time).seconds<3:
                self.latest_history.content['new']=content['new']
                self.latest_history.version+=1
                return

        # add an item to the linked list
        self.latest_history=History_item(type,content,self.latest_history)

    def Undo(self):
        if self.latest_history.last==None:
            return 0 # noting to undo
        self.latest_history.head_direction=-1
        self.latest_history=self.latest_history.last
        self.latest_history.head_direction=0
        return 1
    
    def Redo(self):
        if self.latest_history.next==None:
            return 0 # noting to redo
        self.latest_history.head_direction=1
        self.latest_history=self.latest_history.next
        self.latest_history.head_direction=0
        return 1

    def remove(self):
        self.env.Update_history("rem",self.get_info())
        del self #*?



class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.
    A CodeNode can be invoked either by a input control flow or by client(e.g. double click on the node) .
    After running, it will activate its output control flow (if there is one).
    '''
    def __init__(self,info,env):
        '''
        info: {type=CodeNode,name,pos,code,output}
        '''
        super().__init__(info,env) #*? usage of super?
        self.code=info['code']if 'code' in info else ''
        self.output=info['output']if 'output' in info else ''

        self.in_control=None # only 1 in_control is allowed
        self.out_control=[] # there can be more than one out_control

    def get_info(self):
        return {"type":"CodeNode","name":self.name,"pos":self.pos,"code":self.code,"output":self.output}

    def set_code(self,code):
        # code changes are logged in node history
        self.Update_history("cod",{"name":self.name,"old":self.code,"new":code}) 
        self.code=code

    def remove(self):
        # remove all conneced edges
        if self.in_control:
            self.in_control.remove()
        for i in self.out_control:
            i.remove()
        super().remove()


class FunctionNode(Node):
    '''
    Similar to CodeNode, but a FunctionNode's code defines a function (start with "def").
    The function is invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows and ControlFlow (if there is one).
    '''
    def __init__(self,info,env):
        '''
        info: {type=FunctionNode,name,pos,code,output,in_data,out_data}
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
        return {"type":"FunctionNode","name":self.name,"pos":self.pos,"code":self.code,"output":self.output,"in_data":in_data,"out_data":out_data}

    def set_code(self,code):
        # code changes are logged in node history
        self.Update_history("cod",{"name":self.name,"old":self.code,"new":code})
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
        self.name=info['name']
        self.tail=env.nodes[info['tail']]
        self.head=env.nodes[info['head']]
        self.info=info # for remove history
        self.env.Update_history("new",info)
    
    def get_info(self):
        return self.info

    def remove(self):
        self.env.Update_history("rem",self.info)
        del self #*?

class DataFlow(Edge):
    def __init__(self,info,env):
        '''
        info: {type=DataFlow,name,tail,head,tail_var,head_var}
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
        info: {type=ControlFlow,name,tail,head}
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

import queue

# the environment to run the code in
class Env():

    def __init__(self,name):
        self.name=name
        self.thread=None
        self.tasks = queue.Queue()
        self.nodes={} # {name : Node}
        self.edges={} # {name : Edge}
        self.globals=globals()
        self.locals={}
        self.first_history=self.latest_history=History_item("stt") # first history item
        self.lock_history=False # when undoing or redoing, lock_history set to True to avoid unwanted history change
    
    def Create(self,info): # create any type of node or edge
        # info: {name, type, ...}
        new_instance=globals()[info['type']](info,self)
        name=info['name']
        
        
        if info['type']=="DataFlow" or info['type']=="ControlFlow":
            assert name not in self.edges
            self.edges.update({name:new_instance})
        else:
            assert name not in self.nodes
            self.nodes.update({name:new_instance})

    def Remove(self,info): # remove any type of node or edge
        if info['name'] in self.edges:
            self.edges[info['name']].remove()
            self.edges.pop(info['name'])
        else:
            self.nodes[info['name']].remove()
            self.nodes.pop(info['name'])
    def Move(self,name,pos):
        self.nodes[name].move(pos)

    def Update_history(self,type,content):
        '''
        type, content:
        stt, None - start the environment
        new, info - new node or edge
        mov, {name, old:[oldx,oldy,oldz], new:[newx,newy,newz]} - move node
        rem, info - remove node or edge
        '''
        if self.lock_history:
            return
        # don't repeat mov history
        if type=="mov" and self.latest_history.type=="mov" and content['name'] == self.latest_history.content['name']:
            if (datetime.datetime.now() - self.latest_history.time).seconds<3:
                self.latest_history.content['new']=content['new']
                self.latest_history.version+=1
                return

        # add an item to the linked list
        self.latest_history=History_item(type,content,self.latest_history)

    class History_lock():
        def __init__(self,env):
            self.env=env
        def __enter__(self):
            self.env.lock_history=True
        def __exit__(self, type, value, traceback):
            self.env.lock_history=False
    
    #TODO: handle undo and redo
    def Undo(self):
        if self.latest_history.last==None:
            return 0 # noting to undo
        # undo
        with self.History_lock(self):
            type=self.latest_history.type
            content=self.latest_history.content

            if type=="new":
                self.Remove(content)
            elif type=="rem":
                self.Create(content)
            elif type=="mov":
                self.Move(content['name'],content['old'])

        self.latest_history.head_direction=-1
        self.latest_history=self.latest_history.last
        self.latest_history.head_direction=0
        return 1

    def Redo(self):
        if self.latest_history.next==None:
            return 0 # noting to redo
        self.latest_history.head_direction=1
        self.latest_history=self.latest_history.next
        self.latest_history.head_direction=0

        with self.History_lock(self):
            type=self.latest_history.type
            content=self.latest_history.content
            
            if type=="new":
                self.Create(content)
            elif type=="rem":
                self.Remove(content)
            elif type=="mov":
                self.Move(content['name'],content['new'])

        return 1

    # run in another thread from the main thread (server.py)
    def run(self):
        self.flag_running = True
        while self.flag_running:
            






            '''
            self.task = None # task = None when idle
            self.task = self.tasks.get() # wait for a task
            output=''
            try:
                with stdoutIO() as s:
                    exec_(self.nodes[self.task].code,self.globals,self.locals) # run the code in the Node
                    output = s.getvalue()
            except Exception:
                output += traceback.format_exc()
            self.nodes[self.task].output=output
            print(output)
            '''