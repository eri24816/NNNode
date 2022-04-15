using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NNNode
{
    public interface ISelectable
    {

    }

    public class Selectable : ObjectClient, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        public static List<Selectable> current = new();

        public bool selected = false;
        float lastClickTime;
        float doubleClickDelay = 0.2f;

        float mouseExitToEnterTimeout=0.1f, mouseLastExitTime;
        public static Node TheOnlySelectedNode()
        {
            if (current.Count == 1 && current[0] is Node n) return n;
            return null;
        }
        public static void ClearSelection()
        {
            foreach (Selectable s in current.ToArray())
                s.Unselect();
            current.Clear();
        }
        public static void Delete()
        {
            Selectable[] temp = new Selectable[current.Count];
            current.CopyTo(temp);
            foreach (Selectable s in temp)
            {
                s.syncObject.space.SendDestroy(s.syncObject.id);
            }
        }


        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (unselectOtherWhenClick)
            {
                foreach (Selectable s in current.ToArray())
                    if (s != this)
                        s.Unselect();
            }
        }
        protected virtual void OnDoubleClick()
        {

        }
        public virtual void Select()
        {
            selected = true;
            current.Add(this);
        }
        public virtual void Unselect()
        {
            selected = false;
            current.Remove(this);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (Time.time - mouseLastExitTime > mouseExitToEnterTimeout)
            {
                SoundEffect.Hover(this);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            mouseLastExitTime = Time.time;
        }
        bool unselectOtherWhenClick = false;
        public void OnPointerDown(PointerEventData eventData)
        {
            SoundEffect.Click(this);
            if (eventData.button == 0)
            {
                if (Time.time - lastClickTime < doubleClickDelay) OnDoubleClick();
                else lastClickTime = Time.time;


                unselectOtherWhenClick = false;
                if (CamControl.ctrlDown)
                {
                    if (selected)
                    {
                        Unselect();
                    }
                    else
                    {
                        Select();
                    }
                }
                else if (CamControl.shiftDown)
                {
                    if (!selected)
                    {
                        Select();
                    }
                }
                else
                {
                    if (!selected)
                    {
                        foreach (Selectable s in current.ToArray())
                            if (s != this)
                                s.Unselect(); Select();
                    }
                    else
                    {
                        unselectOtherWhenClick = true;
                    }
                }
            }
        }
        public override void OnDestroy_(Newtonsoft.Json.Linq.JToken message)
        {
            base.OnDestroy_(message);
            if (current.Contains(this)) current.Remove(this);
        }
    }
}
