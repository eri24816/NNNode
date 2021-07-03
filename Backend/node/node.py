from __future__ import annotations
import math
import numpy as np
import json
from typing import Dict, List
from history import *
import datetime
import sys
import traceback
import copy
from typing import TYPE_CHECKING, TypedDict
if TYPE_CHECKING:
    import Environment
    import edge

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

def v3(x,y,z):
    return {'x':x,'y':y,'z':z}

class Node:
    '''
    Base class of all types of nodes
    '''
    display_name = 'Node'
    category = ''
    default_attr = {
        'pos':v3(0,0,0)
    }

    class Info(TypedDict):
        type : str
        id : int

        # for client ------------------------------
        frontend_type : str     # CodeNode, FunctionNode, RoundNode
        category : str
        doc : str
        name : str              # Just for frontend. In backend we never use it but use "id" instead
        pos : List[float]       # While some nodes are not draggable e.g. their positions are determined by its adjacent nodes
        output : str            # Output is necessary for all classes of node to at least store exceptions occured in nodes
        portInfos : List[Dict]  # PortInfos are determined by server side Node classes and is used to tell client how to set up ports

    @classmethod
    def get_class_info(cls)->Info:
        '''
        When creating demo node, this is sent to client
        '''
        tempNode = cls({'id' : -1, 'name' : ''},env = None)
        return tempNode.get_info()
    
    def get_info(self) -> Dict[str]:
        '''
        Node info
        History item "new" stores this. When creating new node, this is sent to all clients.
        And when client sends redo, the node can be recreated from it. 
        '''
        return {
            "type":type(self).__name__,"id":self.id,"category" : self.category,"doc":self.__doc__,"name":self.display_name,"pos":self.pos,"output":self.output,'frontend_type' : self.frontend_type,
        'portInfos' : [port.get_dict() for port in self.port_list],'attr': self.attributes
        }
    
    class Port():
        '''
        Different node classes might have Port classes that own different properties for frontend to read. 
        In such case, inherit this
        '''
        def __init__(self, type : str, isInput : bool, max_connections : int = '64', name : str = '', description : str = '',pos = [0,0,0], on_edge_activate = lambda : None, with_order : bool = False):
            self.type = type
            self.isInput = isInput
            self.max_connections = max_connections
            self.name = name
            self.description = description 
            self.pos = pos
            self.on_edge_activate = on_edge_activate
            self.flows : List[edge.ControlFlow] = [] 
            self.with_order = with_order

        def get_dict(self):
            # for json.dump
            return {
                'type':self.type,
                'isInput' : self.isInput,
                'max_connections' : self.max_connections,
                'with_order' : self.with_order,
                'name': self.name,
                'description' : self.description,
                'pos' : v3(*self.pos)
                }

    def __init__(self, info : Info, env : Environment.Env):
        # The environment
        self.env=env

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

        # Each types of node have different attributes. Client can set them by sending "atr" command.
        # Changes of attributes will create update messages and send to clients with "atr" command.
        self.attributes = self.default_attr.copy()
        
        if 'attr' in info:
            self.attributes.update(info['attr'])

        if self.id != -1:
            
            self.env.Update_history("new", self.get_info())
            self.first_history = self.latest_history = History_item("stt")
            self.lock_history=False

            # For technical issues (C# bad), client won't read node attributes("atr") in "new" command. So update node attributes on client additionaly
            for attr_name in self.attributes.keys():
                self.env.Write_update_message(self.id,'atr',attr_name)

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
            try:
                self._run()
            except Exception:
                self.running_finished(False)
                self.added_output += traceback.format_exc()
                self.flush_output()
            else:
                self.running_finished(True)

        

    def _run(self):
        # Actually define what the type of node acts
        pass
    
    def running_finished(self,success = True):
        pass

    # for client ------------------------------
    def recive_command(self,m):
        '''
        {'id',command' : 'act'}
        {'id',command' : 'atr', 'name', 'value'}
        '''
        command = m['command']
        if command == "act":
            self.activate()
        if command =='atr':
            print('\n'+str(m)+'\n')
            self.set_attribute(m['name'],m['value'])
    
    
    def set_attribute(self,attr_name, value):
        # Attribute changes are logged in node history
        self.Update_history("atr",{"id":self.id,"name": attr_name,"old":self.attributes[attr_name],"new":value}) 
        self.attributes.update({attr_name: value})
        print('\n\nset attribute {} set to {}\n self.attribute = {}\n\n'.format(attr_name,value,self.attributes))
        self.env.Write_update_message(self.id,'atr',attr_name)

    def move(self,pos):
        # TODO: take pos as a attribute
        self.env.Update_history("mov",{"id":self.id,"old":self.pos,"new":pos})# node moves are logged in env history
        self.pos=pos

    def Update_history(self, type, content):
        '''
        type, content:
        stt, None - create the node
        atr, {name, old, new} - change attribute
        '''
        if self.lock_history:
            return
        # don't repeat atr history within 3 seconds 
        if type=="atr" and self.latest_history.type=="atr" and content['id'] == self.latest_history.content['id']:
            if (datetime.datetime.now() - self.latest_history.time).seconds<3:
                self.latest_history.content['new']=content['new']
                self.latest_history.version+=1
                return

        # add an item to the linked list
        self.latest_history=History_item(type,content,self.latest_history) 

    def Undo(self):
        if self.latest_history.last==None:
            return 0 # nothing to undo

        if self.latest_history.type=="atr":
            self.set_attribute(self.latest_history.content['name'],self.latest_history.content['old'])
        
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

        if self.latest_history.type=="atr":
            self.set_attribute(self.latest_history.content['name'],self.latest_history.content['new'])

        return 1

    def remove(self):
        for port in self.port_list:
            for flow in copy.copy(port.flows):
                flow.remove()
        self.env.nodes.pop(self.id)
        self.env.Update_history("rmv",self.get_info())
        del self  #*?    

