from __future__ import annotations
from edges import Edge
from typing import Dict, List
from history import *
import datetime
import sys
import traceback
from typing import TYPE_CHECKING, TypedDict
if TYPE_CHECKING:
    import Environment
    import edges

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
    
    display_name = 'Node'

    class Info(TypedDict):
        type : str
        id : int

        # for client ------------------------------
        frontend_type : str # RectNode, RoundNode
        name : str          # Just for frontend. In backend we never use it but use "id" instead
        pos : List[float]   # While some nodes are not draggable
        output : str        # Output is necessary for all classes of node to at least store exceptions occured in nodes

    @classmethod
    def get_class_info(cls)->Info:
        tempNode = cls({'id' : -1})
        return tempNode.get_info()

    def get_info(self) -> Dict[str]:
        return {
            "type":type(self).__name__,"id":self.id,"name":self.display_name,"pos":self.pos,"output":self.output,'frontend_type' : self.frontend_type,
        'ports' : [port.get_dict for port in self.port_list]
        }
    
    class Port():
        '''
        Different node classes might have Port classes that own different properties for frontend to read. 
        In such case, inherit this
        '''
        def __init__(self, type : str, isInput : bool, max_connections : int = '64', name : str = '', discription : str = ''):
            self.type = type
            self.isInput = isInput
            self.max_connections = max_connections
            self.name = name
            self.discription = discription 
            self.connections : List[edges.ControlFlow] = [] 

        def get_dict(self):
            # for json.dump
            return {'type':self.type, 'isInput' : self.isInput, 'max_connections' : self.max_connections, 'name': self.name, 'discription' : self.discription}

    def __init__(self, info : Info, env : Environment.Env):
        '''
        Base class of node
        '''
        # The environment
        self.env=env

        # The flows connected to the node
        '''
        self.in_control : List[edges.ControlFlow] = []
        self.out_control : List[edges.ControlFlow] = []
        self.in_data : List[List[edges.DataFlow]] = []
        self.out_data : List[List[edges.DataFlow]] = []
        '''

        # For the API, each ports of the node are identified by position in this list
        # Create ports in __init__ then add all port into this list
        self.port_list : List[self.Port] = []

        # Is the node ready to run?
        self.active = False

        # for client ------------------------------
        self.type=type(self).__name__
        self.id=info['id']
        self.pos=info['pos'] if 'pos' in info else [0,0,0]
        self.output = info['output'] if 'output' in info else ''

        # Class name in C#
        self.frontend_type : str

        # added lines of output when running, which will be sent to client
        self.added_output = "" 

        if self.id != -1:
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

        # Redirect printed outputs and error messages to client
        with stdoutIO(self):
            self._run()
        
        self.flush_output()

    def _run(self):
        # Actually define what the type of node acts
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

    display_name = 'Code Node'

    class Info(Node.Info):
        code : str

    def __init__(self,info : Info ,env : Environment.Env):
        super().__init__(info,env)
        self.name=info['name']
        self.frontend_type = 'CodeNode'

        self.in_control = self.Port('ControlPort', True)
        self.out_control = self.Port('ControlPort', True)
        self.port_list = [self.in_control, self.out_control]

        self.code=info['code']if 'code' in info else ''

    def in_control_active(self):
        # The node is activated as soon as its input flow is activated
        self.activate()
        
    def activate(self):
        super().activate()

    def _run(self):
        if self.in_control.__len__()>0:
            self.in_control.connections[0].deactivate()

        fail = False
        try:
            exec_(self.code,self.env.globals,self.env.locals)
        except Exception:
            self.added_output += traceback.format_exc()
            fail = True

        if not fail:
            for flow in self.out_control.connections:
                flow.activate()
        self.deactivate()

    # For client -----------------------------
    def recive_command(self, m):
        super().recive_command(m)
        command = m['command']
        if command =='cod':
            self.set_code(m['info'])

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

class EvalNode(CodeNode):

    def _run(self):
        fail = False
        try:
            result = eval(self.code,self.env.globals,self.env.locals)
        except Exception:
            self.added_output += traceback.format_exc()
            fail = True

        if not fail:
            for flow in self.out_data[0]:
                flow.recive_value(result)
        self.deactivate()

class FunctionNode(Node):
    '''
    Similar to CodeNode, but a FunctionNode's code defines a function (start with "def").
    The function is invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows and ControlFlow (if there is one).
    '''

    display_name = 'Function Node'
    frontend_type = 'FunctionNode'

    class Info(Node.Info):
        in_names : List[str]
        out_names : List[str]
        allow_multiple_in_data : List[bool]

    # Most of the child classes of FunctionNode just differ in following three class properties and their function() method.
    in_names : List[str] = []
    out_names : List[str] = []
    allow_multiple_in_data : List[bool] = []

    

    def __init__(self, info : Info ,env : Environment.Env):
        '''
        info: {type=FunctionNode,id,name,pos,output}
        '''
        super().__init__(info,env)

        self.in_data = [self.Port('DataPort',True,name = port_name)for port_name in self.in_names]
        self.out_data = [self.Port('DataPort',True,name = port_name)for port_name in self.in_names]
        self.port_list = self.in_data + self.out_data

    def in_control_active(self):
        # TODO: Ask for value
        pass

    def in_data_active(self):
        # A FunctionNode activates when all its input dataFlow is active.
        for port in self.in_data:
            for flow in port.connections:
                if not flow.active:
                    return
        self.activate()

    def activate(self):
        super().activate()

        # TODO: Ask for value
        # Or should it be put in _run() ?
        for port in self.in_data:
            for flow in port.connections:
                if not flow.has_value:
                    pass


    def _run(self):
        for port in self.in_data:
            for flow in port.connections:
                flow.deactivate()

        # Gather data from input dataFlows
        funcion_input = []
        for port in self.in_data:
            if len(port.connections) == 0:
                funcion_input.append(None) #TODO: Default value
            elif len(port.connections) == 1:
                funcion_input.append(port[0].data)
            else:
                # More than 1 input flow on the port
                funcion_input.append([flow.data] for flow in port)
        
        # Evaluate the function
        result = self.function(*funcion_input)

        # Send data to output dataFlows
        i = 0
        for result_item in result:
            for flow in self.out_data[i].connections:
                flow.recive_value(result_item)
            i+=1
        
        self.deactivate()

    @staticmethod
    def function():
        # Inherit FunctionNode to write different function.
        # If one input port has more than one dataFlows connected, their data will input to this function as a list.
        pass

