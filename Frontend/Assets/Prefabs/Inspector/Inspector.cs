using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObjectSync;

namespace NNNode.Inspector
{
    public class Inspector : MonoBehaviour
    {
        [SerializeField]
        Hierarchy hierarchy;
        [SerializeField]
        InspectorField<string> stringFieldPrefab;
        readonly List<System.IDisposable> fields = new();

        public void Open(ObjectSync.Object obj)
        {
            foreach(var pair in obj.Attributes)
            {
            }
        }

    }
}