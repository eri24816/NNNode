using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;




public class Manager : MonoBehaviour
{
    public bool connectToServer=true;// Set this to false when debugging and don't want to connect to server.

    public static Manager i;
    public Dictionary<string,Node> Nodes;
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
    void Start()
    {
        messagesFromServer = new Queue<string>();
        Nodes = new Dictionary<string, Node>();
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
        // TODO: Tell server adding an flow
    }

     
    public int nameNum = 0;


    public void AddNode(Node node) // Called by Node when the Creating corutine breaks
    {
        // Tell server adding a node
        Nodes.Add(node.Name, node);
        if (!connectToServer) return;
        if (node is CodeNode)
        {
            env.Send(new APIMessage.NewCodeNode(node.name, node.transform.position).Json);
        }
        if (node is FunctionNode)
        {
            env.Send(new APIMessage.NewFunctionNode(node.name, node.transform.position).Json);
        }
    }


    
    public void MoveNode(Node node,Vector3 pos)
    {
        if (connectToServer)
            env.Send(new APIMessage.Mov(node.Name,pos).Json);
    }


    public void Undo(Node node = null)
    {
        string name = node ? node.Name : "";
        if (connectToServer)
            env.Send("{\"command\":\"udo\",\"node_name\":\"" + name + "\"}");
    }

    public void Redo(Node node = null)
    {
        string name = node ? node.Name : "";
        if (connectToServer)
            env.Send("{\"command\":\"rdo\",\"node_name\":\"" + name + "\"}");
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
        while (messagesFromServer.Count > 0)
        {
            string recived = messagesFromServer.Dequeue();
            print(WSPath + " says: " + recived);
            

            string command = recived.Length >= 16 ? recived.Substring(13, 3) : "";


            if (command == "new")
            {
                if (!Nodes.ContainsKey(FindString(recived, "name")))
                {
                    string type = FindString(recived, "type");
                    print(type);
                    if (type == "CodeNode") {
                        var message = JsonUtility.FromJson<APIMessage.NewCodeNode>(recived);
                        GameObject prefab = prefabDict[message.info.type];
                        var script = Instantiate(prefab).GetComponent<CodeNode>();
                        script.name = script.Name = message.info.name;
                        script.transform.position = new Vector3(message.info.pos[0], message.info.pos[1], message.info.pos[2]);
                    }

                    else if (type == "FunctionNode")
                    {
                        var message = JsonUtility.FromJson<APIMessage.NewFunctionNode>(recived);
                        GameObject prefab = prefabDict[message.info.type];
                        var script = Instantiate(prefab).GetComponent<FunctionNode>();
                        script.name = script.Name = message.info.name;
                        script.transform.position = new Vector3(message.info.pos[0], message.info.pos[1], message.info.pos[2]);
                    }
                }
            }
            else if (command == "mov")
            {
                var message = JsonUtility.FromJson<APIMessage.Mov>(recived);
                Nodes[message.node_name].RawMove(new Vector3(message.pos[0], message.pos[1], message.pos[2]));
            }
            else if (command == "rmv")
            {
                var message = JsonUtility.FromJson<APIMessage.Rmv>(recived);
                Nodes[message.node_name].Remove();
            }
        }
    }

}
