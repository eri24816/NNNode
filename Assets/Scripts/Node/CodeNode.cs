using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI
{
    public class CodeNode : Node
    {
        public bool expanded = false;

        [SerializeField]
        TMPro.TMP_InputField nameInput;

        [SerializeField]
        GameObject CodeEditor;

        [SerializeField]
        TMPro.CodeEditor CodeEditorScript;
        [SerializeField]
        TMPro.TextMeshProUGUI outputText;
        [SerializeField]
        UnityEngine.UI.LayoutElement outputTextLayoutElement;
        readonly CoolDown recvCodeCD=new CoolDown(hz:100);
        string output="";
        public float maxOutputHeight = 150;
        string recievedCode;
        public string Code { get { return CodeEditorScript.text; } set { CodeEditorScript.SetTextWithoutNotify(value); } }

        public override void Update()
        {
            base.Update();
            if (recvCodeCD.Update())
            {
                Code = recievedCode;
            }
        }
        public override void Start()
        {
            base.Start();
            nameInput.enabled = false;
            nameInput.text = Name;
        }

        public void Rename()
        {
            nameInput.enabled = true;
            nameInput.Select();
        }
        public void RecieveCode(string code)
        {
            recvCodeCD.Request();
            recievedCode = code;
        }
        public void RenameEnd()
        {
            nameInput.enabled = false;
        }
        public void CollapseOrExpand()
        {
            expanded ^= true;
            CodeEditor.SetActive(expanded);
            if (expanded)
            {
                outputTextLayoutElement.minHeight = Mathf.Min(maxOutputHeight, outputText.textBounds.size.y);
                ShowOutput(output);
            }
            else
            {
                outputTextLayoutElement.minHeight = 0;
                outputText.text="";
            }
                
        }

        public override Port GetPort(bool isInput = true, string var_name = "")
        {
            return isInput ? ins[0] : outs[0];
        }
        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
        public void SetCode()// called by the code editor
        {
            recvCodeCD.Delay(1);
            Manager.ins.SetCode(this);
        }
        public override void ShowOutput(string output)
        {
            this.output = output;
            if (expanded)
            {
                outputText.overflowMode = TMPro.TextOverflowModes.Overflow;
                outputText.text = output;
                outputText.ForceMeshUpdate();
                outputText.overflowMode = TMPro.TextOverflowModes.Truncate;
                outputTextLayoutElement.minHeight = Mathf.Min(maxOutputHeight, outputText.textBounds.size.y);
            }
        }
    }
}