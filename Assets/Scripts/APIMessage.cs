using UnityEngine;
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