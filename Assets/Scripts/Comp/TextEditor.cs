using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI {
    public class TextEditor : Comp
    {
        [SerializeField]
        TMPro.CodeEditor inputFieldUI;
        Node.NodeAttr<string> targetAttr;
        public override void Init(Node node, string targetAttrName)
        {
            base.Init(node, targetAttrName);
            targetAttr = new Node.NodeAttr<string>(node, targetAttrName,
                (v) => inputFieldUI.text = v,
                () => inputFieldUI.text
                );
        }

        public void OnEndEdit(string value)
        {
            if (targetAttr != null)
                targetAttr.Send();
        }
    }
}