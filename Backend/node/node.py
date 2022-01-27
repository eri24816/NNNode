from __future__ import annotations
import objectsync_server as objsync
import math
import numpy as np
import sys
sys.path.append('D:/NNNode/Backend') # for debugging
from history import History
import sys
import traceback
import config

from typing import TYPE_CHECKING, TypedDict
from typing import Dict, List
if TYPE_CHECKING:
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
    '''
    Unity Vector3 Json format
    '''
    return {'x':x,'y':y,'z':z}

class Port():
    '''
    Different node classes might have Port classes that own different properties for frontend to read. 
    In such case, inherit this
    '''
    def __init__(self,node: Node, type : str, isInput : bool, max_connections : int = '64', name : str = '',
     description : str = '',pos = [0,0,0], on_edge_activate = lambda : None, with_order : bool = False,is_ready = lambda :False,require_value =None):
        self.id = str(len(node.port_list))
        node.port_list.append(self)
        self.type = type
        self.isInput = isInput
        self.max_connections = max_connections
        self.name = name
        self.description = description 
        self.pos = pos
        self.flows : List[edge.DataFlow] = [] 
        self.with_order = with_order # Whether the order of edges connected to the port matters.

        # Delegates==================

        # For input ports, do somthing when an edge is activated
        self.on_edge_activate = on_edge_activate 

        # For output ports, if is_ready returns True, the port can instantly give an output value via require_value
        #self.is_ready = is_ready
        #self.require_value = require_value

    def get_dict(self):
        # for json.dump
        return {
            'id': self.id,
            'type':self.type,
            'isInput' : self.isInput,
            'max_connections' : self.max_connections,
            'with_order' : self.with_order,
            'name': self.name,
            'description' : self.description,
            'pos' : v3(*self.pos)
            }


class Component:
    '''
    Like a slider or an input field
    A component controls an objsync.Attribute.
    Component class have no set() method. When component value is modified, client should send "atr" command, which leads to objsync.Attribute.set()
    '''
    def __init__(self,node : Node,name,type,target_attr):
        node.components.append(self)
        self.name = name # for UI to display
        self.type = type # C# class name
        self.target_attr = target_attr # objsync.Attribute name
    
    def dict(self):
        return {'name':self.name,'type' : self.type, 'target_attr' : self.target_attr}

