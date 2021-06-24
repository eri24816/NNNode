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
            float step = Mathf.PI / (1 + info.in_names.Length);
            int i = 0;
            foreach (string in_name in info.in_names)
            {
                var newPort = Instantiate(inDataPortPrefab, transform).GetComponent<DataPort>();
                var angle = Mathf.PI / 2 + (1+i) * step;
                
                newPort.transform.localPosition = radius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                newPort.nameText.text = "";
                newPort.isInput = true;
                newPort.maxEdges = info.allow_multiple_in_data[i] ? 64 : 1;
                newPort.node = this;
                i++;
            }

            step = Mathf.PI / (1 + info.out_names.Length);
            i = 0;
            foreach (string out_name in info.out_names)
            {
                var newPort = Instantiate(outDataPortPrefab,transform).GetComponent<DataPort>();
                var angle = Mathf.PI / 2 - (1+i) * step;
                newPort.transform.localPosition = radius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                newPort.nameText.text = "";
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