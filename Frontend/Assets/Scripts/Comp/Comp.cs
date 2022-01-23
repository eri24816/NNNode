using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace GraphUI
{
    public class Comp : MonoBehaviour
    {

        [System.Serializable]
        public struct API_new { public string name, type, target_attr; }
        [SerializeField]
        TMPro.TMP_Text name_text;
        public void InitWithInfo(Node node, JToken info)
        {
            if (name_text) name_text.text = (string)info["name"];
            Init(node, (string)info["target_attr"],(string)info["type"]);
        }
        public virtual void Init(Node node, string target_attr,string type="", bool isMainComp = true)
        {
            // An attribute may connected to both a component on the node and a component in the inspector.
            // If it's the latter, isMainComp = false so the attribute's setDel won't point to the component in the inspector.
        }
        public virtual void SetColor(Color color)
        {
            
        }
    }
}
