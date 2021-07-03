using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class FunctionNode : Node
    {
        [SerializeField]
        GameObject inputPanel, outputPanel;
        [SerializeField]
        TMPro.TMP_Text nameText;
        [SerializeField]
        GameObject inDataPortPrefab, outDataPortPrefab;
        public override void Init(string infoJSON, string id = null)
        {
            base.Init(infoJSON,id);

            if(nameText)
                nameText.text = Name;

        }
         
    }
}