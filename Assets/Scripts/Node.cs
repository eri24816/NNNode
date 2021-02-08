using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Port> ins, outs;
    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public RectTransform args;
    public string Name;
    public bool vis = false;
    
     public virtual void Start()
     {
        mesh = new Mesh();
        Reshape(0.7f, 1, 1.2f);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
     }
     

    void Update()
    {
        vis = false;
    }

    public float upPad = 0.1f,downPad=0.1f;
    public void Reshape(float w,float l,float r)
    {
        
        float h = l < r ? l : r;
        upPad = h / 2;

        args.position = transform.position + new Vector3(-w/2, h/2, 0);
        args.sizeDelta = new Vector2(w, h/2)*args.worldToLocalMatrix.m00;
        mesh.vertices= new Vector3[] {
            new Vector3(-w/2,-l/2),
            new Vector3(-w/2,l/2),
            new Vector3(w/2,-r/2),
            new Vector3(w/2,r/2),
        };
        mesh.triangles= new int[]{0,3,2,0,1,3};
        mesh.RecalculateBounds();
        if (ins.Count > 0)
            for(int i = 0; i < ins.Count; i++)
            {
                ins[i].transform.position = new Vector3(-w / 2, (h-upPad-downPad) * (i + 1) / (ins.Count + 1) - h / 2+downPad)+transform.position;
            }

        if (outs.Count > 0)
            for (int i = 0; i < outs.Count; i++)
            {
                outs[i].transform.position = new Vector3(w / 2, (h-upPad-downPad) * (i + 1) / (outs.Count + 1) - h / 2+downPad)+transform.position;
            }
    }
    Vector3 pPos;
    public void Move(Vector3 movement)
    {
        if (vis) return;
        vis = true;
        transform.Translate(movement);
        foreach (Port p in ins)
        {
            if(p.dataFlow) p.dataFlow.Move(movement);
        }
        foreach (Port p in outs)
        {
            if (p.dataFlow) p.dataFlow.Move(movement);
        }
    }
    public void OnMouseDrag()
    {
        if (Input.GetMouseButton(0) && !UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
        {
            if (CamControl.worldMouseDelta.sqrMagnitude > 0)
            {
                Move(CamControl.worldMouseDelta);
            }
        }
    }


}
