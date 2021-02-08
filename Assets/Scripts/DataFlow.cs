using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataFlow : MonoBehaviour
{
    public Port back, front;
    public LineRenderer line;
    public bool vis = false;
    public bool hard = false;
    public void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }
    void Update()
    {
        vis = false;
        if(back&&front)
            Reshape(back.transform.position, front.transform.position);
    }
    public void Move(Vector3 movement)
    {
        if (vis) return;
        vis = true;
        transform.Translate(movement);
        if (hard)
        {
            back.node.Move(movement);
            front.node.Move(movement);
        }
    }

    public IEnumerator Creating()
    {
        yield return null;
        if (back && front) yield break;
        bool dragBack = front;
        Port targetPort = CamControl.colliderHover.GetComponent<Port>();
        if (targetPort)
        {
            if (!targetPort.AcceptEdge(dragBack)) targetPort = null;
        }
        while (Input.GetMouseButton(0))
        {
            targetPort = CamControl.colliderHover.GetComponent<Port>();
            if (targetPort)
            {
                if (!targetPort.AcceptEdge(dragBack)) targetPort = null;
            }
            Vector3 dragPos;
            dragPos = targetPort ? targetPort.transform.position : CamControl.worldMouse;
            if (dragBack)
            {
                Reshape(dragPos, front.transform.position);
            }
            else
            {
                Reshape(back.transform.position, dragPos);
            }
            yield return null;
        }

        if (targetPort)
        {
            if (dragBack) back = targetPort; else front = targetPort;
            targetPort.dataFlow = this;
            Reshape(back.transform.position, front.transform.position);
            yield break;
        }
        else
        {

        }
    }
    public void Reshape(Vector3 back, Vector3 front)
    {                                
        line.SetPositions(new Vector3[] { back, front });
    }
}
