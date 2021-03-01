using UnityEngine;
namespace APIMessage
{


    class NewCodeNode
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string name;
            public string type;
            public float[] pos;
        }
        public Info info;
        public NewCodeNode(string name,  Vector3 pos) { info.name = name; info.type = "CodeNode"; info.pos = new float[] { pos.x, pos.y, pos.z }; }
        public string Json { get => JsonUtility.ToJson(this); }
    }

    class NewFunctionNode
    {
        public string command = "new";
        [System.Serializable]
        public struct Info
        {
            public string name;
            public string type;
            public float[] pos;
        }
        public Info info;
        public NewFunctionNode( string name, Vector3 pos) { info.name = name; info.type = "FunctionNode"; info.pos = new float[] { pos.x, pos.y, pos.z }; }
        public string Json { get => JsonUtility.ToJson(this); }
    }


    class Mov
    {
        public Mov(string node_name,Vector3 pos)
        {
            this.node_name = node_name; this.pos = new float[] { pos.x, pos.y, pos.z };
        }
        public string command = "mov";
        public string node_name;
        public float[] pos = new float[3];
        public string Json { get => JsonUtility.ToJson(this); }

    }

    class Rmv
    {
        public Rmv(string node_name, Vector3 pos)
        {
            this.node_name = node_name;
        }
        public struct Info
        {
            public string name;
        }
        public string command = "rmv";
        public string node_name;
        public string Json { get => JsonUtility.ToJson(this); }

    }
}