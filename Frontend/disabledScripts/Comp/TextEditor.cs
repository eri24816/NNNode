using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI {
    public class TextEditor : Comp
    {
        public TMPro.CodeEditor inputFieldUI;
        Node.Attribute targetAttr;
        public string dataType = "float";
        [SerializeField]
        UnityEngine.UI.Image lineNumberPanel;
        [SerializeField]
        TMPro.TMP_Text lineNumber;
        public override void Init(Node node, string targetAttrName,string type="", bool isMainComp = true)
        {
            base.Init(node, targetAttrName);

            if (dataType == "float")
                targetAttr = Node.Attribute.Register(node, targetAttrName, "float",
                    (v) => inputFieldUI.text = ((float)v).ToString("G4").Replace("E+0", "e").Replace("E-0", "e-").Replace("E+", "e").Replace("E-", "e-"),
                    isMainComp ? () => { try { return float.Parse(inputFieldUI.text); } catch { return null; } } : (Node.Attribute.GetDel)null, comp: this
                    ) ;

            else if(dataType == "string")
                targetAttr = Node.Attribute.Register(node, targetAttrName, "string",
                    (v) => inputFieldUI.text = (string)v,
                    isMainComp ? () => inputFieldUI.text : (Node.Attribute.GetDel)null, comp: this
                    );
        }

        public override void SetColor(Color color)
        {
            base.SetColor(color); 
            lineNumber.color = color;
            color.a = 0.3f;
            lineNumberPanel.color = color;
        }

        public void OnEndEdit(string value)
        {
            if (targetAttr == null) return;
                if (dataType == "float")
                    targetAttr.Set(float.Parse(value));
                else if (dataType == "string")
                targetAttr.Set(value);
        }
        
    }
}