using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
using GraphUI;
using System.Collections.Concurrent;

public class Manager : MonoBehaviour
{
    public bool connectToServer=true;// Set this to false when debugging and don't want to connect to server.

    public static Manager ins;
    public Transform canvasTransform;
    public Dictionary<string,Node> Nodes;
    public Dictionary<string, Flow> Flows;
    public Dictionary<string, Node> DemoNodes;
    public GameObject[] prefabs;
    public GameObject inDataPortPrefab, outDataPortPrefab,inControlPortPrefab,outControlPortPrefab;
    public Dictionary<string, GameObject> prefabDict;

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
        prefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in prefabs)
            prefabDict.Add(prefab.name, prefab);

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

    public string GetNewID()
    {
        Debug.Assert(avaliableIds.TryDequeue(out string id));
        return id;
    }

    public void AddFlow(Flow flow)
    {
        flow.id = GetNewID();
        Flows.Add(flow.id,flow);

        if (connectToServer)
            env.Send(new APIMessage.NewFlow(flow).Json);
    }

     
    public int nameNum = 0;


    public void AddNode(Node node) // Called by Node when the Creating corutine ends
    {
        // Tell server to add a node
        node.id = GetNewID();
        Nodes.Add(node.id, node);
        if (!connectToServer) return;
        env.Send(new APIMessage.NewNode(node.id, node.type, node.name, node.transform.position).Json);
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

    public void SetCode(Node node)
    {
        if (!connectToServer) return;
        if (node is CodeNode codeNode)
        {
            env.Send(new APIMessage.Cod(node.id, codeNode.Code).Json);
        }
        
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
                        GameObject prefab = prefabDict[message.info.type];
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
                        var message = JsonUtility.FromJson<APIMessage.NewNode>(received);
                        GameObject prefab = prefabDict[message.info.frontend_type];
                        var node = Instantiate(prefab).GetComponent<Node>();
                        node.Init(message.info);
                    } 
                }
            }
            else if (command == "mov")
            {
                var message = JsonUtility.FromJson<APIMessage.Mov>(received);
                Nodes[message.id].RawMove(message.pos);
            }
            else if (command == "rmv")
            {
                var message = JsonUtility.FromJson<APIMessage.Rmv>(received);
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

            else if (command == "gid")// get a unused id to assign to new nodes or flows
            {
                var message = JsonUtility.FromJson<APIMessage.Gid>(received);
                avaliableIds.Enqueue(message.id);
            }

            else if (command == "cod")
            {
                var message = JsonUtility.FromJson<APIMessage.Cod>(received);
                Node node = Nodes[message.id];
                if (node is SimpleCodeNode codeNode)
                    codeNode.Code = message.info;
            }
            else if (command == "act")
            {
                var message = JsonUtility.FromJson<APIMessage.UpdateMessage>(received);
                if (Nodes.ContainsKey(message.id))
                {
                    if (message.info == "0")
                        Nodes[message.id].DisplayInactivate();
                    if (message.info == "1")
                        Nodes[message.id].DisplayPending();
                    if (message.info == "2")
                        Nodes[message.id].DisplayActivate();
                }/* TODO
                else
                {
                    if (message.info == "0")
                        Flows[message.id].DisplayInactivate();
                    if (message.info == "1")
                        Flows[message.id].DisplayPending();
                    if (message.info == "2")
                        Flows[message.id].DisplayActivate();
                }*/
            }
            else if (command == "clr")
            {
                var message = JsonUtility.FromJson<APIMessage.Gid>(received);
                Node node = Nodes[message.id];
                node.ClearOutput();
            }
            else if (command == "out")
            {
                var message = JsonUtility.FromJson<APIMessage.UpdateMessage>(received);
                Node node = Nodes[message.id];
                node.AddOutput(message.info);
            }
        }
    }

    public Transform FindCategoryPanel(string categoryString)
    {
        print(categoryString);
        string[] cat = categoryString.Split('/');
        Transform panel = demoNodeContainer;
        foreach(string n in cat)
        {
            print(n);
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
