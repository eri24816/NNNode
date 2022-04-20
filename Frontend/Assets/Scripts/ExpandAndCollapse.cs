using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpandAndCollapse : MonoBehaviour
{
    bool collapsed = false;
    public void CollapseButtonPressed()
    {
        collapsed ^= true;
        if (collapsed) ((RectTransform)transform).sizeDelta = new Vector2(0, ((RectTransform)transform).sizeDelta.y);
        else ((RectTransform)transform).sizeDelta = new Vector2(250, ((RectTransform)transform).sizeDelta.y);
    }
}
 