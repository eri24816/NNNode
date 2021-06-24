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
            public string[] in_names;
            public string[] out_names;
            public bool[] allow_multiple_in_data;
            public string code;
            public string frontend_type;
        }
        public Info info;
        // TODO: directly take node as argument
        public NewNode(string id,string type,string name,  Vector3 pos) { info.id = id; info.name = name; info.type = type; info.pos = pos;}
        public string Json { get => JsonUtility.ToJson(this); }
    }


    public class NewControlFlow
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string type;
            public string head;
            public string tail;
        }
        public Info info;
        public NewControlFlow(ControlFlow flow) { info.id = flow.id; info.type = "ControlFlow"; info.head = flow.head.node.id; info.tail = flow.tail.node.id; }
        public string Json { get => JsonUtility.ToJson(this); }
    }

    public class NewDataFlow
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string type;
            public string head;
            public string tail;
            public string head_var;
            public string tail_var;
        }
        public Info info;
        public NewDataFlow(DataFlow flow) { info.id = flow.id; info.type = "ControlFlow"; info.head = flow.head.node.id; info.tail = flow.tail.node.id;
            //TODO: head_var,tail_var
                }
        public string Json { get => JsonUtility.ToJson(this); }
    }


    public class Mov
    {
        public Mov(string id,Vector3 pos)
        {
            this.id = id; this.pos = new float[] { pos.x, pos.y, pos.z };
        }
        public string command = "mov";
        public string id;
        public float[] pos = new float[3];
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