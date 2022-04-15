using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync
{
    public class Object
    {
        public readonly string type;
        public readonly Space space;
        public readonly IObjectClient objectClient;
        public readonly string id;
        public bool history_on = true;
        public Dictionary<string, IAttribute> Attributes { get; private set; }

        public HashSet<Object> children =new();
        string lastParent = null;
        public Object(Space space,JToken d, IObjectClient objectClient)
        {
            type = (string)d["type"];
            this.space = space;
            this.objectClient = objectClient;
            Attributes = new Dictionary<string, IAttribute>();

            id = (string)d["id"];

            foreach (var pair in (JObject)d["attributes"])
            {
                string name = pair.Key;
                var attr_info = pair.Value;
                var new_attr = space.attributeFactory.Produce(this, name, (string)attr_info["type"]);
                new_attr.Set(attr_info["value"], false);
                Attributes.Add(name, new_attr);
            }
            RegisterAttribute<string>("parent_id", OnParentChanged, "none");
        }

        private void OnParentChanged(string new_value)
        {
            if (lastParent != null) {  space.objs[lastParent].children.Remove(this);}
            
            if (new_value != null) space.objs[new_value].children.Add(this);
            lastParent = new_value;
        }

        public Attribute<T> RegisterAttribute<T>(string name, System.Action<T> listener = null, string history_object = "none", T initValue = default)
        {
            if (Attributes.ContainsKey(name))
            {
                Attribute<T> attr = (Attribute<T>)Attributes[name];
                if (listener != null)
                {
                    attr.OnSet+=listener;
                    listener(attr.Value);
                }
                return attr;
            }
            else 
            {
                Attribute<T> a = new(this, name);
                Attributes[name] = a;

                SendMessage(new API.Out.NewAttribute<T> { command = "new attribute", id = id, name = a.name, type = a.type, history_object = history_object, value = initValue });
                
                if(listener != null)
                    a.OnSet += listener;
                a.Set(initValue, false);
                return a;
            }
        }
        public void DeleteAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                SendMessage(new API.Out.DeleteAttribute { id = id, name = name });
                Attributes.Remove(name);
            }
        }
        public void RecieveMessage(JToken message)
        {
            string command = (string)message["command"];
            if(command == "attribute")
            {
                Attributes[(string)message["name"]].Recieve(message);
            }
            else if(command == "new attribute")
            {
                if (!Attributes.ContainsKey((string)message["name"]))
                {
                    var new_attr = space.attributeFactory.Produce(this, (string)message["name"], (string)message["type"]);
                    new_attr.Set(message["value"], false);
                    Attributes.Add((string)message["name"], new_attr);
                }
            }
            else if (command == "delete attribute")
            {
                if (Attributes.ContainsKey((string)message["name"]))
                    Attributes.Remove((string)message["name"]);
            }
            objectClient.RecieveMessage(message);
        }
        public void SendMessage(object message)
        {
            space.SendMessage(message);
        }
        public void OnDestroy(JToken message)
        {
            foreach (var c in children)
            {
                c.OnDestroy(message);
            }
            space.objs.Remove(id);
            objectClient.OnDestroy_(message);
        }
        public void Update()
        {
            foreach(var a in Attributes)
                a.Value.Update();
        }

        public void Undo()
        {
            SendMessage("{\"command\":\"undo\",\"id\":\"" + id + "\"}");
        }
        public void Redo()
        {
            SendMessage("{\"command\":\"redo\",\"id\":\"" + id + "\"}");
        }
        public void SendHistoryOn()
        {
            SendMessage("{\"command\":\"history on\",\"id\":\"" + id + "\"}");
        }
        public void SendHistoryOff()
        {
            SendMessage("{\"command\":\"history off\",\"id\":\"" + id + "\"}");
        }

        public NoHisyory NoHistory_()
        {
            return new NoHisyory(this);
        }
        public class NoHisyory : System.IDisposable
        {
            readonly Object obj;
            public NoHisyory(Object obj)
            {
                this.obj = obj;
                obj.history_on = false;
                obj.SendHistoryOff();
            }
            public void Dispose()
            {
                obj.history_on = true;
                obj.SendHistoryOn();
            }
        }

        public void Tag(string tag)
        {
            string q = $"tag/{tag}";
            if (!Attributes.ContainsKey(q))
            {
                RegisterAttribute<string>(q);
            }
        }
        public void Untag(string tag)
        {
            string q = $"tag/{tag}";
            if (Attributes.ContainsKey(q))
            {
                DeleteAttribute(q);
            }
        }
        public bool TaggedAs(string tag)
        {
            string q = $"tag/{tag}";
            return Attributes.ContainsKey(q);
        }
    }
}