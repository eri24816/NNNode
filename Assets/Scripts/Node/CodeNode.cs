using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeNode : Node
{
    public bool expanded = false;

    public override void Start()
    {
        base.Start();
        Reshape(1.3f, 1f, 1f);
    }


    public override void Update()
    {
        base.Update();
    }

    public override void Reshape(float w, float l, float r)//Trapezoid shaped node
    {

        float h = l < r ? l : r;
        upPad = h / 2;

        panel.position = transform.position + new Vector3(-w / 2, h / 2, 0);
        panel.sizeDelta = new Vector2(w, h ) * panel.worldToLocalMatrix.m00;
        mesh.vertices = new Vector3[] {
            new Vector3(-w/2,-l/2),
            new Vector3(-w/2,l/2),
            new Vector3(w/2,-r/2),
            new Vector3(w/2,r/2),
        };
        mesh.triangles = new int[] { 0, 3, 2, 0, 1, 3 };
        mesh.RecalculateBounds();
        if (ins.Count > 0)
            for (int i = 0; i < ins.Count; i++)
            {
                ins[i].transform.position = new Vector3(-w / 2, h/2- .1f*i -.1f) + transform.position;
            }

        if (outs.Count > 0)
            for (int i = 0; i < outs.Count; i++)
            {
                outs[i].transform.position = new Vector3(w / 2, h / 2 - .1f * i - .1f) + transform.position;
            }
    }
}
