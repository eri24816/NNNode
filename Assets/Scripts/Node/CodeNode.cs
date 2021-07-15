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

        [SerializeField]
        TMPro.CodeEditor CodeEditorScript;

        protected string recievedCode;
        NodeAttr Code;
        public override void Init(string infoJSON,string id = null)
        {
            base.Init(infoJSON,id);
            Code = NodeAttr.Register(this, $"{name}/code", "string",
                (v) => { CodeEditorScript.SetTextWithoutNotify((string)v); },
                ()=> { return CodeEditorScript.text; });
            var info = JsonUtility.FromJson<API_new_CodeNode>(infoJSON);
        }

        public void SetCode()// called by the code editor
        {
            Code.Send();
        }
    }
}
