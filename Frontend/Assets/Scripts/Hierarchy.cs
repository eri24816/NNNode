using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NNNode
{
    public class Hierarchy : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        GameObject prefab, displayFramePrefab;
        [SerializeField]
        RectTransform leafPanel, subcategoryPanel;
        [SerializeField]
        TMPro.TMP_Text nameText;

        bool expand = true;
        LayoutElement leafLayoutElement, subcategoryLayoutElemebt;

        void Awake()
        {
            leafLayoutElement = leafPanel.GetComponent<LayoutElement>();
            subcategoryLayoutElemebt = subcategoryPanel.GetComponent<LayoutElement>();
        }

        public void AddItem(string categoryString, GameObject item)
        {
            if (categoryString != "" && categoryString[^1] != '/')
            {
                categoryString += '/';
            }

            int idx = categoryString.IndexOf("/");
            if (idx == -1)
            {
                if (item.transform is RectTransform)
                {

                    item.transform.SetParent(leafPanel);
                }
                else
                {
                    // If the item is not an UI object (e.g. it's a 3d model), warp it with a display frame to place it correctly.
                    var displayFrame =  Instantiate(displayFramePrefab, leafPanel);
                    item.transform.SetParent(displayFrame.transform);
                    item.transform.localPosition = Vector3.zero;
                    //item.transform.localScale = Vector3.one*50;
                }
            }
            else
            {
                string restCategoryString = categoryString[(idx + 1)..];
                Hierarchy subcategory = GetSubcategory(categoryString[..idx]).GetComponent<Hierarchy>();

                subcategory.AddItem(restCategoryString, item);
            }

            ExpandOrCollapse();
        }

        public Hierarchy GetSubcategory(string category)
        {
            ExpandOrCollapse();
            var res = subcategoryPanel.transform.Find(category);
            if (res != null) return res.GetComponent<Hierarchy>();

            Hierarchy subcategory = Instantiate(prefab, subcategoryPanel.transform).GetComponent<Hierarchy>();
            subcategory.SetName(category);
            subcategory.prefab = prefab;
            return subcategory;
        }

        public GameObject GetLeaf(string name)
        {
            return leafPanel.transform.Find(name).gameObject;
        }

        public void Clear()
        {
            foreach (Transform child in leafPanel)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in subcategoryPanel)
            {
                Destroy(child.gameObject);
            }
        }

        public void SetName(string name)
        {
            gameObject.name = name;
            nameText.text = name;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            expand ^= true;
            ExpandOrCollapse();
        }
        void ExpandOrCollapse()
        {
            leafLayoutElement.ignoreLayout = (leafLayoutElement.transform.childCount == 0 || !expand) ;
            subcategoryLayoutElemebt.ignoreLayout = (subcategoryLayoutElemebt.transform.childCount == 0 || !expand);

            UpdateLayout();
        }
        public void UpdateLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            var p = transform.parent.GetComponentInParent<Hierarchy>();
            if (p)
            {
                p.UpdateLayout();
            }
        }
    }
}   