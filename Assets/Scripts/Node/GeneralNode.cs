using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class GeneralNode : Node
    {
        [SerializeField]
        GameObject inputPanel, outputPanel;
        [SerializeField]
        TMPro.TMP_Text nameText;
        [SerializeField]
        GameObject inDataPortPrefab, outDataPortPrefab;
        public override void Init(Newtonsoft.Json.Linq.JToken infoJSON)
        {
            base.Init(infoJSON);

            if (nameText)
                nameText.text = Name;

        }

        public override void SetupPort(Port port, Newtonsoft.Json.Linq.JToken portInfo)
        { 
            port.transform.SetParent((bool)portInfo["isInput"] ? inputPanel.transform : outputPanel.transform);
            port.minDir = port.maxDir = (bool)portInfo["isInput"] ? Mathf.PI : 0;
            port.nameText.text = (string)portInfo["name"];
        }
        public override void SetMainColor(Color color)
        {
            base.SetMainColor(color);
            nameText.color = color;
        }
    }
}