# ObjectSync Server
## Overview
[](overview.png)
ObjectSync is a tool which helps app developers to make objects and their states shared across the server and multiple clients. Users can change the objects from either the server or any of the clients, and the changes will be automatically applied to all the other endpoints.

There are three core classes in ObjectSync:

### Space
In ObjectSync, `Object`s must be created in a `Space`. A `Space` has a collection of `Object`s. It assigns an unique id to each `Object` created in it, so `Object`s in the same `Space` can access each other with the ids.

### Object
An `Object` is something we want to share across the server and clients. It can be a form, a game object, a color picker, etc.

An `Object` can have multiple states, which called `Attribute`s. Each `Attribute` has a name and a value.

An `Object` can be serialized and deserialized to and from JSON so it can be saved to and loaded from a file and send through the network.

### Attribute
An `Attribute` stores a state about an `Object`. It can be a number, a string, a boolean, a vector, etc. Once an `Attribute` is created, it can be changed by the server or the clients. When it is changed by any endpoint, the server will send the new value to all the clients.

---

To be specific, let's say we've built a drawing application based on ObjectSync, and now we have a green square in the canvas.
The layout in the `Space` would be like:
    
```
{
    "id": "0",
    "type": "Canvas",
    "attributes": [
        {
            "name": "parent_id",
            "type": "String",
            "value": null
        },
    ],
    "children": [
        {
            "id": "1",
            "type": "Square",
            "attributes": [
                {
                    "name": "parent_id",
                    "type": "String",
                    "value": "0"
                },
                {
                    "name": "color",
                    "type": "Vector3",
                    "value": {0, 1, 0}
                }
                {
                    "name": "position",
                    "type": "Vector3",
                    "value": {64, 64}
                }
            ],
            "children": []
        }
    ]
}
```
## Heirarchy

ObjectSync use a hierarchical structure to organize `Object`s. Each `Object` has a parent [^1] and zero or more children. Each `Object` has a attribute named `parent_id` which stores the id of the parent. 

A space is initialized with a root `Object` in it, which has no parent and is the common ancestor of all other objects. The root `Object` has id="0" and cannot be destroyed.

After a space is initialized, one can create new `Object`s in the space as children of the root `Object` or other existing `Object`.  To create an `Object`, call `Space.create(d)` on the server side or send `{"command": "create", "d":{...} }` from the client side. To define an object's default children, override the `Object.build()` method and call `Space.create(d)`.

> `d` is the object's serialization. See section [Serialization](#serialization) for more details.

Destroying an object removes the object and all of its children from the space. To destroy an `Object`, call `Space.destroy(id)` on the server side or send `{"command": "destroy", "id": "id"}` from the client side.

> If the hierarchical characteristic is not desired, one can set every `Object` they created as a direct childrn of the root object.

[^1]: Except the root `Object`.


## Serialization
An `Object` can be serialized into JSON or a python dict. A serialization is used when passing the `Object`'s state through the network, saving the 'Object' to a file, and restoring a destroyed `Object`. A serialization has the following format:

```
{
    "id": "",
    "type": "",
    "frontend_type": "",
    "attributes": [
        {
            "name": "",
            "type": "",
            "value": "",
            "history_object": ""
        },
        ...
    ],
    "children": [
        <serialization of child object>,
        ...
    ]
}
```
> The serialization is calle "d" in method argument lists and in the JSON api.

## History and Command




## Network and API
The communication between the server and the clients is done through a websocket. 

A client can connect to a space through `ws://<server>:<port>/space/<space name>`.

The server and the clients communicate through a JSON string. The JSON message always has a `"command"` field, from which the reciever knows which method to use to handle the message. Here is the list of built-in message types:

### Server -> Client
#### space_metadata
Sent once client is connected. The field `"types"` contains all the valid type names that can put in the `"type"` field of a `"create"` message.
```
{
    "command": "space_metadata",
    "types": [
        "ObjectTypeName1",
        "ObjectTypeName2",
        ...
    ]
}
```
#### load
Sent once client is connected. Used to initialize the client side `Space`.
```
{
    "command": "load",
    "root_object": <serialization of root object>
}
```
#### create
Inform the client to create a new `Object`.
```
{
    "command": "create",
    "d": <serialization of object>
}
```
#### destroy
Inform the client to destroy an `Object`.
```
{
    "command": "destroy",
    "id": <id>
}
```
#### new attribute
Inform the client to add an attribute to the `Object`.
```
{
    "command": "new attribute",
    "id": <object id>,
    "name": <name>,
    "type": <type>,
    "history_object": <id of history object>
    "value": <init value>
}
```
#### attribute
Inform the client to change an `Object`'s attribute.
```
{
    "command": "attribute",
    "id": <object id>,
    "name": <name>,
    "value": <new value>
}
```

### Client -> Server
#### create
Create an `Object` in the server. After the `Object` has been created, the server will send back a `create` message to all clients.
```
{
    "command": "create",
    "parent": <parent id>,
    "d": <serialization of object>
}
```

Minimal version:
```
{
    "command": "create",
    "parent": <parent id>,
    "d": {
        "type": <type name>
    }
}
```
The minimal version works because the server knows how to build and initialize the object with the default settings. 
#### destroy
Destroy an `Object` in the server. After the `Object` has been destroyed, the server will send back a `destroy` message to all clients.
```
{
    "command": "destroy",
    "id": <id>
}
```
#### attribute
Change an `Object`'s attribute in the server. Afterwards, the server will send back a `attribute` message to all clients.
```
{
    "command": "attribute",
    "id": <object id>,
    "name": <name>,
    "value": <new value>
}
```
#### undo
Undo the last change recorded in an object's history. If there is a `Command` to undo, the server will send back a message indicating what is changed when undoing.
```
{
    "command": "undo",
    "id": <object id>
}
```
#### redo
Redo the last change recorded in an object's history. If there is a `Command` to redo, the server will send back a message indicating what is changed when redoing.
```
{
    "command": "redo",
    "id": <object id>
}
```



