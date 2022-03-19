using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync 
{
    namespace API
    {
        public struct Attribute<T> { public string id, command, name; public T value; }
        public class UpdateMessage
        {
            public UpdateMessage(string id, string command, string info)
            {
                this.id = id; this.command = command; this.info = info;
            }
            public string command;
            public string id;
            public string info;
        }
    }
    public class Attribute 
    {
        public object Value { get; private set; } // If delegate GetValue is not null, this field will not be used.

        readonly Object obj;
        public string type; // For inspector to know how to generate the attribute editor
        public readonly string name;
        readonly CoolDown recvCD, sendCD;
        object recievedValue;

        // Delay recieving value after sending value
        readonly float delay = 1;

        public delegate void SetDelegate(object value);
        public event SetDelegate OnSet;

        public Attribute(Object obj,string name, string type)
        {
            this.obj = obj;
            this.name = name; // Name format: category1/category2/.../attr_name
            this.type = type;
            recvCD = new CoolDown(3);
            sendCD = new CoolDown(2); // Avoid client to upload too frequently e.g. upload the code everytime the user key in a letter.
        }
        bool setLock = false;
        class SetLock : System.IDisposable
        {
            Attribute attr;
            public SetLock(Attribute attr) { attr.setLock = true; this.attr = attr; }
            public void Dispose()
            {
                attr.setLock = false;
            }
        }
        public void Set(object value = null, bool send = true)
        {
            if (setLock) return;
            using (new SetLock(this)) // Avoid recursive Set() call
            {
                Value = value;
                OnSet.Invoke(Value);
                if (send) Send();
            }
        }
        public void Recieve(JToken message)
        {
            recievedValue = JsonHelper.JToken2type(message["value"], type);
            recvCD.Request();
        }
        public void Send()
        {
            sendCD.Request();
            recvCD.Delay(delay);
        }

        // Call this constantly
        public void Update()
        {
            if (recvCD.Update())
            {
                Set(recievedValue, false);
            }
            if (sendCD.Update())
            {
                if (type == "Vector3")
                    Send<UnityEngine.Vector3>();
                else if (type == "float")
                    Send<float>();
                else if (type == "string" || (type.Length >= 8 && type.Substring(0, 8) == "dropdown"))
                    Send<string>();
                else if (type == "bool")
                    Send<bool>();
            }
        }
        public void Send<T>()
        {
            obj.SendMessage(new API.Attribute<T> { id = obj.id, command = "atr", name = name, value = (T)Value });
        }
    }
}