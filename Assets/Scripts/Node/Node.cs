using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

public class Node : MonoBehaviour,IPointerClickHandler
{
    public List<Port> ins, outs;
    public RectTransform panel;
    public string id,Name;

    public virtual Port GetPort(bool isInput = true, string var_name = "")
    {
        return isInput ? ins[0] : outs[0];
    }
    public virtual void Start()
    {
        targetPos = transform.position;
    }

    void Remove()
    {
        // Remove node from this client
        // Invoked by X button
        for (int i = 0; i < ins.Count; i++)
            ins[i].Remove();

        for (int i = 0; i < outs.Count; i++)
            outs[i].Remove();

        Manager.i.RemoveNode(this);
        StartCoroutine(Removing()); // Play removing animation and destroy the game objecct
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
    public virtual void Reshape(float w, float l, float r){}//Trapezoid shaped node
    

    readonly CoolDown recvMoveCD = new CoolDown(hz: 10);
    readonly CoolDown sendMoveCD = new CoolDown(hz: 10);
    Vector3 targetPos;
    public void Move(Vector3 movement)
    {
        transform.Translate(movement);// Although the server will send a move message back soon, the node in frontend moves in advance to avoid a bad UX.
        sendMoveCD.Request();
        recvMoveCD.Delay(1);
    }
    public void RawMove(Vector3 p) 
    {
        recvMoveCD.Request();
        targetPos = p; // can't set transform.position outside update
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

    
    public virtual IEnumerator Creating()//Drag and drop?
    {
        yield return null;
        /*
        while (Input.GetMouseButton(0))
        {
            
            yield return null;
        }*/
        Manager.i.AddNode(this);
    }
    public virtual IEnumerator Removing()// SAO-like?
    {
        yield return null;
        Destroy(gameObject);
    }

    float lastClickTime;
    float doubleClickDelay = 0.2f;
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickDelay) OnDoubleClick();
        else lastClickTime = Time.time;
    }

    protected virtual void OnDoubleClick()
    {
        
    }

    
}
