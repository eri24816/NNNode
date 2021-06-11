from history import *
import datetime
import Environment
class Node:
    def __init__(self,info,env : Environment.Env):
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
    

    def on_input_flow_activated(self):
        # check if the condition is satisfied that the node can be activated (e.g. all its input flows is activated)
        pass

    def activate(self):
        self.activated = True
        # inform client to play animations

    def deactivate(self):
        self.activated = False
        # inform client to play animations


class CodeNode(Node):
    '''
    A node with editable code, like a block in jupyter notebook.

    The node will be invoked when every input dataflows and the input controlflow (if there is one) are all activated.
    It can also be invoked directly by client(e.g. double click on the node).
    After running, it will activate its output dataflows and ControlFlow (if there is one).

    If the node has no in or out data port, it will just run the code in it.
    If the node has DataPort, it will define a function.
    '''
    def __init__(self,info,env):
        '''
        info: {type=CodeNode,id,name,pos,code,output}
        '''
        super().__init__(info,env) #*? usage of super?
        self.code=info['code']if 'code' in info else ''
        self.output=info['output']if 'output' in info else ''
        self.added_output = '' # added lines of output when running, which will be send to client

        self.in_control=None # only 1 in_control is allowed
        self.out_control = []  # there can be more than one out_control
        
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

        self.active=0

    def get_info(self):
        in_data=[i for i in self.in_data] # get keys
        out_data=[i for i in self.out_data] # get keys
        return {"type":"CodeNode","id":self.id,"name":self.name,"pos":self.pos,"code":self.code,"output":self.output,"in_data":in_data,"out_data":out_data}

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
        for i in self.in_data:
            if self.in_data[i]:
                self.in_data[i].remove()
        for i in self.out_data:
            for j in self.out_data[i]:
                j.remove()
        # then remove the node itself
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
        if self.active: 
            return # prevent duplication in env.nodes_to_run
        self.env.nodes_to_run.put(self)
        self.env.Write_update_message(self.id, 'act', '1')  # 1 means "pending" (just for client to display)
        self.active=1

    def start_running(self):
        self.output = ''
        self.env.Write_update_message(self.id,'act','2') # 2 means "running"
        self.env.Write_update_message(self.id, 'clr') # clear output before running
    
    def flush_output(self): # called when client send 'upd'
        if self.added_output == '':
            return
        self.output += self.added_output
        self.env.Write_update_message(self.id, 'out', self.added_output) # send client only currently added lines of output
        self.added_output = ''
    
    def finish_running(self):
        self.flush_output()
        self.deactivate()
        for out_control in self.out_control:
            out_control.activate()
        self.env.Write_update_message(self.id, 'act', '0')
        self.active=0


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
