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
            public class DeleteAttribute
            {
                public string command = "delete attribute", id, name;
            }
            public class Attribute<T>
            {
                public string command = "attribute";
                public string id, name; public T value;
            }
            public class Create
            {
                public string command = "create";

                public string parent="0";
                public D d;
                [System.Serializable]
                public struct D
                {
                    public string type;
                    public Dictionary<string,Attribute> attributes;
                }
                [System.Serializable]
                public class Attribute
                {
                    public string  history_object;
                    public string type { get { return value.GetType().Name; } }
                    public object value;
                }
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
                    public string type;
                }
            }
        }
    }

    public interface IObjectClient
    {
        public void RecieveMessage(JToken message);
        public void OnCreate(JToken message, Object obj);
        public void OnDestroy_(JToken message); // OnDestroy is Unity message
    }

    public interface ISpaceClient
    {
        public void RecieveMessage(JToken message);
        public IObjectClient CreateObjectClient(JToken d);
        public void OnObjectCreated(Object obj);
    }

    public class Space
    {
        public readonly ISpaceClient spaceClient;

        public Dictionary<string, Object> objs;

        readonly WebSocket ws;

        readonly ConcurrentQueue<string> messagesFromServer;

        public AttributeFactory attributeFactory;

        readonly string route;

        public JToken metadata;

        public bool flush = true;

        public Space(ISpaceClient spaceClient, string route, AttributeFactory attributeFactory = null)
        {
            this.spaceClient = spaceClient;
            messagesFromServer = new ConcurrentQueue<string>();
            objs = new Dictionary<string, Object>();


            ws = new WebSocket(route);
            ws.Connect();
            ws.OnMessage += (sender, e) => messagesFromServer.Enqueue(e.Data);

            this.attributeFactory = attributeFactory ?? new AttributeFactory();

            this.route = route;

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"Connected to space {route}");
#endif
        }

        public void SendMessage(object obj)
        {
            if (obj.GetType() == typeof(string))
                ws.Send((string)obj);
            else
                ws.Send(JsonConvert.SerializeObject(obj));
        }

        public int nameNum = 0;
        public void SendDestroy(string id)
        {
            SendMessage("{\"command\":\"destroy\",\"id\":\"" + id + "\"}");
        }
        public void SendFlushOn()
        {
            SendMessage("{\"command\":\"flush on\"}");
        }
        public void SendFlushOff()
        {
            SendMessage("{\"command\":\"flush off\"}");
        }

        public void Update()
        {
            /* Call this constantly */

            while (messagesFromServer.TryDequeue(out string receivedString))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"{route} > {receivedString}");
#endif
                if (receivedString[0] != '{') return; // Filter out debugging message
                var message = JObject.Parse(receivedString);
                string command = (string)message["command"];

                if (command == "space_metadata")
                {
                    metadata = message;
                }
                else if(command == "load")
                {
                    var rootObject = message["root_object"];
                    Create(rootObject);
                }
                else if (command == "create")
                {
                    Create(message["d"]);
                }
                else if (command == "destroy")
                {
                    var id = (string)message["id"];
                    objs[id].OnDestroy(message); 
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
        
        public Object Create(JToken d)
        {
            if(objs.ContainsKey((string)d["id"]))
            {
                throw new System.Exception("id exists");
            }

            IObjectClient objectClient = spaceClient.CreateObjectClient(d);
            Object newObj = new(this, d, objectClient);

            objectClient.OnCreate(d, newObj);

            objs.Add((string)d["id"], newObj);


            foreach (var child_d in d["children"])
            {
                Create(child_d);
                //newObj.children.Add(Create(child_d));
            }

            spaceClient.OnObjectCreated(newObj);

            return newObj;
        }
        public void Close()
        {
            try
            {
                //ws.Close(CloseStatusCode.Normal, "disconnect from space");
            }
            catch(System.Exception)
            {

            }
        }
        ~Space() {
            Close();
        }

        public CommandSequence CommandSequence_()
        {
            return new CommandSequence(this);
        }
        public class CommandSequence : System.IDisposable
        {
            Space space;
            public CommandSequence(Space space)
            {
                this.space = space;
                space.flush = false;
                space.SendFlushOff();
            }
            public void Dispose()
            {
                space.flush=true;   
                space.SendFlushOn();
            }
        }
    }
}