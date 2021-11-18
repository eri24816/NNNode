using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphUI;

public class Inspector : Hierachy
{
    Hierachy hierachy;
    [SerializeField]
    GameObject floatInput,stringInput,dropdown;

    [SerializeField]
    GameObject itemPanel;

    private void Start()
    {
        hierachy = GetComponent<Hierachy>();
    }

    public void Open(Node node)
    {
        foreach(var attr in node.attributes.Values)
        {

            if (attr.type == "float"|| attr.type == "string")
            {
                var newComp = CreateAttrEditor(attr.type == "string"?stringInput: floatInput,attr).GetComponent<GraphUI.TextEditor>();
                newComp.dataType = attr.type;
                //newComp.name = attr.name;
                newComp.Init(node, attr.name,attr.type,isMainComp:false);
            }

            else if (attr.type.Length>=8 && attr.type.Substring(0,8) == "dropdown")
            {
                var newComp = CreateAttrEditor(dropdown, attr).GetComponent<Dropdown>();
                //newComp.name = attr.name;
                newComp.Init(node, attr.name,type:attr.type,isMainComp: false);
                
            }
        }
    }

    GameObject CreateAttrEditor(GameObject prefab,Node.NodeAttr attr)
    {

        int i = attr.name.LastIndexOf('/');
        GameObject itemPanel_ = Instantiate(itemPanel, FindCategoryPanel(i==-1?"-":attr.name.Substring(0,i), "CategoryPanel"));
        itemPanel_.GetComponentInChildren<TMPro.TMP_Text>().text = attr.name.Substring(i+1);
        //itemPanel_.transform.SetSiblingIndex();
        return Instantiate(prefab, itemPanel_.transform);
    }


}
