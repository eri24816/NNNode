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
        [SerializeField]
        UnityEngine.UI.Image fill;
        Node.NodeAttr targetAttr;
        public override void Init(Node node,  string targetAttrName, bool isMainComp = true)
        {
            base.Init(node, targetAttrName);
            targetAttr = Node.NodeAttr.Register(node, targetAttrName,"float",
                (v) => sliderUI.value = (float)v,
                isMainComp?() => sliderUI.value:(Node.NodeAttr.GetDel)null
                ) ;
            Node.NodeAttr.Register(node, $"{name}/min", "float", (v) => sliderUI.minValue = (float)v, initValue: 0f,comp:this);
            Node.NodeAttr.Register(node, $"{name}/max", "float", (v) => sliderUI.maxValue = (float)v, initValue: 100f, comp: this);

            
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
        public override void SetColor(Color color)
        {
            base.SetColor(color);
            fill.color = color;
        }
    }
}