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
        public override void Init(APIMessage.NewNode.Info info)
        {
            base.Init(info);

            if(nameText)
                nameText.text = Name;

            int i = 0;
            foreach (string portInfoJson in info.portInfos)
            {
                PortInfo portInfo = JsonUtility.FromJson<PortInfo>(portInfoJson);
                DataPort newPort;
                
                if(portInfo.isInput)
                    newPort = Instantiate(inDataPortPrefab, inputPanel.transform).GetComponent<DataPort>();
                else
                    newPort = Instantiate(outDataPortPrefab, outputPanel.transform).GetComponent<DataPort>();
                ports.Add(newPort);
                newPort.nameText.text = portInfo.name;
                newPort.port_id = i;
                newPort.isInput = portInfo.isInput;
                newPort.maxEdges = portInfo.max_connections;
                newPort.node = this;
                i++;
            }
        }

    }
}