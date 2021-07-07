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
        public override void Init(string infoJSON,string id = null)
        {
            base.Init(infoJSON,id);

            if (nameText)
                nameText.text = Name;

            

        }

        public override void SetupPort(Port port, Port.API_new portInfo)
        {
            float radius = ((RectTransform)transform).sizeDelta.x / 2;
            port.transform.localPosition = radius * portInfo.pos;
            port.minDir = port.maxDir = Mathf.Atan2(portInfo.pos.y, portInfo.pos.x);
        }


        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
    }
}