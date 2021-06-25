using UnityEngine;
using GraphUI;
namespace APIMessage
{


    public class NewNode
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string name;
            public string type;
            public Vector3 pos;
            public string code;
            public string frontend_type;
            public string[] portInfos; // PortInfo classes (or structs?) are defined in each node classes
        }
        public Info info;
        // TODO: directly take node as argument
        public NewNode(string id,string type,string name,  Vector3 pos) { info.id = id; info.name = name; info.type = type; info.pos = pos;}
        public string Json { get => JsonUtility.ToJson(this); }
    }


    public class NewFlow
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string type;
            public string head;
            public string tail;
            public int head_port_id;
            public int tail_port_id;
        }
        public Info info;
        public NewFlow(Flow flow) { info.id = flow.id; info.type = flow.GetType().Name; info.head = flow.head.node.id; info.tail = flow.tail.node.id;
            info.head_port_id = flow.head.port_id; info.tail_port_id = flow.tail.port_id;
                }
        public string Json { get => JsonUtility.ToJson(this); }
    }


    public class Mov
    {
        public Mov(string id,Vector3 pos)
        {
            this.id = id; this.pos = pos;
        }
        public string command = "mov";
        public string id;
        public Vector3 pos;
        public string Json { get => JsonUtility.ToJson(this); }

    }

    public class Cod
    {
        public Cod(string id, string info)
        {
            this.id = id; this.info=info;
        }
        public string command = "cod";
        public string id;
        public string info;
        public string Json { get => JsonUtility.ToJson(this); }

    }

    public class UpdateMessage
    {
        public string command;
        public string id;
        public string info;
    }

    public class Rmv
    {
        public Rmv(string id)
        {
            this.id = id;
        } 
        public string command = "rmv";
        public string id;
        public string Json { get => JsonUtility.ToJson(this); }
         
    }
    public class Gid
    {
        
        public string command = "gid";
        public string id="";
        public string Json { get => JsonUtility.ToJson(this); }

    }
}