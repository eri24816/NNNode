using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class DataPort : Port
    {
        public TMPro.TMP_Text nameText;
        override protected void Start()
        {
            base.Start();
            flowType = typeof(DataFlow);
        }

        override protected void Update()
        {
            base.Update();
        }

    }
}
