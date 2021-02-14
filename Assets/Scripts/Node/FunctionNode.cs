using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionNode : Node
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        Reshape(0.7f, .4f, 0.4f);
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }
}
