using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GraphUI
{
    public class FunctionNode : Node
    {

        protected Mesh mesh;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public override void Start()
        {
            base.Start();

            mesh = new Mesh();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            Reshape(0.7f, .4f, 0.4f);
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
        }
        public override void Reshape(float w, float l, float r)
        {
            float h = l < r ? l : r;
            upPad = h / 2;

            panel.position = transform.position + new Vector3(-w / 2, h / 2, 0);
            panel.sizeDelta = new Vector2(w, h / 2) * panel.worldToLocalMatrix.m00;
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
                    ins[i].transform.position = new Vector3(-w / 2, (h - upPad - downPad) * (i + 1) / (ins.Count + 1) - h / 2 + downPad) + transform.position;
                }

            if (outs.Count > 0)
                for (int i = 0; i < outs.Count; i++)
                {
                    outs[i].transform.position = new Vector3(w / 2, (h - upPad - downPad) * (i + 1) / (outs.Count + 1) - h / 2 + downPad) + transform.position;
                }
        }
    }
}