using UnityEngine;
using UnityEngine.EventSystems;

public class Test : MonoBehaviour, IBeginDragHandler,IDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        print("bd");
    }

    public void OnDrag(PointerEventData eventData)
    {
        print("d");
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
