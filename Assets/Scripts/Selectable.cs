using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI
{
    public class Selectable : MonoBehaviour, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        public static List<Selectable> current=new List<Selectable>();
        
        public bool selected = false;
        float lastClickTime;
        float doubleClickDelay = 0.2f;

        public static void ClearSelection()
        {
            foreach (Selectable s in current)
                s.Unselect();
            current.Clear();
        }
        public static void Delete()
        {
            foreach (Selectable s in current)
            {
                s.Remove();
            }
            current.Clear();
        }
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (dragged) return; // Check dragging
            
            if (eventData.button == 0)
            {
                if (Time.time - lastClickTime < doubleClickDelay) OnDoubleClick();
                else lastClickTime = Time.time;


                if (CamControl. ctrlDown)
                {
                    if (selected)
                    {
                        current.Remove(this);
                        Unselect();
                    }
                    else
                    {
                        current.Add(this);
                        Select();
                    }
                }
                else if (CamControl.shiftDown)
                {
                    if (!selected)
                    {
                        current.Add(this);
                        Select();
                    }
                }
                else
                {
                    foreach (Selectable s in current)
                        if (s != this)
                            s.Unselect();
                    current.Clear();
                    current.Add(this);
                    if (!selected)
                        Select();
                }
            }
        }
        protected virtual void OnDoubleClick()
        {

        }
        public virtual void Select()
        {
            selected = true;
        }
        public virtual void Unselect()
        {
            selected = false;
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            
        }
        protected bool dragged;
        public void OnPointerDown(PointerEventData eventData)
        {
            dragged = false;
        }
        protected virtual void Remove()
        {
            
        }
    }
}
