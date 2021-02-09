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
        info: {name,position,code,output}
        '''
        self.name=info['name']
        self.position=info['pos']
        self.code=info['code']if 'code' in info else ''
        self.output=info['output']if 'output' in info else ''
        self.env=env
        self.env.Update_history("new",{"name":self.name,"pos":self.position})
        self.env.latest_history.name=self.name
        self.first_history=self.latest_history=History_item("stt")
        # currently there'll be only stt and cod in node history

    def move(self,pos):
        self.env.Update_history("mov",{"name":self.name,"old":self.position,"new":pos})# node moves are logged in env history
        self.position=pos

    def set_code(self,code):
        self.Update_history("cod",{"name":self.name,"old":self.code,"new":code}) # code changes are logged in node history
        self.code=code

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

import queue

# the environment to run the code in
class Env():

    def __init__(self,name):
        self.name=name
        self.thread=None
        self.tasks = queue.Queue()
        self.nodes={} # {id : Node}
        self.globals=globals()
        self.locals={}
        self.first_history=self.latest_history=History_item("stt") # first history item
    

    def Update_history(self,type,content):
        '''
        type, content:
        stt, None - start the environment
        new, {name, pos:[x,y,z]} - new node
        mov, {name, old:[oldx,oldy,oldz], new:[newx,newy,newz]} - move node
        '''

        # don't repeat mov history
        if type=="mov" and self.latest_history.type=="mov" and content['name'] == self.latest_history.content['name']:
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

    # run in another thread from the main thread (server.py)
    def run(self):
        self.flag_running = True
        while self.flag_running:
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