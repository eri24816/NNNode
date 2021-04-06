using UnityEngine;
using GraphUI;
namespace APIMessage
{


    class NewCodeNode
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string name;
            public string type;
            public float[] pos;
        }
        public Info info;
        // TODO: directly take node as argument
        public NewCodeNode(string id,string name,  Vector3 pos) { info.id = id; info.name = name; info.type = "CodeNode"; info.pos = new float[] { pos.x, pos.y, pos.z }; }
        public string Json { get => JsonUtility.ToJson(this); }
    }

    class NewFunctionNode
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string id;
            public string name;
            public string type;
            public float[] pos;
        }
        public Info info;
        public NewFunctionNode(string id, string name, Vector3 pos) { info.id = id; info.name = name; info.type = "FunctionNode"; info.pos = new float[] { pos.x, pos.y, pos.z }; }
        public string Json { get => JsonUtility.ToJson(this); }
    }

    class NewControlFlow
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

    class NewDataFlow
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


    class Mov
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

    class Cod
    {
        public Cod(string id, string code)
        {
            this.id = id; this.code=code;
        }
        public string command = "cod";
        public string id;
        public string code;
        public string Json { get => JsonUtility.ToJson(this); }

    }

    class Rmv
    {
        public Rmv(string id)
        {
            this.id = id;
        } 
        public string command = "rmv";
        public string id;
        public string Json { get => JsonUtility.ToJson(this); }
         
    }
    class Gid
    {
        
        public string command = "gid";
        public string id="";
        public string Json { get => JsonUtility.ToJson(this); }

    }
}