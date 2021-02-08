
import sys
from io import StringIO
import ast
import traceback

# Redirect stdout
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
            exec(compile(ast.Module(body=stmts[:-1]), filename="<ast>", mode="exec"), globals, locals)
        print(eval(compile(ast.Expression(body=stmts[-1].value), filename="<ast>", mode="eval"), globals, locals))
    else:    
        exec(script, globals, locals)
    return output

import queue

#The environment to run the code in.
class Env():

    def __init__(self):
        self.flag_running = True
        self.tasks = queue.Queue()
        self.blocks={}

    # Run in another thread from the main thread (server.py)
    def run(self):

        while self.flag_running:
            self.task=None # task = None when idle
            self.task = self.tasks.get()
            output=''
            try:
                with stdoutIO() as s:
                    exec_(message)
                    output = s.getvalue()
            except Exception:
                output = traceback.format_exc()
            print(output)