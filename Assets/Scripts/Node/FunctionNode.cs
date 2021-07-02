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
        public override void Init(string infoJSON)
        {
            base.Init(infoJSON);

            if(nameText)
                nameText.text = Name;

        }
         
    }
}