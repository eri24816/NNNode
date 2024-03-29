using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace GraphUI
{
    public class Dropdown : Comp
    {
        [SerializeField]
        TMPro.TMP_Text text;
        Node.Attribute targetAttr;
        [SerializeField]
        TMPro.TMP_Dropdown TMP_Dropdown;
        List<string> options;
        public void SetOptions(string optionsList)
        {
            TMP_Dropdown.AddOptions(options = optionsList.Split(',').ToList());
        }

        public override void Init(Node node, string targetAttrName,string type="", bool isMainComp = true)
        {
            
            base.Init(node, targetAttrName);

            SetOptions(type.Split(':')[1]);
            
            targetAttr = Node.Attribute.Register(node, targetAttrName, "string",
                (v) => { TMP_Dropdown.SetValueWithoutNotify(options.IndexOf((string)v)); },
                () => options[TMP_Dropdown.value]
                ) ;
        }
        public void OnValueChanged(int value)
        {
            targetAttr.Set(options[value]);
        }
    }
}