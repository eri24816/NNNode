using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NNNode
{
    public class Inspector : MonoBehaviour
    {
        [SerializeField]
        TMP_Text nameText;
        [SerializeField]
        Hierarchy attributeHierarchy;
        [SerializeField]
        GameObject itemContainer;
        [SerializeField]
        IntInput intInput;

        public void Add(ObjectSync.Object o)
        {
            nameText.text += o.type;
            foreach(var p in o.Attributes)
            {
                var fullName = p.Key;
                var attr = p.Value;
                string categoryString = fullName[..(fullName.IndexOf('/')+1)];
                string name = fullName[fullName.IndexOf('/')..];

                Hierarchy category = attributeHierarchy.GetSubcategory(categoryString);
                GameObject c = category.GetLeaf(name);

                if (attr is ObjectSync.Attribute<int> a)
                {
                    if (c == null) {
                        c = Instantiate(itemContainer);
                        c.name = name;
                        Instantiate(intInput, c.transform);
                        attributeHierarchy.AddItem(categoryString,c);
                    }
                    IntInput input = c.GetComponentInChildren<IntInput>();
                    input.SetAttribute(a);
                }
            }
        }
    }
}