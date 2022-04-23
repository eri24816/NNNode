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
class FriendlyTypeName
{
    // From https://stackoverflow.com/questions/401681/how-can-i-get-the-correct-text-definition-of-a-generic-type-using-reflection
    public static string GetFriendlyTypeName(System.Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var builder = new System.Text.StringBuilder();
        var name = type.Name;
        var index = name.IndexOf("`");
        builder.Append( name[..index]);
        builder.Append('<');
        var first = true;
        foreach (var arg in type.GetGenericArguments())
        {
            if (!first)
            {
                builder.Append(',');
            }
            builder.Append(GetFriendlyTypeName(arg));
            first = false;
        }
        builder.Append('>');
        return builder.ToString();
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
                "Stream" => new Attribute<StreamAttribute>(obj, name),

                "List<String>" => new Attribute<List<string>>(obj, name),
                "List<int>" => new Attribute<List<int>>(obj, name),
                "List<float>" => new Attribute<List<float>>(obj, name),
                "List<Boolean>" => new Attribute<List<bool>>(obj, name),
                "List<Vector3>" => new Attribute<List<UnityEngine.Vector3>>(obj, name),

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

        public readonly Object obj;
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
            type = FriendlyTypeName.GetFriendlyTypeName(typeof(T));
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
        public void Set(T value, bool send = true, bool sendImmediately = false)
        {
            if (!obj.space.flush || !obj.history_on) sendImmediately = true; // Space.NoFlush and Object.HistoryOff required attributes changes to be immediately sent to work correctly.
            if (value!=null && value.Equals(Value)) return;
            if (setLock) return;
            using (new SetLock(this)) // Avoid recursive Set() call through the callback.
            {
                if(this.value!=null)
                    send &= (!this.value.Equals(value));
                this.value = value;
                OnSet?.Invoke(Value);
                if (send)
                {
                    if (sendImmediately)
                    {
                        obj.SendMessage(new API.Out.Attribute<T> { id = obj.id, command = "attribute", name = name, value = value });
                    }
                    else
                        Send();
                }
            }
        }
        public void Set(JToken value, bool send = true)
        {
            Set(value.ToObject<T>(), send);
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

    // A special type of attribute that saves traffic
    public class StreamAttribute : Attribute<string>
    {
        public StreamAttribute(Object obj, string name) : base(obj, name)
        {
            type = "Stream";
        }
        public void ReceiveAdd(string value)
        {
            Set(Value + value, false);
        }
        public void Add(string value)
        {
            obj.SendMessage("{\"command\":\"add\",\"id\":\"" + obj.id + "\",\"name\":\"" + name + "\",\"value\":\"" + value + "\"}");
        }
        public void ReceiveClear()
        {
            Set("", false);
        }
        public void Clear()
        {
            obj.SendMessage("{\"command\":\"add\",\"id\":\"" + obj.id + "\",\"name\":\"" + name + "\"}");
        }
    }
}