# NNNode
![image](https://user-images.githubusercontent.com/30017117/126078649-887d8749-33ff-40f1-89e8-a43623f4e358.png)

## Introduction

NNNode is a node-based python editor, specifically for editing, training, and visualizing pytotch neural network models. (It's still under development)

A complete neural network (NN) model consits of dozens of basic modules (layers). The computed tensors are passed from modules to modules in some specific routes. When optimizing and debugging a model, one has to frequently modify those routes as well as add/remove some modules temporarily to do experiments. Doing these on plain code may make the code ugly and hard to read. Inspecting the inside of the model and visualizing results during training may need extra effort loading data into tools like TensorBoard or Visdom.

In NNNode, all NN modules, operations, and variables can be represented as nodes, and data (tensor) flows can be represented as edges. In this way, the complete NN model is visuallized, and anyone can understand its architecture at a glance, no need to dig into the code.

The client (UI) is made with Unity, which connectes to Python host with Websocket.

## Features
- User-defined nodes
- Undo/redo system
- Connects with websocket, so it can run on a remote server
- Sync between multiple clients
- Real-time client update while the graph is running (including node outputs, node attribute changes, and visualization components)

## Start Using
### Start server
```
cd backend
python server.py
```
### Start client
1. Download Unity 2020.3.14 
2. Open this folder as a Unity project
3. Press the start button

## Front-end node types
### GeneralNode

<img src="https://user-images.githubusercontent.com/30017117/126072854-ba9c47a9-5e0f-4220-8a6e-81a896a2dc11.png" height="100" />

### RoundNode

<img src="https://user-images.githubusercontent.com/30017117/126073098-0689d4d5-57ad-4b43-8219-63c30b00ab82.png" height="100" />

### SimpleNode

<img src="https://user-images.githubusercontent.com/30017117/126073182-67c3cd75-ff0f-460c-a8e2-b1087e30c3b5.png" height="100" />

Supported features of each node type:
|   |GeneralNode|RoundNode|SimpleNode|
|---|:-:|:-:|:-:|
|Port|✅ | ✅  | ✅<br>(max 1 in-port and 1 out-port)  |
|Component|✅ | ⬜️  | ✅  |
|Name display| ✅  |  ✅ | ⬜️  |

## Define custom nodes
### FunctionNode
A FunctionNode serves as a function. You can define your own one by inheriting the FunctionNode class.

By simply adding this into /backend/node/user_defined.py:

```python
class IsEvenNode(FunctionNode):
    display_name = 'is_even'
    in_names = ['n']
    out_names = ['out']
```
you'll get a new node:

<img src="https://user-images.githubusercontent.com/30017117/126074317-8fbb8654-f82d-47e6-bcbf-d727281f8c31.png" height="100" />

Implement the function to make the node actually work.

```python
class IsEvenNode(FunctionNode):
    display_name = 'is_even'
    in_names = ['n']
    out_names = ['out']
    
    @staticmethod
    def function(n):
        return not (n%2)
```
 
 ## Base Node class
 ...
 You can add the line
 ```python
 frontend_type = 'RoundNode'
 ```
to make the node round.

<img src="https://user-images.githubusercontent.com/30017117/126074685-e189983f-7a6b-4a8f-bbf8-b0898acf9cc6.png" height="100" />

## TODOs
- ⬜️ Write visualization components
- ⬜️ Make nodes snap to gird
- ⬜️ Enable embedding SimpleNode into FunctionNode
- ⬜️ Save/Load
- ⬜️ Intergrate node editor to frontend (not important)

