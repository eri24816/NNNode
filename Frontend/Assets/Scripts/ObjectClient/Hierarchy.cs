using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectSync.ObjectClients
{
    [AddComponentMenu("ObjectClient/Hierarchy")]
    public class Hierarchy : ObjectClient
    {
        [SerializeField]
        NNNode.Hierarchy heirarchy;

        public override void OnAddChild(ObjectClient child)
        {
            heirarchy.AddItem(child.category, child.gameObject);
            heirarchy.SetName("Node List");
        }
    }
}