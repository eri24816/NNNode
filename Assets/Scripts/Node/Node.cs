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
    public RectTransform panel;
    public string Name;
    

    public virtual void Start()
    {
        Manager.i.Nodes.Add(Name,this);
        targetPos = transform.position;
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
