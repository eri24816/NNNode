using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeDrag : MonoBehaviour
{
    public event System.Action<Vector3,Transform> Drop;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //transform.position = 
        }
        else
        {
            Drop(transform.localPosition,transform.parent);
        }
    }
}
