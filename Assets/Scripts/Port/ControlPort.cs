using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class ControlPort : Port
    {

        override protected void Start()
        {
            base.Start();
            flowType = typeof(ControlFlow);
        }

        override protected void Update()
        {
            base.Update();
        }

    }
}