using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync 
{
    public class AttributeFactory
    {
        public virtual IAttribute Produce(Object obj,string name, string typeName)
        {
            return typeName switch
            {
                "string" => new Attribute<string>(obj, name),
                "int" => new Attribute<int>(obj, name),
                "float" => new Attribute<float>(obj, name),
                _ => throw new System.NotSupportedException("type not supported")
            };
        }
    }
    public class Attribute<T> : IAttribute
    {
        public object OValue { get { return value; } }
        public T Value { get { return value; } }
        private T value;
        private T recievedValue;

        readonly Object obj;
        public readonly string name;
        public string type; // For inspector to know how to generate the attribute editor
        readonly CoolDown recvCD, sendCD;
        
        // Delay recieving value after sending value
        readonly float delay = 1;

        public event System.Action<T> OnSet;
        public Attribute(Object obj, string name)
        {
            this.obj = obj;
            this.name = name; // Name format: category1/category2/.../attr_name
            type = typeof(T).Name;
            recvCD = new CoolDown(3);
            sendCD = new CoolDown(2); // Avoid client to upload too frequently e.g. upload the code everytime the user key in a letter.
        }
        bool setLock = false;
        class SetLock : System.IDisposable
        {
            readonly Attribute<T> attr;
            public SetLock(Attribute<T> attr) { attr.setLock = true; this.attr = attr; }
            public void Dispose()
            {
                attr.setLock = false;
            }
        }
        public void Set(T value, bool send = true)
        {
            if (setLock) return;
            using (new SetLock(this)) // Avoid recursive Set() call
            {
                this.value = value;
                OnSet.Invoke(Value);
                if (send) Send();
            }
        }
        public void Set(object value, bool send = true)
        {
            Set((T)value, send);
        }
        public void Recieve(JToken message)
        {
            recievedValue = (T)message["value"].ToObject(typeof(T));
            recvCD.Request();
        }
        public void Send()
        {
            sendCD.Request();
            recvCD.Delay(delay);
        }
        public void Update()
        {
            if (recvCD.Update())
            {
                Set(recievedValue, false);
            }
            if (sendCD.Update())
            {
                obj.SendMessage(new API.Out.Attribute<T> { id = obj.id, command = "atr", name = name, value = value });
            }
        }
    }
}