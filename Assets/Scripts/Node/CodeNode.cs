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

        
        public string Code { get { return CodeEditorScript.text; } set { CodeEditorScript.SetTextWithoutNotify( value); } }

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
        public void RenameEnd()
        {
            nameInput.enabled = false;
        }
        public void CollapseOrExpand()
        {
            expanded ^= true;
            CodeEditor.SetActive(expanded);
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
            Manager.ins.SetCode(this);
        }
    }
}