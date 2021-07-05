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
        public void Init(Node node, API_new info)
        {
            
            name = info.name;
            name_text.name = name;
            Init(node, info.target_attr);
        }
        public virtual void Init(Node node, string target_attr)
        {

        }
    }
}
