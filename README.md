# NNNode

## Introduction

NNNode is a node-based python editor for editing, training, and visualizing neural network models (still under develoment)

A complete neural network (NN) model consits of dozens of basic modules (layers). The computed tensors are passed from modules to modules in some specific routes. When optimizing and debugging a model, one has to frequently modify those routes as well as add/remove some modules temporarily to do experiments. Doing these on plain code may make the code ugly and hard to read. Inspecting the inside of the model and visualizing results during training may need extra effort loading data into tools like TensorBoard or Visdom.

In NNNode, all NN modules, operations, and variables can be represented as nodes, and data (tensor) flows are edges. In this way, the complete NN model is visuallized, and anyone can understand its architecture at a glance, no need to dig into the code.

The client (UI) is made with Unity, which connectes to Python host with Websocket.

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
![image](https://user-images.githubusercontent.com/30017117/126072854-ba9c47a9-5e0f-4220-8a6e-81a896a2dc11.png)

### RoundNode
![image](https://user-images.githubusercontent.com/30017117/126073098-0689d4d5-57ad-4b43-8219-63c30b00ab82.png)

### SimpleNode
![image](https://user-images.githubusercontent.com/30017117/126073182-67c3cd75-ff0f-460c-a8e2-b1087e30c3b5.png)

Supported feature of each node type:
|   |GeneralNode|RoundNode|SimpleNode|
|---|:-:|:-:|:-:|
|Port|✅ | ✅  | ✅  |
|Component|✅ | ⬜️  | ✅  |
|Name display| ✅  |  ✅ | ⬜️  |

