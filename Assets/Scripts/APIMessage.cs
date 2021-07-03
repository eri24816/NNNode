using UnityEngine;
namespace APIMessage
{


    public class NewNode
    {
        // only for recieving.
        // making command "new" is done by node.
        [System.Serializable]
        public struct Info
        {
            public string frontend_type;
        }
        public Info info;
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
    }

    public struct UpdateMessage
    {
        public string id, command;
    }

    public class Gid
    {
        
        public string command = "gid";
        public string id="";
        public string Json { get => JsonUtility.ToJson(this); }

    }
}