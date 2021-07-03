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

            float radius = ((RectTransform)transform).sizeDelta.x / 2;

        }
        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }
    }
}