using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ObjectSync 
{
    public class Attribute 
    {
        public struct API_atr<T> { public string id, command, name; public T value; }
        public struct API_nat<T> { public string command, id, name, type, h; public T value; } // new attribute

        object value; // If delegate GetValue is not null, this field will not be used.
        public string type; // For inspector to know how to generate the attribute editor
        public readonly string name, id;
        readonly CoolDown recvCD, sendCD;
        object recievedValue;

        // Delay recieving value after sending value
        readonly float delay = 1;

        // Like get set of property
        public delegate void SetDel(object value);
        public List<SetDel> setDel;
        public delegate object GetDel();
        public GetDel getDel;

        public Attribute(string id,string name, string type, SetDel setDel, GetDel getDel)
        {
            this.id = id;
            this.name = name; // Name format: category1/category2/.../attr_name
            this.type = type;
            this.setDel = new List<SetDel>();
            if (setDel != null)
                this.setDel.Add(setDel);
            this.getDel = getDel;
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
                if (value != null)
                    this.value = value;
                else
                    this.value = getDel();
                foreach (var i in setDel)
                    i(this.value);
                if (send) Send();
            }
        }
        public object Get()
        {
            if (getDel != null) return getDel();
            return value;
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
            Space.ins.SendToServer(new API_atr<T> { id = id, command = "atr", name = name, value = (T)Get() });
        }
    }
}