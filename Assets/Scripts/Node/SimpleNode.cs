using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class SimpleNode : Node
    {
        protected override void CreatePort(Port.API_new portInfo)
        {
            GameObject prefab;
            if (portInfo.type == "ControlPort")
                prefab = portInfo.isInput ? Manager.ins.inControlPortPrefab : Manager.ins.outControlPortPrefab;
            else
                prefab = portInfo.isInput ? Manager.ins.inDataPortPrefab : Manager.ins.outDataPortPrefab;

            Port newPort = Instantiate(prefab,transform).GetComponent<Port>();
            ports.Add(newPort);
            newPort.Init(this, portInfo);
            

            
        }
        public override void SetupPort(Port port, Port.API_new portInfo)
        {
            port.minDir = port.maxDir = portInfo.isInput ? Mathf.PI : 0;
            if (portInfo.isInput) port.transform.SetAsFirstSibling();
            else port.transform.SetAsLastSibling();
        }
    }
}