class Node(objsync.Object):
    '''
    Base class of all types of nodes
    '''
    display_name = 'Node'
    shape : str = ''
    category = ''

    class Info(TypedDict):
        type : str
        id : int

        # for client ------------------------------
        shape : str     # CodeNode, FunctionNode, RoundNode
        category : str
        doc : str
        name : str              # Just for frontend. In backend we never use it but use "id" instead
        output : str            # Output is necessary for all classes of node to at least store exceptions occured in nodes
        portInfos : List[Dict]  # PortInfos are determined by server side Node classes and is used to tell client how to set up ports

    @classmethod
    def get_class_info(cls)->Info:
        '''
        When creating demo node, this is sent to client
        '''
        tempNode = cls({'id' : -1, 'name' : ''},space = None)
        return tempNode.get_info()
    
    def serialize(self) -> Dict[str]:
        '''
        Node info
        History item "new" stores this. When creating new node, this is sent to all clients.
        And when client sends redo, the node can be recreated from it. 
        '''
        d = super(Node, self).serialize()
        d.update({
            "category" : self.category,"doc":self.__doc__,"name":self.display_name,"output":self.output,'shape' : self.shape,
            'portInfos' : [port.get_dict() for port in self.port_list],
            'attr': [attr.dict()for _, attr in self.attributes.items()],
            'comp': [comp.dict()for  comp in self.components]
        })
        return d

    ## Initialization ------------------------------

    def __init__(self, space : objsync.Space, d : Dict[str]):
        '''
        For child classes, DO NOT override this. Override initialize() instead.
        '''
        super(objsync.Object, self).__init__(space, d)

        # For the API, each ports of the node are identified by position in this list
        # Create ports in __init__ then add all port into this list
        self.port_list : List[Port] = []

        # Each types of node have different objsync.Attributes. Client can set them by sending "atr" command.
        # Changes of objsync.Attributes will create update messages and send to clients with "atr" command.
        
        self.components : List[Component] = []

        self.type=type(self).__name__

        # Is the node ready to run?
        self.active = False

        self.output = d['output'] if 'output' in d else ''
        # added lines of output when running, which will be sent to client
        self.added_output = "" 

        if self.id != -1:
            self.history = History()

        self.object = None

        self.initialize() 

        self.color = objsync.Attribute(self, 'color', 'Vector3',v3(*config.get_color(self.category)),'')
        if self.id != -1:
            self.space.Update_history("new", self.get_info())
            self.space.Add_direct_message({'command':'new','info':self.get_info()})
        
        
    def initialize(self):
        '''
        Setup the node's objsync.Attributes and components
        This method is separated from __init__() because overrides of initialize() should be called after some setup in __init__()
        '''
    
    ## Core methods ------------------------------

    def attempt_to_activate(self):
        if self.is_ready() and not self.space.lock_deque:
            self.activate()

    def activate(self):
        '''
        Call this to enqueue the node in space.
        Later, space will call _run() of this node
        '''
        if self.active: 
            return # prevent duplication in space.nodes_to_run
        self.active = True
        self.space.add_to_deque(self) # It's normally working in LIFO order, but manual activation by client can be FIFO.
        
        self.space.Add_buffered_message(self.id, 'act', '1')  # 1 means "pending" (just for client to display)

    def deactivate(self):
        self.active = False

        self.space.Add_buffered_message(self.id, 'act', '0')

    def run(self):
        # space calls this method
        self.space.Add_buffered_message(self.id,'act','2') # 2 means "running"
        self.space.Add_buffered_message(self.id, 'clr') # Clear output

        # Redirect printed outputs and error messages to client
        with stdoutIO(self): #TODO: optimize this
            try:
                self._run()
            except Exception:
                self.running_finished(False)
                self.added_output += traceback.format_exc()
                self.flush_output()
            else:
                self.running_finished(True)  


    ## Backward signal ------------------------------

    def is_ready(self):
        '''
        Returns True if the node is ready to provide its outputs
        '''
        return False

    def require_value(self):
        '''
        Called by a flow in front
        Always check is_ready() == True before calling this
        '''
        assert self.is_ready()

        # If a node produces a backward signal, prevent its sibling to be activated by setting lock_deque to True
        if not self.space.lock_deque:
            with self.space.get_deque_lock():
                self.run()
        else:
            self.run()


    ## Virtual methods ------------------------------

    def _run(self):
        '''
        Actually define what the type of node acts
        '''
        pass
    
    def running_finished(self, success = True):
        '''
        If deactivating after running is unwanted, override this
        '''
        self.deactivate()


    ## Operations from/to client ------------------------------

    def flush_output(self): # called when client send 'upd'
        if self.added_output == '':
            return
        self.output += self.added_output
        self.space.Add_buffered_message(self.id, 'out', self.added_output) # send client only currently added lines of output
        self.added_output = ''

    def recive_message(self,m):
        '''
        {'id',command' : 'act'}
        {'id',command' : 'atr', 'name', 'value'}
        '''
        super().recive_message(m)
        command = m['command']
                    
        if command == "act":
            self.On_double_click()

    def On_double_click(self):
        self.attempt_to_activate()

