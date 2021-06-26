using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        Transform left, right, top, buttom;
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
            int i = 0;
            var rt = (RectTransform)transform;
            foreach (string portInfoJson in info.portInfos)
            {
                PortInfo portInfo = JsonUtility.FromJson<PortInfo>(portInfoJson);
                Port newPort;
                GameObject prefab;

                if (portInfo.type == "ControlPort")
                    prefab = portInfo.isInput ? Manager.ins.inControlPortPrefab : Manager.ins.outControlPortPrefab;
                else
                    prefab = portInfo.isInput ? Manager.ins.inDataPortPrefab : Manager.ins.outDataPortPrefab;
                
                Transform parent = null;
                if (portInfo.pos == Vector3.left)
                    parent = left;
                else if (portInfo.pos == Vector3.right)
                    parent = right;
                else if (portInfo.pos == Vector3.up)
                    parent = top;
                else if (portInfo.pos == Vector3.down)
                    parent = buttom;

                newPort = Instantiate(prefab, parent).GetComponent<Port>();

                ports.Add(newPort);
                newPort.port_id = i;
                newPort.isInput = portInfo.isInput;
                newPort.maxEdges = portInfo.max_connections;
                newPort.node = this;
                newPort.name = portInfo.name;
                newPort.nameText.text = "";
                i++;

            }
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
            foreach (GameObject g in toHideOnMinimize)
                g.SetActive(expanded);
            ((RectTransform)transform).sizeDelta = new Vector2(expanded ? expandedWidth : minimizedWidth, ((RectTransform)transform).sizeDelta.y);
            if (expanded)
            {
                UpdateOutputText();
            }
        }
    }
}
