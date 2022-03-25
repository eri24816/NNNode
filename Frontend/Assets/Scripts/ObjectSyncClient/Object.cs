using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync
{
    public class Object
    {
        readonly Space space;
        readonly IObjectClient objectClient;
        public readonly string id;
        public Dictionary<string, IAttribute> Attributes { get; private set; }
        public Object(Space space,JToken message, IObjectClient objectClient)
        {
            this.space = space;
            this.objectClient = objectClient;
            Attributes = new Dictionary<string, IAttribute>();

            JToken d = message["d"];
            id = (string)d["id"];

            foreach (var attr_info in d["attributes"])
            {
                var new_attr = space.attributeFactory.Produce(this, (string)attr_info["name"], (string)attr_info["type"]);
                new_attr.Set(attr_info["value"], false);
            }
        }
        public Attribute<T> RegisterAttribute<T>(string name, System.Action<T> onSet = null, T initValue = default, string history_Object = "node")
        {
            if (Attributes.ContainsKey(name))
            {
                Attribute<T> attr = (Attribute<T>)Attributes[name];
                if (onSet != null)
                {
                    attr.OnSet+=onSet;
                    onSet(attr.Value);
                }
                return attr;
            }
            else
            {
                Attribute<T> a = new Attribute<T>(this, name);
                Attributes[name] = a;

                SendMessage(new API.Out.NewAttribute<T> { command = "new attribute", id = id, name = a.name, type = a.type, history_object = history_Object, value = initValue });
                
                if(onSet != null)
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