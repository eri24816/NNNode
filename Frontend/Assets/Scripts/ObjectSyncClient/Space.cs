using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
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
        readonly ISpaceClient spaceClient;
        public bool connectToServer = true;// Set this to false when debugging and don't want to connect to server.

        public Dictionary<string, Object> objs;

        readonly string WSPath = "ws://localhost:1000/";
        readonly string env_name = "my_env";
        WebSocket lobby, env;
        readonly ConcurrentQueue<string> messagesFromServer;
        readonly ConcurrentQueue<string> avaliableIds;

        public Space(ISpaceClient spaceClient)
        {
            this.spaceClient = spaceClient;
            messagesFromServer = new ConcurrentQueue<string>();
            avaliableIds = new ConcurrentQueue<string>();
            objs = new Dictionary<string, Object>();

            if (connectToServer)
            {
                lobby = new WebSocket(WSPath + "lobby");
                lobby.Connect();
                lobby.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
                lobby.Send("stt " + env_name);

                env = new WebSocket(WSPath + "space/" + env_name);
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

        public void Undo(string id)
        {
            SendMessage("{\"command\":\"undo\",\"id\":\"" + id + "\"}");
        }

        public void Redo(string id)
        {
            SendMessage("{\"command\":\"redo\",\"id\":\"" + id + "\"}");
        }

        private void Update()
        {
            if (avaliableIds.Count < 5)
            {
                if (connectToServer)
                    SendMessage("{\"command\":\"get id\"}"); // request for an unused id
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
        
        public Object CreateObject(JToken message)
        {
            if (objs.ContainsKey((string)message["id"])) return null;

            IObjectClient objectClient = spaceClient.CreateHasObject(message);
            Object newObj = new Object(this, message, objectClient);

            objectClient.OnCreate(message, newObj);
            
            return newObj;
        }
    }
}