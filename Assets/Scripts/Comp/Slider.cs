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
        Node.NodeAttr<float> targetAttr;
        public override void Init(Node node,  string targetAttrName)
        {
            base.Init(node, targetAttrName);
            targetAttr = new Node.NodeAttr<float>(node, targetAttrName,
                (v) => sliderUI.value = v,
                () => sliderUI.value
                ) ;
        }
        public void OnValueChanged(float v)
        {
            inputFieldUI.text = v.ToString("G4").Replace("E+0","e").Replace("E-0", "e-").Replace("E+", "e").Replace("E-", "e-");
            if(targetAttr != null)
                targetAttr.Send();
        }
        public void OnEndEdit(string value)
        {
            sliderUI.value = float.Parse(value);
        }
    }
}