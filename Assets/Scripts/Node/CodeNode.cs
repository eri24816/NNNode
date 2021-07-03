using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI {
    public class CodeNode : Node
    {
        /*
            Base class of SimpleCodeNode and BigCodeNode
        */
        
        public struct API_new_CodeNode
        {
            [System.Serializable]
            public struct Info
            {
                public string code;
            }
            public Info info;
        }
        /*
        public class API_cod
        {
            public API_cod(CodeNode node)
            {
                id = node.id; info = node.Code_;
            }
            public string command = "cod";
            public string id;
            public string info;
        }
        */
        [SerializeField]
        TMPro.CodeEditor CodeEditorScript;

        //readonly CoolDown recvCodeCD = new CoolDown(hz: 10);

        protected string recievedCode;
        //public string Code_ { get { return CodeEditorScript.text; } set { CodeEditorScript.SetTextWithoutNotify(value); } }
        NodeAttr<string> Code;
        public override void Init(string infoJSON,string id = null)
        {
            base.Init(infoJSON,id);
            Code = new NodeAttr<string>(this, "code", 
                (v) => { CodeEditorScript.SetTextWithoutNotify(v); },
                ()=> { return CodeEditorScript.text; });
            var info = JsonUtility.FromJson<API_new_CodeNode>(infoJSON);
            //Code_ = info.info.code;
        }

        public override void Update()
        {
            base.Update();
            /*
            if (recvCodeCD.Update())
            {
                Code_ = recievedCode;
            }*/
        }

        public override void RecieveUpdateMessage(string message,string command)
        {
            switch (command)
            {/*
                case "cod":
                    recvCodeCD.Request();
                    recievedCode = JsonUtility.FromJson<API_cod>(message).info;
                    break;*/
                default:
                    base.RecieveUpdateMessage(message, command);
                    break;
            }
        }
        
        public void SetCode()// called by the code editor
        {
            Code.Send();
        }
    }
}
