using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
using GraphUI;
using System.Collections.Concurrent;

public interface IUpdateMessageReciever
{
    public void RecieveUpdateMessage(string message, string command);
}

public class Manager : MonoBehaviour
{
    public bool connectToServer = true;// Set this to false when debugging and don't want to connect to server.

    public static Manager ins;

    public Dictionary<string, Node> Nodes;
    public Dictionary<string, Flow> Flows;
    public Dictionary<string, Node> DemoNodes;

    public GameObject[] nodePrefabs;
    public Dictionary<string, GameObject> nodePrefabDict;

    public GameObject[] compPrefabs;
    public Dictionary<string, GameObject> compPrefabDict;

    public GameObject inDataPortPrefab, outDataPortPrefab,inControlPortPrefab,outControlPortPrefab;

    public Transform canvasTransform;
    public Transform demoNodeContainer;
    public GameObject containerPrefab;
    
    public enum State
    {
        idle,
        draggingFlow
    }
    public State state;

    string WSPath = "ws://localhost:1000/";
    string env_name = "my_env";
    WebSocket lobby, env;
    ConcurrentQueue<string> messagesFromServer;
    ConcurrentQueue<string> avaliableIds;
    void Start()
    {
        Application.targetFrameRate = 60;

        messagesFromServer = new ConcurrentQueue<string>();
        avaliableIds = new ConcurrentQueue<string>();
        Nodes = new Dictionary<string, Node>();
        Flows = new Dictionary<string, Flow>();
        DemoNodes = new Dictionary<string, Node>();
        nodePrefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in nodePrefabs)
            nodePrefabDict.Add(prefab.name, prefab);
        compPrefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in compPrefabs)
            compPrefabDict.Add(prefab.name, prefab);

        ins = this;

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

    public void SendToServer(object obj)
    {
        if(connectToServer)
            env.Send(JsonUtility.ToJson(obj));
    }

    public string GetNewID()
    {
        Debug.Assert(avaliableIds.TryDequeue(out string id));
        return id;
    }
     
    public int nameNum = 0;

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

    public void Activate(Node node)
    {
        if (connectToServer&&node.id!="-1")
            env.Send("{\"command\":\"act\",\"id\":\"" + node.id + "\"}");
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

        while (messagesFromServer.TryDequeue(out string received))
        {
            print(WSPath + " says: " + received);
            if (received[0] != '{') return;
            string command = received.Length >= 16 ? received.Substring(13, 3) : "";
            if (command == "new")
            {
                var id = FindString(received, "id");
                if (!Nodes.ContainsKey(id) && !Flows.ContainsKey(id))
                {
                    string type = FindString(received, "type");
                    if (type == "ControlFlow" || (type == "DataFlow"))
                    {
                        var message = JsonUtility.FromJson<APIMessage.NewFlow>(received);
                        GameObject prefab = nodePrefabDict[message.info.type];
                        var flow = Instantiate(prefab).GetComponent<Flow>();
                        flow.id = message.info.id;
                        flow.head = Nodes[message.info.head].ports[message.info.head_port_id];
                        flow.head.Edges.Add(flow);
                        flow.tail = Nodes[message.info.tail].ports[message.info.tail_port_id];
                        flow.tail.Edges.Add(flow);
                        Flows.Add(message.info.id, flow);
                    }
                    else
                    {
                        CreateNode(received);
                    } 
                }
            }

            else if (command == "gid")// get a unused id to assign to new nodes or flows
            {
                var message = JsonUtility.FromJson<APIMessage.Gid>(received);
                avaliableIds.Enqueue(message.id);
            }

            else // directly send update messages to node
            {
                var id = JsonUtility.FromJson<APIMessage.UpdateMessage>(received).id;
                if (Nodes.ContainsKey(id))
                    Nodes[id].RecieveUpdateMessage(received, command);
                else if(Flows.ContainsKey(id))
                    Flows[id].RecieveUpdateMessage(received, command);
            }
        }
    }
    public Node CreateNode(string json,string id = null)
    {
        var message = JsonUtility.FromJson<APIMessage.NewNode>(json);
        if (Nodes.ContainsKey(message.info.id))return null;
        GameObject prefab = nodePrefabDict[message.info.frontend_type];
        var node = Instantiate(prefab).GetComponent<Node>();
        node.Init(json,id);
        return node;
    }
    public Transform FindCategoryPanel(string categoryString)
    {
        string[] cat = categoryString.Split('/');
        Transform panel = demoNodeContainer;
        foreach(string n in cat)
        {
            var t = panel.Find(n);
            if (t) panel = t;
            else
            {
                panel = Instantiate(containerPrefab, panel).transform;
                panel.SetSiblingIndex(panel.parent.childCount - 2);
                panel.GetComponentInChildren<TMPro.TMP_Text>().text = n;
                panel.name = n;
            }
        }
        return panel.Find("NodePanel");
    }
}
