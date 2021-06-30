using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI {
    public class CodeNode : Node
    {
        /*
            Base class of SimpleCodeNode and BigCodeNode
        */
        [SerializeField]
        TMPro.CodeEditor CodeEditorScript;

        readonly CoolDown recvCodeCD = new CoolDown(hz: 10);

        protected string recievedCode;
        public string Code { get { return CodeEditorScript.text; } set { CodeEditorScript.SetTextWithoutNotify(value); } }
        public override void Update()
        {
            base.Update();
            if (recvCodeCD.Update())
            {
                Code = recievedCode;
            }
        }

        public void RecieveCode(string code)
        {
            recvCodeCD.Request();
            recievedCode = code;
        }

        public void SetCode()// called by the code editor
        {
            recvCodeCD.Delay(1);
            Manager.ins.SetCode(this);
        }
    }
}
