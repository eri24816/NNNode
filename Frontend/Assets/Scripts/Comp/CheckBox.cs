using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class CheckBox : Comp
    {
        [SerializeField]
        UnityEngine.UI.Toggle toggle;
        Node.NodeAttr targetAttr;

        public override void Init(Node node, string targetAttrName, string type = "", bool isMainComp = true)
        {
            base.Init(node, targetAttrName);
            targetAttr = Node.NodeAttr.Register(node, targetAttrName, "bool",
                (v) => toggle.SetIsOnWithoutNotify( (bool)v),
                () => toggle.isOn
                );
        }
        public void OnValueChanged()
        {
            targetAttr.Set(toggle.isOn);
        }
    }
}