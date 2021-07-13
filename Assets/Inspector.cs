using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphUI;

public class Inspector : MonoBehaviour
{
    [SerializeField]
    RectTransform attributePanel;
    [SerializeField]
    GameObject floatInput,stringInput;
    public void Open(Node node)
    {
        foreach(var attr in node.attributes.Values)
        {
            if (attr.type == "float")
            {
                var newComp = Instantiate(floatInput, attributePanel).GetComponent<GraphUI.TextEditor>();
                newComp.dataType = "float";
                newComp.Init(node, attr.name,false);
            }


            else if (attr.type == "string")
            {
                var newComp = Instantiate(stringInput, attributePanel).GetComponent<GraphUI.TextEditor>();
                newComp.dataType = "string";
                newComp.Init(node, attr.name,false);
            }
        }
    }
    public void Close()
    {
        foreach (Transform child in attributePanel)
        {
            Destroy(child.gameObject);
        }
    }
}
