using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Port : MonoBehaviour, IDragHandler
{
    /*
        Child game object of a node.
        Handle edges connected to the node.
    */
    
    public Node node;
    public List <Flow> Edges; // some type of port accepts multiple edges
    public int maxEdges;
    public bool isInput; // true: input false: output
    public System.Type flowType;

    protected virtual void Start()
    {
        node = transform.parent.GetComponent<Node>();
    }

    protected virtual void Update()
    {
        
    }

    
    public virtual bool AcceptEdge(Flow flow)
    {
        return (flow.GetType() == flowType) && (Edges.Count < maxEdges) && (isInput ? flow.head == null : flow.tail == null);
    }


    public void Disconnect(Flow flow)
    {
        Edges.Remove(flow);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Manager.i.state == Manager.State.idle && Input.GetMouseButton(0))
        {
            if (Edges.Count < maxEdges)
            {

                Flow newEdge = Instantiate(Manager.i.prefabDict[flowType.Name]).GetComponent<Flow>();
                if (isInput) newEdge.head = this;
                else newEdge.tail = this;
            }
        }
    }
}
    