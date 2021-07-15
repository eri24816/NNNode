using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphUI
{
    public class Comp : MonoBehaviour
    {

        [System.Serializable]
        public struct API_new { public string name, type, target_attr; }
        [SerializeField]
        TMPro.TMP_Text name_text;
        public bool destroyed = false;
        public void Init(Node node, API_new info)
        {
            destroyed = false;
            name = info.name;
            if(name_text)
                name_text.name = name;
            Init(node, info.target_attr);
        }
        public virtual void Init(Node node, string target_attr, bool isMainComp = true)
        {
            // An attribute may connected to both a component on the node and a component in the inspector.
            // If it's the latter, isMainComp = false so the attribute's setDel won't point to the component in the inspector.
        }
        void OnDestroy()
        {
            destroyed = true;
        }
    }
}
