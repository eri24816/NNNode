using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class Slider : Comp
    {
        [SerializeField]
        UnityEngine.UI.Slider sliderUI;
        [SerializeField]
        TMPro.CodeEditor inputFieldUI;
        Node.NodeAttr targetAttr;
        public override void Init(Node node,  string targetAttrName, bool isMainComp = true)
        {
            base.Init(node, targetAttrName);
            targetAttr = new Node.NodeAttr(node, targetAttrName,"float",
                (v) => sliderUI.value = (float)v,
                isMainComp?() => sliderUI.value:(Node.NodeAttr.GetDel)null
                ) ;
            Node.NodeAttr.Register(node, "min", "float",  (v) => sliderUI.minValue = (float)v).Set(0f);
            Node.NodeAttr.Register(node, "max", "float", (v) => sliderUI.maxValue = (float)v).Set(100f);

            
        }
        public void OnValueChanged(float v)
        {
            inputFieldUI.text = v.ToString("G4").Replace("E+0","e").Replace("E-0", "e-").Replace("E+", "e").Replace("E-", "e-");
            if(targetAttr != null)
                targetAttr.Set();
        }
        public void OnEndEdit(string value)
        {
            sliderUI.value = float.Parse(value);
        }
    }
}