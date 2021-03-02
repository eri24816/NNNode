using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;




public class Manager : MonoBehaviour
{
    public bool connectToServer=true;// Set this to false when debugging and don't want to connect to server.

    public static Manager i;
    public Dictionary<string,Node> Nodes;
    public Dictionary<string, Flow> Flows;
    public GameObject[] prefabs;
    public Dictionary<string, GameObject> prefabDict;
    public enum State
    {
        idle,
        draggingFlow
    }
    public State state;

    string WSPath = "ws://localhost:1000/";
    string env_name = "my_env";
    WebSocket lobby, env;
    Queue<string> messagesFromServer;
    Queue<string> avaliableIds;
    void Start()
    {
        messagesFromServer = new Queue<string>();
        avaliableIds = new Queue<string>();
        Nodes = new Dictionary<string, Node>();
        Flows = new Dictionary<string, Flow>();
        prefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in prefabs)
            prefabDict.Add(prefab.name, prefab);

        i = this;

        if (connectToServer)
        {
            lobby = new WebSocket(WSPath + "lobby");
            lobby.Connect();
            lobby.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
            lobby.Send("stt " + env_name);


            env = new WebSocket(WSPath + "env/" + env_name);
            env.Connect();
            env.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
            StartCoroutine(AskForUpdate(hz: 5)); // repeat getting update message from server
        }
        
    }

    public void AddFlow(Flow flow)
    {
        flow.id = avaliableIds.Dequeue();
        Flows.Add(flow.id,flow);
        if (flow is ControlFlow controlFlow)
        {
            if (connectToServer)
                env.Send(new APIMessage.NewControlFlow(controlFlow).Json);
        }
        if (flow is DataFlow dataFlow)
        {
            if (connectToServer)
                env.Send(new APIMessage.NewDataFlow(dataFlow).Json);
        }
        
    }

     
    public int nameNum = 0;


    public void AddNode(Node node) // Called by Node when the Creating corutine ends
    {
        // Tell server adding a node
        node.id = avaliableIds.Dequeue();
        Nodes.Add(node.id, node);
        if (!connectToServer) return;
        if (node is CodeNode)
        {
            env.Send(new APIMessage.NewCodeNode(node.id,node.name, node.transform.position).Json);
        }
        if (node is FunctionNode)
        {
            env.Send(new APIMessage.NewFunctionNode(node.id, node.name, node.transform.position).Json);
        }
    }

    public void RemoveNode(Node node)
    {
        Nodes.Remove(node.id);
        if (connectToServer)
            env.Send(new APIMessage.Rmv(node.id).Json);
    }

    public void RemoveFlow(Flow flow)
    {
        Flows.Remove(flow.id);
        if (connectToServer)
            env.Send(new APIMessage.Rmv(flow.id).Json);
    }

    public void MoveNode(Node node,Vector3 pos)
    {
        if (connectToServer)
            env.Send(new APIMessage.Mov(node.id,pos).Json);
    }


    public void Undo(Node node = null)
    {
        string id = node ? node.id : "";
        if (connectToServer)
            env.Send("{\"command\":\"udo\",\"id\":\"" + id + "\"}");
    }

    public void Redo(Node node = null)
    {
        string id = node ? node.id : "";
        if (connectToServer)
            env.Send("{\"command\":\"rdo\",\"id\":\"" + id + "\"}");
    }
    
    //TODO: update before every env.Send()
    IEnumerator AskForUpdate(float hz=5)
    {
        float timeSpan = 1f / hz;
        while (env.IsAlive)
        {
            env.Send("{\"command\":\"upd\"}");
            yield return new WaitForSeconds(timeSpan);
        }
    }

    string FindString(string json,string key)
    {
        int i = json.IndexOf(key),i1;
        i += key.Length + 2;
        while (json[i]!='\"' && json[i]!= '\'')
        {
            i++;
        }
        i1 = ++i;
        while (json[i] != '\"' && json[i] != '\'')
        {
            i++;
        }
        return json.Substring(i1, i - i1);
    }

    private void Update()
    {
        if (avaliableIds.Count < 5)
        {
            if (connectToServer)
                env.Send(new APIMessage.Gid().Json); // request for an unused id
            else
                avaliableIds.Enqueue((nameNum++).ToString());
        }
        while (messagesFromServer.Count > 0)
        {
            string recived = messagesFromServer.Dequeue();
            print(WSPath + " says: " + recived);
            

            string command = recived.Length >= 16 ? recived.Substring(13, 3) : "";


            if (command == "new")
            {
                if (!Nodes.ContainsKey(FindString(recived, "id"))&& !Flows.ContainsKey(FindString(recived, "id")))
                {
                    
                    string type = FindString(recived, "type");
                    if (type == "CodeNode") {
                        var message = JsonUtility.FromJson<APIMessage.NewCodeNode>(recived);
                        GameObject prefab = prefabDict[message.info.type];
                        var script = Instantiate(prefab).GetComponent<CodeNode>();
                        script.name = script.Name = message.info.name;
                        script.id = message.info.id;
                        Nodes.Add(message.info.id, script);
                        script.transform.position = new Vector3(message.info.pos[0], message.info.pos[1], message.info.pos[2]);
                    }

                    else if (type == "FunctionNode")
                    {
                        var message = JsonUtility.FromJson<APIMessage.NewFunctionNode>(recived);
                        GameObject prefab = prefabDict[message.info.type];
                        var script = Instantiate(prefab).GetComponent<FunctionNode>();
                        script.name = script.Name = message.info.name;
                        script.id = message.info.id;
                        Nodes.Add(message.info.id, script);
                        script.transform.position = new Vector3(message.info.pos[0], message.info.pos[1], message.info.pos[2]);
                    }
                    else if (type == "ControlFlow")
                    {
                        var message = JsonUtility.FromJson<APIMessage.NewControlFlow>(recived);
                        GameObject prefab = prefabDict[message.info.type];
                        var script = Instantiate(prefab).GetComponent<ControlFlow>();
                        script.id = message.info.id;
                        script.head = Nodes[message.info.head].GetPort(true);
                        script.head.Edges.Add(script);
                        script.tail = Nodes[message.info.tail].GetPort(false);
                        script.tail.Edges.Add(script);
                        Flows.Add(message.info.id, script);

                    }
                }
            }
            else if (command == "mov")
            {
                var message = JsonUtility.FromJson<APIMessage.Mov>(recived);
                Nodes[message.id].RawMove(new Vector3(message.pos[0], message.pos[1], message.pos[2]));
            }
            else if (command == "rmv")
            {
                var message = JsonUtility.FromJson<APIMessage.Rmv>(recived);
                if (Nodes.ContainsKey(message.id))
                {
                    StartCoroutine(Nodes[message.id].Removing());
                    Nodes.Remove(message.id);
                }
                if (Flows.ContainsKey(message.id))
                {
                    Flows[message.id].RawRemove();
                    Flows.Remove(message.id);
                }

            }
            // TODO: remove flow

            else if (command == "gid")// get a unused id to assign to new nodes or flows
            {
                var message = JsonUtility.FromJson<APIMessage.Gid>(recived);
                avaliableIds.Enqueue(message.id);
            }

        }
    }

}
