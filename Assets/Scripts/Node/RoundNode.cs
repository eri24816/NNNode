using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class RoundNode : Node
    {


        [SerializeField]
        TMPro.TMP_Text nameText;
        [SerializeField]
        GameObject inDataPortPrefab, outDataPortPrefab;
        public override void Init(APIMessage.NewNode.Info info)
        {
            base.Init(info);

            if(nameText)
                nameText.text = Name;

            float radius = ((RectTransform)transform).sizeDelta.x / 2;
            int i = 0;
            foreach (string portInfoJson in info.portInfos)
            {
                PortInfo portInfo = JsonUtility.FromJson<PortInfo>(portInfoJson);
                if (portInfo.type == "ControlPort")
                {
                    ControlPort newPort;
                    if (portInfo.isInput)
                        newPort = Instantiate(inDataPortPrefab, transform).GetComponent<ControlPort>();
                    else
                        newPort = Instantiate(outDataPortPrefab, transform).GetComponent<ControlPort>();
                    ports.Add(newPort);
                    newPort.desiredLocalPos = radius * portInfo.pos;
                    newPort.port_id = i; 
                    newPort.isInput = portInfo.isInput;
                    newPort.maxEdges = portInfo.max_connections;
                    newPort.node = this;
                    i++;
                }
                else if (portInfo.type == "DataPort")
                {

                    DataPort newPort;
                    if (portInfo.isInput)
                        newPort = Instantiate(inDataPortPrefab, transform).GetComponent<DataPort>();
                    else
                        newPort = Instantiate(outDataPortPrefab, transform).GetComponent<DataPort>();
                    ports.Add(newPort);
                    newPort.desiredLocalPos = radius * portInfo.pos;
                    newPort.nameText.text = "";
                    newPort.port_id = i;
                    newPort.isInput = portInfo.isInput;
                    newPort.maxEdges = portInfo.max_connections;
                    newPort.node = this;
                    i++;
                }
            }

        }
        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
    }
}