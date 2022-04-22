using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NNNode
{
    public class Hierarchy : MonoBehaviour
    {
        [SerializeField]
        GameObject prefab;
        [SerializeField]
        RectTransform LeafPanel, SubcategoryPanel;
        [SerializeField]
        TMPro.TMP_Text nameText;

        public void AddItem(string categoryString, GameObject item)
        {
            if (categoryString != "" && categoryString[^1] != '/')
            {
                categoryString += '/';
            }

            int idx = categoryString.IndexOf("/");
            if (idx == -1)
            {
                item.transform.SetParent(LeafPanel);
            }
            else
            {
                string restCategoryString = categoryString[(idx + 1)..];
                Hierarchy subcategory = GetSubcategory(categoryString[..idx]).GetComponent<Hierarchy>();

                subcategory.AddItem(restCategoryString, item);
            }
        }

        public Hierarchy GetSubcategory(string category)
        {
            var res = SubcategoryPanel.transform.Find(category);
            if (res != null) return res.GetComponent<Hierarchy>();

            Hierarchy subcategory = Instantiate(prefab, SubcategoryPanel.transform).GetComponent<Hierarchy>();
            subcategory.SetName(category);
            subcategory.prefab = prefab;
            return subcategory;
        }
        public void Clear()
        {
            foreach (Transform child in LeafPanel)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in SubcategoryPanel)
            {
                Destroy(child.gameObject);
            }
        }

        public void SetName(string name)
        {
            gameObject.name = name;
            nameText.text = name;
        }

    }
}   