class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.

    The node will be invoked when its input Controlflow is activated or double click on the node.
    It will execute its code and activate its output ControlFlow (if there is one).
    '''

    shape = 'VerticalSimple'
    category = 'basic'
    display_name = 'Code'

    def initialize(self):
        super().initialize()

        self.code = objsync.Attribute(self,'code','string','')
        Component(self,'input_field','TextEditor','code')

        Component(self,'output_field','Text','output')
    
    def is_ready(self):
        return True

    def _run(self):

        exec_(self.code.value,self.space.globals,self.space.locals)

    def running_finished(self, success = True):
        self.flush_output()
        self.deactivate()

class EvalAssignNode(Node):

    shape = 'Simple'
    category = 'basic'
    display_name = 'Evaluate or Assign'

    def initialize(self):
        super().initialize()
        
        self.in_data = Port(self,'DataPort',True,64,pos = [-1,0,0], on_edge_activate = self.in_data_active)
        self.out_data = Port(self,'DataPort',False,64,pos = [1,0,0])

        self.code = objsync.Attribute(self,'code','string','')
        Component(self,'input_field','SmallTextEditor','code')

        self.block_backward = objsync.Attribute(self,'block_backward','bool',True)

    def is_ready(self):
        if self.block_backward.value:
            return True
        else:
            return self.in_data.flows[0].is_ready()

    def require_value(self):
        if self.block_backward.value or not self.is_ready():
            self.value = eval(self.code.value,self.space.globals,self.space.locals)
            for flow in self.out_data.flows:
                flow.recive_value(self.value)
        else:
            self.attempt_to_activate()

    def in_data_active(self,port):
        self.attempt_to_activate()

    def _run(self):
        if len(self.in_data.flows)>0:

            if len(self.in_data.flows) == 1:
                self.value = self.in_data.flows[0].get_value()
            else:
                self.value = []
                for flow in self.in_data.flows:
                    self.value.append(flow.get_value())
            #exec_(self.objsync.Attributes['code'] + " = __value", self.space.globals, {'__value' : self.value})
            self.space.globals.update({self.code.value: self.value})
        else:
            self.value = eval(self.code.value,self.space.globals,self.space.locals)
        

    def running_finished(self, success = True):
        if success:
            self.flush_output()
            for flow in self.out_data.flows:
                flow.recive_value(self.value)
        self.deactivate()
        
class FunctionNode(Node):
    '''
    A FunctionNode defines a function.
    The function is invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows (if there is one).
    '''

    #display_name = 'Function'
    category = 'function'

    class Info(Node.Info):
        in_names : List[str]
        out_names : List[str]
        allow_multiple_in_data : List[bool]

    # Most of the child classes of FunctionNode just differ in following 4 class properties and their function() method.
    shape = 'General'
    in_names : List[str] = []
    out_names : List[str] = []
    max_in_data : List[int] = []

    def initialize(self):
        '''
        info: {type=FunctionNode,id,name,pos,output}
        '''
        super().initialize()

        if self.shape == 'Round':
            in_port_pos = [[math.cos(t),math.sin(t),0.0] for t in np.linspace(np.pi/2,np.pi*3/2,len(self.in_names)+2)[1:-1]]
            out_port_pos = [[math.cos(t),math.sin(t),0.0] for t in np.linspace(np.pi/2,-np.pi/2,len(self.out_names)+2)[1:-1]]
        else :
            in_port_pos = [[0,0,0]]*len(self.in_names)
            out_port_pos = [[0,0,0]]*len(self.out_names)
        
        if self.max_in_data == []:
            self.max_in_data = [1]*len(self.in_names)

        # Initialize ports from self.in_names, self.out_names and self.max_in_data
        self.in_data = [Port(self,'DataPort',True,name = port_name,max_connections= max_in_data,
         on_edge_activate = self.in_data_activate, pos= pos)for (port_name,max_in_data,pos) in zip(self.in_names,self.max_in_data,in_port_pos)]
        self.out_data = [Port(self,'DataPort',False,name = port_name, pos = pos,is_ready = self.is_ready)for port_name,pos in zip(self.out_names,out_port_pos)]

    def is_ready(self):
        '''
        Return true if it's ready to ouput.
        FunctionNode requires all the previous node to be ready, to be ready itself.
        '''
        for port in self.in_data:
            for flow in port.flows:
                if not flow.is_ready() :
                    return False
        return True

    def in_data_activate(self,port):
        self.attempt_to_activate()

    def _run(self):

        # Gather data from input dataFlows
        funcion_input = []
        for port,max_in_data in zip(self.in_data,self.max_in_data):
            if max_in_data == 1:
                if len(port.flows) == 0:
                    funcion_input.append(None) #TODO: Default value
                else:
                    funcion_input.append(port.flows[0].get_value())
            else:
                # Gather inpute data into a list
                funcion_input.append([flow.get_value() for flow in port.flows])
        
        # Evaluate the function
        result = self.function(*funcion_input)

        # Send data to output dataFlows
        if len(self.out_data) == 1:
            for flow in self.out_data[0].flows:
                flow.recive_value(result)

        # if result is tuple
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

class ObjectNode(Node):
    display_name = 'Object'
    category = 'basic'

    def initialize(self):
        super().initialize()
        