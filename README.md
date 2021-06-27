# NNNode
A node-based python environment for editing, training, and visualizing neural network models

A complete neural network (NN) model usually consits of dozens of basic modules (layers). The computed tensors are passed from modules to modules in some specific routes. When optimizing the model architecture and debugging, one has to frequently modify those routes as well as add/remove some modules temporarily to do experiments. Doing these on plain code may make the code ugly.

The client (UI) is made with Unity, which connectes to Python host with Websocket.
