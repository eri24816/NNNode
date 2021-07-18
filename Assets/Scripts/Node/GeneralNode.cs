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
        public override void Init(string infoJSON, string id = null)
        {
            base.Init(infoJSON,id);

            if(nameText)
                nameText.text = Name;

        }

        public override void SetupPort(Port port, Port.API_new portInfo)
        { 
            port.transform.SetParent(portInfo.isInput ? inputPanel.transform : outputPanel.transform);
            port.minDir = port.maxDir = portInfo.isInput ? Mathf.PI : 0;
            port.nameText.text = portInfo.name;
        }
    }
}