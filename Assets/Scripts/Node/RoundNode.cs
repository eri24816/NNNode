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
        public override void Init(Newtonsoft.Json.Linq.JToken infoJSON)
        {
            base.Init(infoJSON);

            if (nameText)
                nameText.text = Name;
        }

        public override void SetupPort(Port port, Newtonsoft.Json.Linq.JToken portInfo)
        {
            float radius = ((RectTransform)transform).sizeDelta.x / 2;
            port.transform.localPosition = radius * portInfo["pos"].ToObject<Vector3>();
            port.minDir = port.maxDir = Mathf.Atan2((float)portInfo["pos"]["y"], (float)portInfo["pos"]["x"]);
            port.DisplayKnob();
        }


        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
    }
}