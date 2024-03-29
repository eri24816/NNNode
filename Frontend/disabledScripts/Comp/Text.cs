using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class Text : Comp
    {
        [SerializeField]
        TMPro.TMP_Text text;
        Node.Attribute targetAttr;

        public override void Init(Node node, string targetAttrName,string type="", bool isMainComp = true)
        {
            base.Init(node, targetAttrName);
            targetAttr = Node.Attribute.Register(node, targetAttrName, "string",
                (v) => text.text = (string)v,
                () => text.text
                ) ;
        }
    }
}