using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NNNode
{
    public interface ISlot
    {
        public bool AcceptsDrop(IDroppable dropped);
    }

    public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Static
        public static Slot Current
        {
            get { return current; }
            set
            {
                if (value != null && current == null)
                {
                    Droppable.EnterSlot_s(value.slot);
                }
                else if (value == null && current != null)
                {
                    Droppable.ExitSlot_s(current.slot);
                }
                else if (value != null && current != null && value != current)
                {
                    Droppable.ExitSlot_s(current.slot);
                    Droppable.EnterSlot_s(value.slot);
                }
                current = value;
            }
        }
        static Slot current;
        #endregion

        public ISlot slot;
        void Awake()
        {
            slot = GetComponent<ISlot>();
            if (slot == null)
            {
                throw new System.NullReferenceException($"Game object {name} with Slot must have a component implementing ISlot.");
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerEnter.GetComponentInParent<Slot>() != this) return;
            Current = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(eventData.pointerEnter!=null)
                if (eventData.pointerEnter.GetComponentInParent<Slot>() != this) return;
            if (Current == this) Current = null;
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}