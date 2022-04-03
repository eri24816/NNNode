using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync
{
    public class Object
    {
        public readonly Space space;
        public readonly IObjectClient objectClient;
        public readonly string id;
        public Dictionary<string, IAttribute> Attributes { get; private set; }
        public Object(Space space,JToken d, IObjectClient objectClient)
        {
            this.space = space;
            this.objectClient = objectClient;
            Attributes = new Dictionary<string, IAttribute>();

            id = (string)d["id"];

            foreach (var attr_info in d["attributes"])
            {
                var new_attr = space.attributeFactory.Produce(this, (string)attr_info["name"], (string)attr_info["type"]);
                new_attr.Set(attr_info["value"], false);
                Attributes.Add((string)attr_info["name"], new_attr);
            }
        }
        public Attribute<T> RegisterAttribute<T>(string name, System.Action<T> onSet = null, string history_object = "none", T initValue = default)
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

                SendMessage(new API.Out.NewAttribute<T> { command = "new attribute", id = id, name = a.name, type = a.type, history_object = history_object, value = initValue });
                
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
        public void OnDestroy(JToken message)
        {
            objectClient.OnDestroy_(message);
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