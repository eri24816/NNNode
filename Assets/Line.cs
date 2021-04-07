using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphUI
{
    public class Line : Graphic
    {
        
        public int resolution;
        int res_;
        public Vector3 Tail
        {
            set
            {
                if (value != tail)
                {
                    tail = value;
                    SetVerticesDirty();
                }
            }
        }
        public Vector3 Head
        {
            set
            {
                if (value != head)
                {
                    head = value;
                    SetVerticesDirty();
                }
            }
        }
        Vector3 tail, head;

        public float shapeVel = 0.5f;
        public float curveDist = 0.8f,width=0.2f;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            UIVertex vertex = UIVertex.simpleVert;
            if (resolution != res_)
            {
                res_ = resolution;
                vh.Clear();
                vh.AddVert(vertex);
                vh.AddVert(vertex);
                for (int i = 0; i < resolution; i++)
                {
                    vh.AddVert(vertex);
                    vh.AddVert(vertex);
                    vh.AddTriangle(i * 2, i * 2 + 1, i * 2 + 2);
                    vh.AddTriangle(i * 2 + 1, i * 2 + 2, i * 2 + 3);
                }
            }


            float dist = Vector3.Distance(tail, head);
            float vel = shapeVel * Mathf.Min(1, dist);

            Vector3[] points = new Vector3[resolution];
            for (int i = 0; i < resolution / 2; i++)
            {
                float t = ((float)i) / resolution / Mathf.Max(dist, 1) * curveDist;
                float u = 1 - t;
                points[i] = tail * u * u * u + (tail + Vector3.right * vel) * 3 * u * u * t + (head + Vector3.left * vel) * 3 * u * t * t + head * t * t * t;
            }
            for (int i = resolution / 2; i < resolution; i++)
            {
                float u = ((float)(resolution - i - 1)) / resolution / Mathf.Max(dist, 1) * curveDist;
                float t = 1 - u;
                points[i] = tail * u * u * u + (tail + Vector3.right * vel) * 3 * u * u * t + (head + Vector3.left * vel) * 3 * u * t * t + head * t * t * t;
            }

            Vector3[] delta = new Vector3[resolution - 1];
            for (int i = 0; i < resolution - 2; i++)
            {
                delta[i] = Vector3.Normalize(points[i + 1] - points[i]);
            }

            Vector3 shift = width * Vector3.up;
            for (int i = 0; i < resolution; i++)
            {
                if (i == 0 || i == resolution - 1)
                {
                    shift = width * Vector3.up;
                }
                else
                {
                    Vector3 c = Vector3.Cross(delta[i - 1], delta[i]);
                    if(c == Vector3.zero)shift = width * Vector3.up;
                    else
                    {
                        Vector3 shift_ = width * (delta[i - 1] + delta[i]) / c.magnitude;
                        shift = shift_ * Mathf.Sign(Vector3.Dot(shift, shift_));
                    }
                }
                vertex.position = points[i] + shift;
                vh.SetUIVertex(vertex, i * 2);
                vertex.position = points[i] - shift;
                vh.SetUIVertex(vertex, i * 2+1);
            }

        }
    }
}
