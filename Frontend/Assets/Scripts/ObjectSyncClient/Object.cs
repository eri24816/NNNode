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
        public Dictionary<string, Attribute> attributes { get; private set; }
        public Object(Space space,JToken message, IObjectClient objectClient)
        {
            this.space = space;
            JToken d = message["d"];
            this.objectClient = objectClient;
            id = (string)d["id"];

            attributes = new Dictionary<string, Attribute>();

            foreach (var attr_info in d["attr"])
            {
                var new_attr = new Attribute(this, (string)attr_info["name"], (string)attr_info["type"], null, null, null);
                new_attr.Set(JsonHelper.JToken2type(attr_info["value"], new_attr.type), false);
            }
        }
        public Attribute RegisterAttr(string name, string type, Attribute.SetDelegate setDel = null, object initValue = null, string history_in = "node")
        {
            if (Attributes.ContainsKey(name))
            {
                Attribute attr = Attributes[name];
                if (setDel != null)
                {
                    attr.OnSet+=setDel;
                    setDel(attr.Value);
                }
                return attr;
            }
            else
            {
                void SendNat<T>() => SendMessage(new Attribute.API_nat<T> { command = "new attribute", id = id, name = name, type = type, h = history_in, value = initValue == null ? default(T) : (T)initValue });
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

        public virtual void Init(JToken d)
        {


            
           
            if (createByThisClient)
                Space.ins.SendToServer(new API_new(this));
            else
                // If createByThisClient, set Pos attribute after the node is dropped to its initial position (in OnDragCreating()).
                Pos = Attribute.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; }, history_in: "env");
            Output = Attribute.Register(this, "output", "string", (v) => { OnOutputChanged((string)v); }, history_in: "", initValue: "");

            foreach (var attr_info in d["attr"])
            {
                var new_attr = new Attribute(this,(string)attr_info["name"], (string)attr_info["type"]);
                new_attr.Set(JsonHelper.JToken2type(attr_info["value"], new_attr.type), false);
            }

            Attribute.Register(this, "color", "Vector3", (v) => { var w = (Vector3)v; SetColor(new Color(w.x, w.y, w.z)); }, history_in: "");


            foreach (var portInfo in d["portInfos"])
            {
                CreatePort(portInfo);
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