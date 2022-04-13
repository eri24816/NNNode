using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI {
    public interface IDroppable
    {
        public void BeginDrag();
        public bool AcceptsDropOn(ISlot target);
        public void EnterSlot(ISlot slot);
        public void ExitSlot(ISlot slot);
        public void DropOn(ISlot target);
        public void Drag(Vector3 delta);
        public void EndDrag();
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class Droppable : MonoBehaviour
    {
        #region Static
        public static readonly List<Droppable> current = new();
        public static void EnterSlot_s(ISlot slot)
        {
            if (current == null) return;
            foreach (var droppable in current)
            {
                droppable.EnterSlot(slot);
            }
        }
        public static void ExitSlot_s(ISlot slot)
        {
            if (current == null) return;
            foreach (var droppable in current)
            {
                droppable.ExitSlot(slot);
            }
        }
        public static void BeginDrag_s()
        {
            current.Clear();
            foreach (var selectable in Selectable.current)
            {
                Droppable droppable = selectable.GetComponent<Droppable>();
                if (droppable != null)
                {
                    current.Add(droppable);
                    droppable.StartCoroutine(droppable.Dragging());
                }
            }
        }
        public void EndDrag_s()
        {
            foreach(var draggable in current)
            {
                draggable.isDragging = false;
            }
            current.Clear();
        }
        #endregion

        CanvasGroup canvasGroup;
        public IDroppable obj;
        ISlot slot;
        bool isDragging = true;
        Vector3 delta;
        void Awake()
        {
            obj = GetComponent<IDroppable>();
            if (obj == null)
            {
                throw new System.NullReferenceException($"Game object {name} with Droppable must have a component implementing IDroppable.");
            }
            canvasGroup = GetComponent<CanvasGroup>();
        }
        IEnumerator Dragging()
        {
            isDragging = true;
            delta = Vector3.zero;
            yield return new WaitForEndOfFrame();
            obj.BeginDrag();
            using (new NoBlockRaycast(canvasGroup))
            {
                while (isDragging)
                {
                    if (!Input.GetMouseButton(0))
                    {
                        EndDrag_s();
                    }
                    delta += CamControl.worldMouseDelta;
                    obj.Drag(delta);
                    yield return null;
                }
            }
            if(slot != null)
            {
                if (obj.AcceptsDropOn(slot) && slot.AcceptsDrop(obj))
                {
                    obj.DropOn(slot);
                }
            }
            obj.EndDrag();
        }

        void EnterSlot(ISlot slot)
        {
            if (obj.AcceptsDropOn(slot) && slot.AcceptsDrop(obj))
            {
                obj.EnterSlot(slot);
                this.slot = slot;
            }
        }
        void ExitSlot(ISlot slot)
        {
            if (slot == this.slot)
            {
                obj.ExitSlot(slot);
            }
        }

        private void OnDestroy()
        {
            if (current.Contains(this)) current.Remove(this);
        }
    }
}

class NoBlockRaycast : System.IDisposable
{
    CanvasGroup canvasGroup;
    public NoBlockRaycast(CanvasGroup canvasGroup)
    {
        canvasGroup.blocksRaycasts = false;
        this.canvasGroup = canvasGroup;
    }
    public void Dispose()
    {
        canvasGroup.blocksRaycasts = true;
    }
}