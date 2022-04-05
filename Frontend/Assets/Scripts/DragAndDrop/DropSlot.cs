using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IDropSlot
{
    void AcceptsDrop(ObjectClient dropped);
    void FakeDrop(ObjectClient dropped);
    void Drop(ObjectClient dropped);
}

public class DropSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    IDropSlot slot;
    // Start is called before the first frame update
    void Start()
    {
        slot = GetComponent<IDropSlot>();
    } 

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }
}
