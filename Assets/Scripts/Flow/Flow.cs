using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flow : MonoBehaviour
{
    public Port tail, head;
    public LineRenderer line;
    public bool hard = false;
    protected virtual void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 10;
        StartCoroutine(Creating());
    }
    protected virtual void Update()
    {
        if (tail && head)
            Reshape(tail.transform.position, head.transform.position);
    }
    public void Move(Vector3 movement)
    {
        transform.Translate(movement);
        if (hard)
        {
            tail.node.Move(movement);
            head.node.Move(movement);
        }
    }

    public IEnumerator Creating()
    {
        Manager.i.state = Manager.State.draggingFlow;
        yield return null; // wait for next frame
        if (tail && head)
        {
            Manager.i.state = Manager.State.idle;
            yield break;
        }
        bool dragTail = head;

        Port targetPort = null;
        while (Input.GetMouseButton(0)) // while mouse hold
        {
            targetPort = null;
            if(CamControl.colliderHover)
                targetPort = CamControl.colliderHover.GetComponent<Port>();
            if (targetPort)
                if (!targetPort.AcceptEdge(this)) targetPort = null;
            Vector3 dragPos;
            dragPos = targetPort ? targetPort.transform.position : CamControl.worldMouse;

            // reshape
            if (dragTail)
                Reshape(dragPos, head.transform.position);
            else
                Reshape(tail.transform.position, dragPos);
            yield return null;
        }


        // after mouse release
        if (targetPort)
        {
            if (dragTail) tail = targetPort; else head = targetPort;
            targetPort.Edges.Add(this);
            Manager.i.CreateFlow(this);
            Manager.i.state = Manager.State.idle;
            yield break;
        }
        else
        {
            Remove();
        }
        Manager.i.state = Manager.State.idle;
    }

    public void Remove()
    {
        if (tail) tail.Disconnect(this);
        if (head) head.Disconnect(this);
        Destroy(gameObject);
    }

    public float shapeVel = 0.5f;
    public float curveDist = 0.8f;
    public void Reshape(Vector3 tail, Vector3 head)
    {
        float dist = Vector3.Distance(tail, head);
        float vel = shapeVel * Mathf.Min(1, dist);

        
        for (int i = 0; i < line.positionCount/2; i++)
        {
            float t = ((float)i)/ line.positionCount / Mathf.Max( dist ,1) * curveDist;
            float u = 1 - t;
            line.SetPosition(i, tail*u*u*u+ (tail + Vector3.right * vel) * 3*u*u*t + (head + Vector3.left * vel) * 3 * u * t * t+head*t*t*t);
        }
        for (int i = line.positionCount / 2;i<line.positionCount; i++)
        {
            float u = ((float)(line.positionCount-i-1)) / line.positionCount / Mathf.Max(dist, 1) * curveDist;
            float t = 1 - u;
            line.SetPosition(i, tail * u * u * u + (tail + Vector3.right * vel) * 3 * u * u * t + (head + Vector3.left * vel) * 3 * u * t * t + head * t * t * t);
        }
        Gradient gradient = new Gradient
        {
            colorKeys = new GradientColorKey[] { new GradientColorKey(new Color(0.6f,0.7f,1f), 0), new GradientColorKey(new Color(0.6f,0.7f,0.8f), 1) },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.2f, Mathf.Min(0.5f, 0.15f / dist)), new GradientAlphaKey(.2f, 1 - Mathf.Min(0.5f, 0.15f / dist)), new GradientAlphaKey(1, 1) }
        };
        line.colorGradient = gradient;
    }
}