class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.

    The node will be invoked when its input Controlflow is activated or double click on the node.
    It will execute its code and activate its output ControlFlow (if there is one).
    '''

    frontend_type = 'CodeNode'
    category = 'basic'
    display_name = 'Code'

    default_attr = Node.default_attr.copy()
    default_attr.update( {'code':''})
    
    class Info(Node.Info):
        code : str

    def __init__(self,info : Info ,env : Environment.Env):

        self.name=info['name']
        
        super().__init__(info,env)
        
        self.in_control = self.Port('ControlPort', True, on_edge_activate = self.in_control_activate, pos = [-1,0,0])
        self.out_control = self.Port('ControlPort', False, pos = [1,0,0])
        self.port_list = [self.in_control, self.out_control]
       

    def get_info(self) -> Dict[str]:
        t = super().get_info()
        t.update({'attr':self.attributes})
        return t

    def in_control_activate(self):
        # The node is activated as soon as its input flow is activated
        self.activate()
        
    def activate(self):
        super().activate()

    def _run(self):
        for flow in self.in_control.flows:
            flow.deactivate()

        exec_(self.attributes['code'],self.env.globals,self.env.locals)

    def running_finished(self, success = True):
        self.flush_output()
        if success:
            for flow in self.out_control.flows:
                flow.activate()
        self.deactivate()

    # For client -----------------------------


class EvalAssignNode(CodeNode):

    frontend_type = 'SimpleCodeNode'
    category = 'basic'
    display_name = 'Evaluate or Assign'

    def __init__(self, info: Node.Info, env: Environment.Env):
        super().__init__(info, env)
        self.in_data = self.Port('DataPort',True,64,pos = [-1,0,0],on_edge_activate = self.in_data_active)
        self.out_data = self.Port('DataPort',False,64,pos = [1,0,0])
        self.port_list = [self.in_data, self.out_data]

    def in_data_active(self):
        #TODO : Check if in data is empty and ask for value
        self.activate()

    def _run(self):
        if len(self.in_data.flows)>0:
            for flow in self.in_data.flows:
                flow.deactivate()

            if len(self.in_data.flows) == 1:
                self.value = self.in_data.flows[0].data
            else:
                self.value = []
                for flow in self.in_data.flows:
                    self.value.append(flow.data)
            #exec_(self.attributes['code'] + " = __value", self.env.globals, {'__value' : self.value})
            self.env.globals.update({self.attributes['code']: self.value})
        else:
            self.value = eval(self.attributes['code'],self.env.globals,self.env.locals)
        

    def running_finished(self, success = True):
        if success:
            for flow in self.out_data.flows:
                flow.recive_value(self.value)
        self.deactivate()
        
class FunctionNode(Node):
    '''
    Similar to CodeNode, but a FunctionNode's code defines a function (start with "def").
    The function is invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows and ControlFlow (if there is one).
    '''

    display_name = 'Function'
    category = 'function'
    frontend_type = 'FunctionNode'

    class Info(Node.Info):
        in_names : List[str]
        out_names : List[str]
        allow_multiple_in_data : List[bool]

    # Most of the child classes of FunctionNode just differ in following 4 class properties and their function() method.
    frontend_type = 'FunctionNode' # FunctionNode, RoundNode
    in_names : List[str] = []
    out_names : List[str] = []
    max_in_data : List[int] = []

    def __init__(self, info : Info ,env : Environment.Env):
        '''
        info: {type=FunctionNode,id,name,pos,output}
        '''
        super().__init__(info,env)
        if self.frontend_type == 'RoundNode':
            in_port_pos = [[math.cos(t),math.sin(t),0.0] for t in np.linspace(np.pi/2,np.pi*3/2,len(self.in_names)+2)[1:-1]]
            out_port_pos = [[math.cos(t),math.sin(t),0.0] for t in np.linspace(np.pi/2,-np.pi/2,len(self.out_names)+2)[1:-1]]
        else :
            in_port_pos = [[0,0,0]]*len(self.in_names)
            out_port_pos = [[0,0,0]]*len(self.out_names)
        
        # Initialize ports from self.in_names, self.out_names and self.max_in_data
        self.in_data = [self.Port('DataPort',True,name = port_name,max_connections= max_in_data,
         on_edge_activate = self.in_data_activate, pos= pos)for (port_name,max_in_data,pos) in zip(self.in_names,self.max_in_data,in_port_pos)]
        self.out_data = [self.Port('DataPort',False,name = port_name, pos = pos)for port_name,pos in zip(self.out_names,out_port_pos)]
        self.port_list = self.in_data + self.out_data

    def in_data_activate(self):
        # A functionNode activates when all its input dataFlow is active.
        for port in self.in_data:
            for flow in port.flows:
                if not flow.active:
                    return
        self.activate()

    def activate(self):
        super().activate()

        # TODO: Ask for value
        # Or should it be put in _run() ?
        for port in self.in_data:
            for flow in port.flows:
                if not flow.has_value:
                    pass


    def _run(self):
        for port in self.in_data:
            for flow in port.flows:
                flow.deactivate()

        # Gather data from input dataFlows
        funcion_input = []
        for port in self.in_data:
            if self.max_in_data == 1:
                if len(port.flows) == 0:
                    funcion_input.append(None) #TODO: Default value
                else:
                    funcion_input.append(port.flows[0].data)
            else:
                # Gather inpute data into a list
                funcion_input.append([flow.data for flow in port.flows])
        
        # Evaluate the function
        result = self.function(*funcion_input)

        # Send data to output dataFlows
        if len(self.out_data) == 1:
            for flow in self.out_data[0].flows:
                flow.recive_value(result)

        # result is tuple
        elif len(self.out_data) > 1:
            i=0
            for result_item in result:
                for flow in self.out_data[i].flows:
                    flow.recive_value(result_item)
                i+=1

    def running_finished(self, success = True):
        self.deactivate()

    @staticmethod
    def function():
        # Inherit FunctionNode to write different function.
        # If one input port has more than one dataFlows connected, their data will input to this function as a list.
        pass

