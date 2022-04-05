using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IDragAndDrop
{
    void AcceptsDropOn(ObjectClient target);
    void DropOn(ObjectClient target);
}

[RequireComponent(typeof(CanvasGroup))]
public class DragAndDrop : MonoBehaviour
{
    public static DragAndDrop objectBeingDragged = null;
    CanvasGroup canvasGroup;
    IDragAndDrop obj;
    void Start()
    {
        //obj = GetComponent<IDragAndDrop>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void BeginDrag()
    {
        if(objectBeingDragged == null)
            objectBeingDragged = this;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (!Input.GetMouseButton(0)) {
            EndDrag();
            return;
        }
        if(objectBeingDragged == this)
        {
            transform.position += CamControl.worldMouseDelta;
        }
    }

    public void EndDrag()
    {
        if (objectBeingDragged == this)
        {
            objectBeingDragged = null;
            canvasGroup.blocksRaycasts = true;
        }
    }


}
