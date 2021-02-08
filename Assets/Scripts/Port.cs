using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Port : MonoBehaviour
{
    public string varName;
    public Node node;
    public DataFlow dataFlow;
    public bool isInput;
    void Start()
    {
        node = transform.parent.parent.GetComponent<Node>();
    }

    
    void Update()
    {
        
    }

    public void OnMouseDrag()
    {
        if (Input.GetMouseButton(0))
        {
            if (!dataFlow)
            {
                dataFlow = Manager.i.CreateDataFlow();
                if (isInput)
                {
                    dataFlow.front = this;
                }
                else
                {
                    dataFlow.back = this;
                }
            }
        }
    }
    public bool AcceptEdge(bool isBack)
    {
        if (dataFlow) return false;
        if (isBack==isInput) return false;
        return true;
    }
}
    