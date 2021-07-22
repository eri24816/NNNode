using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class Text : Comp
    {
        [SerializeField]
        TMPro.TMP_Text text;
        Node.NodeAttr targetAttr;

        public override void Init(Node node, string targetAttrName, bool isMainComp = true)
        {
            base.Init(node, targetAttrName);
            targetAttr = Node.NodeAttr.Register(node, targetAttrName, "string",
                (v) => text.text = (string)v,
                () => throw new System.NotImplementedException()
                ) ;
        }
    }
}