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
            foreach (string in_name in info.in_names)
            {
                var newPort = Instantiate(inDataPortPrefab, inputPanel.transform).GetComponent<DataPort>();
                newPort.nameText.text = in_name;
                newPort.isInput = true;
                newPort.maxEdges = info.allow_multiple_in_data[i] ? 64 : 1;
                newPort.node = this;
                i++;
            }

            i = 0;
            foreach (string out_name in info.out_names)
            {
                var newPort = Instantiate(outDataPortPrefab, outputPanel.transform).GetComponent<DataPort>();
                newPort.nameText.text = out_name;
                newPort.isInput = false;
                newPort.maxEdges = 64;
                newPort.node = this;
                i++;
            }
        }
        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
    }
}