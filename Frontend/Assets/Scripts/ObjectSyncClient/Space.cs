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
    public interface IObjectClient
    {
        public void RecieveMessage(JToken message);
        public void OnCreate(JToken message, Object obj);
        public void OnDestroy();
    }

    public interface ISpaceClient
    {
        public void RecieveMessage(JToken message);
        public IObjectClient CreateHasObject(JToken message);
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

    public class Space
    {
        ISpaceClient spaceClient;
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

        public Space(ISpaceClient hasSpace)
        {
            this.spaceClient = hasSpace;
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

        public void SendMessage(object obj)
        {
            if (connectToServer)
                env.Send(JsonUtility.ToJson(obj)); // Here I use UnityEngine.JsonUtility instead of Json.NET because the latter produces error converting Vector3.
        }

        string GetNewID()
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
                Debug.Log(WSPath + " says: " + receivedString);
                if (receivedString[0] != '{') return; // Filter out debugging message
                var recieved = JObject.Parse(receivedString);
                string command = (string)recieved["command"];
                if (command == "new")
                {
                    var id = (string)recieved["id"];
                    if (!objs.ContainsKey(id))
                        CreateObject(recieved);
                }
                else if (command == "gid")// get a unused id to assign to the object
                {
                    //var message = JsonUtility.FromJson<APIMessage.Gid>(received);
                    avaliableIds.Enqueue((string)recieved["id"]);
                }
                else // directly send update messages to the object
                {
                    var id = (string)recieved["id"];
                    if (objs.ContainsKey(id))
                        objs[id].RecieveMessage(recieved);
                }
            }
        }
        
        public Object CreateObject(JToken message, bool is_new = false)
        {
            if (objs.ContainsKey((string)message["id"])) return null;

            IObjectClient objectClient = spaceClient.CreateHasObject(message);
            Object newObj = new Object(this, message, objectClient);

            objectClient.OnCreate(message, newObj);
            
            return newObj;
        }
    }
}