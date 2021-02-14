using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPort : Port
{
    public string varName;
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
