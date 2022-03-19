using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync
{
    namespace API
    {
        public struct NewAttribute<T> { public string command, id, name, type, h; public T value; } // new attribute
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

            foreach (var attr_info in d["attr"])
            {
                var new_attr = new Attribute(this, (string)attr_info["name"], (string)attr_info["type"]);
                new_attr.Set(JsonHelper.JToken2type(attr_info["value"], new_attr.type), false);
            }
        }
        public Attribute RegisterAttr(string name, string type, Attribute.SetDelegate setDel = null, object initValue = null, string history_in = "node")
        {
            if (Attributes.ContainsKey(name))
            {
                Attribute attr =Attributes[name];
                if (setDel != null)
                {
                    attr.OnSet+=setDel;
                    setDel(attr.Value);
                }
                return attr;
            }
            else
            {
                void SendNat<T>() => SendMessage(new API.NewAttribute<T> { command = "new attribute", id = id, name = name, type = type, h = history_in, value = initValue == null ? default(T) : (T)initValue });
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
                a.OnSet += setDel;
                a.Set(initValue, false);
                return a;
            }
        }
        public void RecieveMessage(JToken message)
        {
            objectClient.RecieveMessage(message);
        }
        public void SendMessage(object message)
        {
            space.SendMessage(message);
        }
    }
}