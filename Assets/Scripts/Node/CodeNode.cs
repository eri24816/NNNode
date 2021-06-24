using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class CodeNode : FunctionNode
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
        [SerializeField]
        List<GameObject> toHideOnMinimize;
        [SerializeField]
        float expandedWidth, minimizedWidth;
        public float maxOutputHeight = 150;

        readonly CoolDown recvCodeCD = new CoolDown(hz: 10);
        string output = "";
        
        string recievedCode;
        public string Code { get { return CodeEditorScript.text; } set { CodeEditorScript.SetTextWithoutNotify(value); } }

        public override void Init(APIMessage.NewNode.Info info)
        {
            base.Init(info);
            nameInput.text = name;
            Code = info.code;
        }

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
            foreach (GameObject g in toHideOnMinimize)
                g.SetActive(expanded);
            ((RectTransform)transform).sizeDelta = new Vector2(expanded ? expandedWidth : minimizedWidth, ((RectTransform)transform).sizeDelta.y);
            if (expanded)
            {
                UpdateOutputText();
            }
        }

        public void SetCode()// called by the code editor
        {
            recvCodeCD.Delay(1);
            Manager.ins.SetCode(this);
        }

        public void AddOutput(string output)
        {
            this.output += output;
            if (expanded)
                UpdateOutputText();
        }
        public void ClearOutput()
        {
            this.output ="";
            if (expanded)
                UpdateOutputText();
        }
        void UpdateOutputText()
        {
            outputText.overflowMode = TMPro.TextOverflowModes.Overflow;
            outputText.text = output;
            outputText.ForceMeshUpdate();
            outputText.overflowMode = TMPro.TextOverflowModes.Truncate;
            outputTextLayoutElement.minHeight = Mathf.Min(maxOutputHeight, outputText.textBounds.size.y);
        }
    }
}
