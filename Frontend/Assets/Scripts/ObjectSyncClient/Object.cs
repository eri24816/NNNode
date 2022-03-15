using System.Collections.Generic;

namespace ObjectSync
{
    public class Object
    {
        public readonly string id;
        public string Type { get; private set; }
        public Dictionary<string, Attribute> attributes { get; private set; }
        Object()
        {
        }
        public Attribute RegisterAttr(string name, string type, Attribute.SetDel setDel = null, Attribute.GetDel getDel = null, object initValue = null, string history_in = "node")
        {
            if (attributes.ContainsKey(name))
            {
                Attribute attr = attributes[name];
                if (setDel != null)
                {
                    attr.setDel.Add(new System.Tuple<object, Attribute.SetDel>(setDel));
                    setDel(attr.Get());
                }
                if (getDel != null)
                    attr.getDel = getDel;

                return attr;
            }
            else
            {
                void SendNat<T>() => Manager.ins.SendToServer(new Attribute.API_nat<T> { command = "new attribute", id = id, name = name, type = type, h = history_in, value = initValue == null ? default(T) : (T)initValue });
                if (!isDemo)
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

                Attribute a = new Attribute(id ,name, type, setDel, getDel, comp);
                a.Set(initValue, false);
                return a;
            }
        }
    }
}