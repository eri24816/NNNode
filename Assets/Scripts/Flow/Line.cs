using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphUI
{
    public class Line : Graphic,ICanvasRaycastFilter
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
                    transform.position = (tail + head) / 2;
                    rectTransform.sizeDelta = new Vector2(Mathf.Abs(head.x - tail.x) + 2 * width, Mathf.Abs(head.y - tail.y) + 2 * width);
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
                    transform.position = (tail + head) / 2;
                    rectTransform.sizeDelta = new Vector2(Mathf.Abs(head.x - tail.x)+2*width, Mathf.Abs(head.y - tail.y) + 2 * width);
                }
            }
        }
        public Vector3 Head_dir
        {
            set
            {
                if (value != head_vel)
                {
                    head_vel = value;
                    head_vel.Normalize();
                    SetVerticesDirty();
                }
            }
        }
        public Vector3 Tail_dir
        {
            set
            {
                if (value != tail_vel)
                {
                    tail_vel = value;
                    tail_vel.Normalize();
                    SetVerticesDirty();
                }
            }
        }

        Vector3 tail, head;
        Vector3 tail_vel = Vector3.right, head_vel = Vector3.left;

        public float shapeVel = 0.5f;
        public float curveDist = 0.8f,width=0.2f;
        Vector3[] points,delta;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            
            UIVertex vertex = UIVertex.simpleVert;
            
            vh.Clear();
            vertex.color = color;
            


            float dist = Vector3.Distance(tail, head);
            float vel = shapeVel * Mathf.Min(1, dist);

            points = new Vector3[resolution];
            for (int i = 0; i < resolution / 2; i++)
            {
                float t = ((float)i) / resolution / Mathf.Max(dist, 1) * curveDist;
                float u = 1 - t;
                points[i] = tail * u * u * u + (tail + tail_vel * vel) * 3 * u * u * t + (head + head_vel * vel) * 3 * u * t * t + head * t * t * t;
            }
            for (int i = resolution / 2; i < resolution; i++)
            {
                float u = ((float)(resolution - i - 1)) / resolution / Mathf.Max(dist, 1) * curveDist;
                float t = 1 - u;
                points[i] = tail * u * u * u + (tail + tail_vel * vel) * 3 * u * u * t + (head + head_vel * vel) * 3 * u * t * t + head * t * t * t;
            }

            delta = new Vector3[resolution - 1];
            for (int i = 0; i < resolution - 1; i++)
            {
                delta[i] = Vector3.Normalize(points[i + 1] - points[i]);
            }

            Vector3 shift = width * Vector3.up, shift_;
            for (int i = 0; i < resolution; i++)
            {
                if (i == 0 || i == resolution -1)
                //if(true)
                {
                    shift_ = width * Vector3.up;
                }
                else
                {
                    float c = Vector3.Cross(delta[i - 1], -delta[i]).magnitude;
                    if (Mathf.Abs(c)<0.15)shift_ = width * Vector3.Normalize(delta[i - 1] - delta[i]); 
                    else
                    {
                        shift_ = width * (delta[i - 1] - delta[i]) / c;
                        
                    }
                }
                shift = shift_ * Mathf.Sign(Vector3.Dot(shift, shift_));
                vertex.position = points[i] + shift-transform.position;
                vh.AddVert(vertex);
                vertex.position = points[i] - shift-transform.position;
                vh.AddVert(vertex);
            }
            for (int i = 0; i < resolution-1; i++)
            {
                vh.AddTriangle(i * 2, i * 2 + 1, i * 2 + 2);
                vh.AddTriangle(i * 2 + 1, i * 2 + 2, i * 2 + 3);
            }
        }
        Vector3 b,w,o;
        float dist;
        public float raycastThreshold = 1.2f,longerEndPoint=2f;
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            Vector3 longerEndPoint_ = longerEndPoint * b * width;
            for (int i = 0; i < resolution - 1; i++)
            {
                var ray = eventCamera.ScreenPointToRay(sp);

                b = delta[i];
                w =  Vector3.Cross(ray.direction, b);
                w.Normalize();
                dist = Vector3.Project(points[i]-ray.origin , w).magnitude;
                if (dist > width * raycastThreshold) continue;

                o = ray.origin + dist * w;
                float cos = CosineBetween(o - points[i]- longerEndPoint_, o - points[i + 1]+ longerEndPoint_);
                if (CosineBetween(o - points[i]- longerEndPoint_, ray.direction) >= cos && CosineBetween(o - points[i + 1]+ longerEndPoint_, ray.direction) >= cos)
                return true;
            }
            return false;
        }
        float CosineBetween(Vector3 a,Vector3 b)
        {
            return Mathf.Abs( Vector3.Dot(a, b) / (a.magnitude * b.magnitude));
        }
        public IEnumerator ChangeColor(Color target, float speed = 15)
        {
            Color original = color;
            float t = 1f;
            while (t > 0.02f)
            {
                t *= Mathf.Pow(0.5f, Time.deltaTime * speed);
                color = Color.Lerp(target, original, t);
                yield return null;
            }
            color = target;
        }
    }
    
}
