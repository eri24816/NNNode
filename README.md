# NNNode
A node-based python environment for editing, training, and visualizing neural network models

A complete neural network (NN) model usually consits of dozens of basic modules (layers). The computed tensors are passed from modules to modules in some specific routes. When optimizing and debugging a model, one has to frequently modify those routes as well as add/remove some modules temporarily to do experiments. Doing these on plain code may make the code ugly and hard to read. Inspecting the inside of the model and visualizing results during training may need extra effort loading data into tools like TensorBoard or Visdom.

In NNNode, all NN modules, operations, and variables can be represented as nodes, and data (tensor) flows are edges. In this way, the complete NN model is visuallized, and anyone can understand its architecture at a glance, no need to dig into code.

The client (UI) is made with Unity, which connectes to Python host with Websocket.
