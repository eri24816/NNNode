using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolDown
{
    float span;
    float waitTime = 0;
    bool pending = false;
    public CoolDown(float hz)
    {
        span = 1f / hz;
    }
    public void Request()
    {
        pending = true;
    }
    public void Delay(float t=-1)
    {
        waitTime = Time.time + (t == -1 ? span : t);
    }
    public bool Update()
    {
        if (pending && Time.time > waitTime)
        {
            pending = false;
            waitTime = Time.time + span;
            return true;
        }
        return false;
    }
}

public class Node : MonoBehaviour
{
    public List<Port> ins, outs;
    protected Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public RectTransform panel;
    public string Name;
    

    public virtual void Start()
    {
        Manager.i.Nodes.Add(Name,this);
        targetPos = transform.position;
        mesh = new Mesh();
        
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void Remove()
    {
        Destroy(gameObject);
        Manager.i.Nodes.Remove(Name);
    }

    public virtual void Update()
    {
        if (recvMoveCD.Update()) {
            transform.position = targetPos;
        }
        if (sendMoveCD.Update())
        {
            Manager.i.MoveNode(this, transform.position);//Ask the server to move the node.
        }
    }

    public float upPad = 0.1f, downPad = 0.1f;
    public virtual void Reshape(float w, float l, float r)//Trapezoid shaped node
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

    readonly CoolDown recvMoveCD = new CoolDown(hz: 10);
    readonly CoolDown sendMoveCD = new CoolDown(hz: 10);
    Vector3 targetPos;
    public void Move(Vector3 movement)
    {
        transform.Translate(movement);// Although the server will send a move message back soon, the node in frontend moves in advance to avoid a bad UX.
        sendMoveCD.Request();
        recvMoveCD.Delay(1);
    }
    public void RawMove(Vector3 p) // can't set transform.position outside update
    {
        recvMoveCD.Request();
        targetPos = p;
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

    /*
    public IEnumerator Creating()
    {
        yield return null;

        while (Input.GetMouseButton(0))
        {
            
            yield return null;
        }
    }*/

}
