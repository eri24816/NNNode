using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class CoolDown
{
    float span;
    System.DateTime waitTime = System.DateTime.Now;
    bool pending = false;
    public CoolDown(float hz)
    {
        span = 1f / hz;
    }
    public void Request()
    {
        pending = true;
    }
    public void Delay(float t = -1)
    {
        waitTime = System.DateTime.Now.AddSeconds(t == -1 ? span : t);
        //pending = false;
    }
    public bool Update()
    {
        if (pending && System.DateTime.Now > waitTime)
        {
            pending = false;
            waitTime = System.DateTime.Now.AddSeconds(span);
            return true;
        }
        return false;
    }
}
namespace ObjectSync 
{
    public class AttributeFactory
    {
        public virtual IAttribute Produce(Object obj,string name, string typeName)
        {
            return typeName switch
            {
                "String" => new Attribute<string>(obj, name),
                "int" => new Attribute<int>(obj, name),
                "float" => new Attribute<float>(obj, name),
                "Boolean" => new Attribute<bool>(obj, name),
                "Vector3" => new Attribute<UnityEngine.Vector3>(obj, name),
                _ => throw new System.NotSupportedException($"type not supported : {typeName}")
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
                if(OnSet!=null)
                    OnSet(Value);
                if (send) Send();
            }
        }
        public void Set(JToken value, bool send = true)
        {
            Set(value.ToObject<T>(), send);
            /*
            try
            {
                Set(value.ToObject<T>(), send); 
            }
            catch
            {
                throw new System.InvalidCastException($"Cast failed. SrcType : {value.GetType()} DstType : {typeof(T).Name} Value : {value}");
            }*/
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
                obj.SendMessage(new API.Out.Attribute<T> { id = obj.id, command = "attribute", name = name, value = value });
            }
        }
    }
}