using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
using GraphUI;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObjectSync
{
    namespace Interfaces
    {
        public interface I
    }
    public interface IUpdateMessageReciever
    {
        public void RecieveUpdateMessage(JToken message);
    }

    public static class JsonHelper
    {
        public static object JToken2type(JToken j, string type)
        {
            if (type.Length >= 8 && type.Substring(0, 8) == "dropdown") type = "string";
            return type switch
            {
                "string" => (string)j,
                "float" => (float)j,
                "Vector3" => j.ToObject<Vector3>(),
                "bool" => (bool)j,
                _ => throw new System.Exception($"Type {type} not supported"),
            };
        }
    }

    public class Space : MonoBehaviour
    {
        public bool connectToServer = true;// Set this to false when debugging and don't want to connect to server.

        public Dictionary<string, Object> objs;

        public GameObject[] nodePrefabs;

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
            messagesFromServer = new ConcurrentQueue<string>();
            avaliableIds = new ConcurrentQueue<string>();
            objs = new Dictionary<string, Object>();


            if (connectToServer)
            {
                lobby = new WebSocket(WSPath + "lobby");
                lobby.Connect();
                lobby.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
                lobby.Send("stt " + env_name);

                env = new WebSocket(WSPath + "env/" + env_name);
                env.Connect();
                env.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
            }

        }

        public void SendToServer(object obj)
        {
            if (connectToServer)
                env.Send(JsonUtility.ToJson(obj)); // Here I use UnityEngine.JsonUtility instead of Json.NET because the latter produces error converting Vector3.
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
            if (connectToServer && node.id != "-1")
                env.Send("{\"command\":\"act\",\"id\":\"" + node.id + "\"}");
        }

        private void Update()
        {
            if (avaliableIds.Count < 5)
            {
                if (connectToServer)
                    env.Send("{\"command\":\"gid\"}"); // request for an unused id
                else
                    avaliableIds.Enqueue((nameNum++).ToString());
            }

            while (messagesFromServer.TryDequeue(out string receivedString))
            {
                print(WSPath + " says: " + receivedString);
                if (receivedString[0] != '{') return; // Filter out debugging message
                var recieved = JObject.Parse(receivedString);
                string command = (string)recieved["command"];
                if (command == "new")
                {
                    var info = recieved["info"];
                    var id = (string)info["id"];
                    if (!Nodes.ContainsKey(id) && !Flows.ContainsKey(id))
                    {
                        var type = (string)info["type"];
                        if (type == "ControlFlow" || type == "DataFlow")
                        {
                            GameObject prefab = Theme.ins.Create((string)info["type"]);
                            var flow = prefab.GetComponent<Flow>();
                            flow.id = (string)info["id"];
                            flow.head = Nodes[(string)info["head"]].ports[(int)info["head_port_id"]];
                            flow.head.Edges.Add(flow);
                            flow.tail = Nodes[(string)info["tail"]].ports[(int)info["tail_port_id"]];
                            flow.tail.Edges.Add(flow);
                            Flows.Add(id, flow);
                        }
                        else
                        {
                            CreateNode(info);
                        }
                    }
                }

                else if (command == "gid")// get a unused id to assign to new nodes or flows
                {
                    //var message = JsonUtility.FromJson<APIMessage.Gid>(received);
                    avaliableIds.Enqueue((string)recieved["id"]);
                }

                else // directly send update messages to node
                {
                    var id = (string)recieved["id"];
                    if (Nodes.ContainsKey(id))
                        Nodes[id].RecieveUpdateMessage(recieved);
                    else if (Flows.ContainsKey(id))
                        Flows[id].RecieveUpdateMessage(recieved);
                }
            }
        }
        public Node CreateNode(JToken info, bool createByThisClient = false)
        {
            if (Nodes.ContainsKey((string)info["id"])) return null;/*
        GameObject prefab = nodePrefabDict[message.info.shape+"Node"];
        var node = Instantiate(prefab).GetComponent<Node>();*/
            var node = Theme.ins.Create((string)info["shape"] + "Node").GetComponent<Node>(); ;
            node.createByThisClient = createByThisClient;
            node.Init(info);
            return node;
        }


    }
}