using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Background : MonoBehaviour,IPointerClickHandler,IScrollHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == 0)
            NNNode.Selectable.ClearSelection();
    }
    public void OnScroll(PointerEventData eventData)
    {
        CamControl.ins.OnScroll(eventData);
    }
}
