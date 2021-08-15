using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphUI;

public class Inspector : MonoBehaviour
{
    [SerializeField]
    RectTransform attributePanel;
    [SerializeField]
    GameObject itemPanel,categoryPanel;
    [SerializeField]
    GameObject floatInput,stringInput,dropdown;
    public void Open(Node node)
    {
        foreach(var attr in node.attributes.Values)
        {
            if (attr.type == "float")
            {
                var newComp = CreateAttrEditor(floatInput,attr).GetComponent<GraphUI.TextEditor>();
                newComp.dataType = "float";
                newComp.Init(node, attr.name,false);
            }


            else if (attr.type == "string")
            {
                var newComp = CreateAttrEditor(stringInput, attr).GetComponent<GraphUI.TextEditor>();
                newComp.dataType = "string";
                newComp.Init(node, attr.name,false);
            }
            else if (attr.type.Length>=8 && attr.type.Substring(0,8) == "dropdown")
            {
                var newComp = CreateAttrEditor(dropdown, attr).GetComponent<Dropdown>();

                newComp.Init(node, attr.name, false);
            }
        }
    }

    GameObject CreateAttrEditor(GameObject prefab,Node.NodeAttr attr)
    {
        int i = attr.name.LastIndexOf('/');
        GameObject itemPanel_ = Instantiate(itemPanel, Manager.ins.FindCategoryPanel(i==-1?"-":attr.name.Substring(0,i), attributePanel,categoryPanel));
        itemPanel_.GetComponentInChildren<TMPro.TMP_Text>().text = attr.name.Substring(i+1);
        //itemPanel_.transform.SetSiblingIndex();
        return Instantiate(prefab, itemPanel_.transform);
    }

    public void Close()
    {
        foreach (Transform child in attributePanel)
        {
             Destroy(child.gameObject);
        }
    }
}
