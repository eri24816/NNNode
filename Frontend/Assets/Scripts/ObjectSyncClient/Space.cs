using System.Collections.Generic;
using WebSocketSharp;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ObjectSync
{
    namespace API
    {
        namespace Out
        {
            public class NewAttribute<T>
            {
                public string command = "new attribute", id, name, type, history_object;
                public T value;
            }
            public class Attribute<T>
            {
                public string command = "attribute";
                public string id, name; public T value;
            }
            public class Create
            {
                public string command = "create";
                public string type;
            }
        }
        namespace In
        {
            public class NewAttribute<T>
            {
                public string command = "new attribute", id, name, type;
                public T value;
            }
            public class Attribute<T>
            {
                public string command = "attribute";
                public string id, name; public T value;
            }
            public class Create
            {
                public string command = "create";
                public D d;
                [System.Serializable]
                public struct D
                {
                    public string id;
                    public string frontendType;
                }
            }
        }
    }

    public interface IObjectClient
    {
        public void RecieveMessage(JToken message);
        public void OnCreate(JToken message, Object obj);
        public void OnDestroy();
    }

    public interface ISpaceClient
    {
        public void RecieveMessage(JToken message);
        public IObjectClient CreateObjectClient(JToken message);

        // Define custom attribute type conversion
        public object ConvertJsonToType(JToken j, string type);
    }

    public class Space
    {
        readonly ISpaceClient spaceClient;

        public Dictionary<string, Object> objs;

        readonly WebSocket spaceWS;

        bool running = true;
        readonly ConcurrentQueue<string> messagesFromServer;

        public Space(ISpaceClient spaceClient, string route)
        {
            this.spaceClient = spaceClient;
            messagesFromServer = new ConcurrentQueue<string>();
            objs = new Dictionary<string, Object>();


            spaceWS = new WebSocket(route);
            spaceWS.Connect();
            spaceWS.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);
        }

        public void SendMessage(object obj)
        {
            spaceWS.Send(JsonConvert.SerializeObject(obj));
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
        public object JToken2type(JToken j, string type)
        {
            if (type.Length >= 8 && type.Substring(0, 8) == "dropdown") type = "string";
            return type switch
            {
                "string" => (string)j,
                "float" => (float)j,
                "bool" => (bool)j,
                _ => spaceClient.ConvertJsonToType(j,type),
            };
        }

        public void Update()
        {
            /* Call this constantly */

            while (messagesFromServer.TryDequeue(out string receivedString))
            {
                if (receivedString[0] != '{') return; // Filter out debugging message
                var message = JObject.Parse(receivedString);
                string command = (string)message["command"];

                if (command == "new")
                {
                    var id = (string)message["id"];
                    if (!objs.ContainsKey(id))
                        Create(message);
                }
                else // directly send update messages to the object
                {
                    var id = (string)message["id"];
                    if (objs.ContainsKey(id))
                        objs[id].RecieveMessage(message);
                }

                spaceClient.RecieveMessage(message);
            }
            foreach(var obj in objs)
            {
                obj.Value.Update();
            }
        }
        
        public Object Create(JToken message)
        {
            if( !objs.ContainsKey((string)message["id"]))
            {
                throw new System.Exception("id exists");
            }

            IObjectClient objectClient = spaceClient.CreateObjectClient(message);
            Object newObj = new Object(this, message, objectClient);

            objectClient.OnCreate(message, newObj);
            
            return newObj;
        }
    }
}