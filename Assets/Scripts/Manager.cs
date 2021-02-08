using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public static Manager i;
    public List<Node> allNodes;

    [System.Serializable]
    public struct Prefabs
    {
        public GameObject DataFlow;
        public GameObject Node;
    }
    public Prefabs prefabs;

    public DataFlow CreateDataFlow()
    {
        DataFlow newDataFlow = Instantiate(prefabs.DataFlow).GetComponent<DataFlow>();
        StartCoroutine(newDataFlow.Creating());
        return newDataFlow;
    }

    void Start()
    {
        i = this;
    }

    
}
