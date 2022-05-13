using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayFrame : MonoBehaviour
{

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(-20f, Time.time*10, 0f);
    }
}
