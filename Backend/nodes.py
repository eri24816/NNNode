from __future__ import annotations
from typing import List
from history import *
import datetime
import sys
import traceback
from typing import TYPE_CHECKING
if TYPE_CHECKING:
    import Environment

class node_StringIO():
    def __init__(self,node):
        self.node = node
    def write(self,value):
        self.node.added_output += value

# redirect stdout
class stdoutIO():
    def __init__(self,node):
        self.node = node
    def __enter__(self):
        self.old = sys.stdout
        stdout = node_StringIO(self.node)
        sys.stdout = stdout
        return stdout
    def __exit__(self, type, value, traceback):
        sys.stdout = self.old

import ast
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


class Node:
    def __init__(self,info,env : Environment.Env):
        '''
        Base class of node
        '''
        # The environment
        self.env=env

        # The flows connected to the node
        self.in_control : List[ControlFlow] = []
        self.out_control : List[ControlFlow] = []
        self.in_data : List[List[DataFlow]] = []
        self.out_data : List[List[DataFlow]] = []

        # Is the node ready to run?
        self.active = False

        # for client ------------------------------
        self.type=info['type']
        self.id=info['id']
        self.name=info['name']
        self.pos=info['pos']
        self.output = info['output'] if 'output' in info else ''

        # Data port names
        self.in_ports : List[str] = []
        self.out_ports : List[str] = []

        self.added_output = "" # added lines of output when running, which will be sent to client

        self.env.Update_history("new", info)
        self.first_history = self.latest_history = History_item("stt")
        self.lock_history=False

    def in_control_active(self):
        # check if the condition is satisfied that the node can be activated
        pass

    def in_data_active(self):
        # check if the condition is satisfied that the node can be activated
        pass

    def activate(self):
        if self.active: 
            return # prevent duplication in env.nodes_to_run
        self.active = True
        self.env.nodes_to_run.put(self)
        
        # for client ------------------------------
        self.env.Write_update_message(self.id, 'act', '1')  # 1 means "pending" (just for client to display)

    def deactivate(self):
        self.active = False

        # for client ------------------------------
        self.env.Write_update_message(self.id, 'act', '0')

    def flush_output(self): # called when client send 'upd'
        if self.added_output == '':
            return
        self.output += self.added_output
        self.env.Write_update_message(self.id, 'out', self.added_output) # send client only currently added lines of output
        self.added_output = ''

    def run(self):
        # Env calls this method
        self.env.Write_update_message(self.id,'act','2') # 2 means "running"
        self.env.Write_update_message(self.id, 'clr') # Clear output
        try:
            with stdoutIO(self):
                self._run()
        except Exception:
            self.added_output += traceback.format_exc()
        self.flush_output()

    def _run(self):
        # Actually do what the node does
        pass


    # for client ------------------------------
    def recive_command(self,m):
        command = m['command']
        if command == "act":
            self.activate()

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
        for flow in self.in_control:
            flow.remove()
        for flow in self.out_control:
            flow.remove()
        for port in self.in_data:
            for flow in port:
                flow.remove()
        for port in self.out_data:
            for flow in port:
                flow.remove()
        self.env.nodes.pop(self.id)
        self.env.Update_history("rmv",self.get_info())
        del self  #*?
    




class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.

    The node will be invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    It will just run the code in it.
    After running, it will activate its output dataflows and ControlFlow (if there is one).
    '''
    def __init__(self,info,env):
        super().__init__(info,env)
        self.code=info['code']if 'code' in info else ''

    def get_info(self):
        return {"type":"CodeNode","id":self.id,"name":self.name,"pos":self.pos,"code":self.code,"output":self.output}

    def in_control_active(self):
        # A code node can only have one input flow, so the node is activated as soon as its input flow is activated
        self.activate()
        
    def activate(self):
        super().activate()
        if self.in_control.__len__()>0:
            self.in_control[0].deactivate()

    def _run(self):
        exec_(self.code,self.env.globals,self.env.locals)
        if self.out_control.__len__()>0:
            self.out_control[0].activate()
        self.deactivate()

    # For client -----------------------------
    def recive_command(self, m):
        super().recive_command(m)
        command = m['command']
        if command =='cod':
            self.set_code(m['value'])
        

    def set_code(self,code):
        # code changes are logged in node history
        self.code=code
        self.Update_history("cod",{"id":self.id,"old":self.code,"new":code}) 
        self.env.Write_update_message(self.id,'cod','')

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


class Edge(): # abstract class
    def __init__(self,info,env):
        '''
        create an edge connecting two nodes
        '''
        self.env=env

        self.tail : Node = env.nodes[info['tail']]
        self.head : Node = env.nodes[info['head']]        

        self.active=False

        # for client --------------------------------
        self.info = info  # for remove history
        self.type=info['type']
        self.id=info['id']
        self.env.Update_history("new", info)

    def get_info(self):
        return self.info

    

    def activate(self):
        self.active = True
        #TODO: inform client to play animations

    def deactivate(self):
        self.active = False
        #TODO: inform client to play animations

def remove(self):
        self.env.edges.pop(self.id)
        self.env.Update_history("rmv", self.info)
        del self  #*?

class DataFlow(Edge):
    def __init__(self,info,env):
        '''
        info: {type=DataFlow,id,tail,head,tail_var,head_var}
        '''
        super().__init__(info,env)
        self.active=False
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

        # Connect to the tail and head node
        self.tail.out_control.append(self)
        self.head.in_control.append(self)

    def remove(self):
        self.tail.out_control.remove(self)
        self.head.in_control.remove(self)
        super().remove()

    def activate(self):
        super().activate()
        # inform the head node
        self.head.in_control_active()

