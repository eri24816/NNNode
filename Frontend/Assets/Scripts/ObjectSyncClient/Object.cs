using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync
{
    namespace API
    {
        
    }
    public class Object
    {
        readonly Space space;
        readonly IObjectClient objectClient;
        public readonly string id;
        public Dictionary<string, Attribute> Attributes { get; private set; }
        public Object(Space space,JToken message, IObjectClient objectClient)
        {
            this.space = space;
            JToken d = message["d"];
            this.objectClient = objectClient;
            id = (string)d["id"];

            Attributes = new Dictionary<string, Attribute>();

            foreach (var attr_info in d["attributes"])
            {
                var new_attr = new Attribute(this, (string)attr_info["name"], (string)attr_info["type"]);
                new_attr.Set(JsonHelper.JToken2type(attr_info["value"], new_attr.type), false);
            }
        }
        public Attribute RegisterAttribute(string name, string type, System.Action<object> onSet = null, object initValue = null, string history_Object = "node")
        {
            if (Attributes.ContainsKey(name))
            {
                Attribute attr = Attributes[name];
                if (onSet != null)
                {
                    attr.OnSet+=onSet;
                    onSet(attr.Value);
                }
                return attr;
            }
            else
            {
                void SendNat<T>() => SendMessage(new API.Out.NewAttribute<T> { command = "new attribute", id = id, name = name, type = type, history_object = history_Object, value = initValue == null ? default(T) : (T)initValue });
                switch (type)
                {
                    case "string":
                        SendNat<string>(); break;
                    case "float":
                        SendNat<float>(); break;
                    case "Vector3":
                        SendNat<UnityEngine.Vector3>(); break;
                    case "bool":
                        SendNat<bool>(); break;
                }

                Attribute a = new Attribute(this,name,type);
                a.OnSet += onSet;
                a.Set(initValue, false);
                return a;
            }
        }
        public void RecieveMessage(JToken message)
        {
            string command = (string)message["command"];
            if(command == "attribute")
            {
                Attributes[(string)message["name"]].Recieve(message);
            }
            objectClient.RecieveMessage(message);
        }
        public void SendMessage(object message)
        {
            space.SendMessage(message);
        }
        public void Update()
        {
            foreach(var a in Attributes)
                a.Value.Update();
        }
    }
}