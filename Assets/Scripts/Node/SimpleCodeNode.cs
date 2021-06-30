using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class SimpleCodeNode : CodeNode
    {


        public override void Init(APIMessage.NewNode.Info info)
        {
            base.Init(info);
            Code = info.code;
            int i = 0;
            var rt = (RectTransform)transform;
            foreach (string portInfoJson in info.portInfos)
            {
                PortInfo portInfo = JsonUtility.FromJson<PortInfo>(portInfoJson);
                Port newPort;
                GameObject prefab;

                if (portInfo.type == "ControlPort")
                    prefab = portInfo.isInput ? Manager.ins.inControlPortPrefab : Manager.ins.outControlPortPrefab;
                else
                    prefab = portInfo.isInput ? Manager.ins.inDataPortPrefab : Manager.ins.outDataPortPrefab;

                newPort = Instantiate(prefab, transform).GetComponent<Port>();

                ports.Add(newPort);
                newPort.port_id = i;
                newPort.isInput = portInfo.isInput;
                newPort.maxEdges = portInfo.max_connections;
                newPort.node = this;
                newPort.name = portInfo.name;
                ((RectTransform)newPort.transform).anchorMin = ((RectTransform)newPort.transform).anchorMax = new Vector2(portInfo.pos.x / 2 + .5f, portInfo.pos.y / 2 + .5f);
                newPort.nameText.text = "";
                i++;
                 
            }
        }


    }
}