using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI
{
    public class Flow : Selectable
    {

        public string id;
        public Port tail, head;
        public Line line;
        public bool hard = false;
        public Color colorSelected, colorHover, colorUnselected, colorActivated;


        protected virtual void Start()
        {
            transform.SetParent(Manager.ins.canvasTransform);
            line = GetComponent<Line>();
        }
        protected virtual void Update()
        {
            if (tail && head)
            {
                
                line.Tail = tail.transform.position;
                line.Head = head.transform.position;
            }
        }
        public void Move(Vector3 movement)
        {
            transform.Translate(movement);
            if (hard)
            {
                tail.node.Move(movement);
                head.node.Move(movement);
                
            }
        }

        public IEnumerator Creating()
        {
            Manager.ins.state = Manager.State.draggingFlow;
            yield return null; // wait for next frame
            if (tail && head)
            {
                Manager.ins.state = Manager.State.idle;
                yield break;
            }
            bool dragTail = head;

            Port targetPort = null;
            while (Input.GetMouseButton(0)) // while mouse hold
            {
                targetPort = null;
                if (CamControl.colliderHover)
                    targetPort = CamControl.colliderHover.GetComponent<Port>();
                if (targetPort)
                    if (!targetPort.AcceptEdge(this)) targetPort = null;
                Vector3 dragPos;
                dragPos = targetPort ? targetPort.transform.position : CamControl.worldMouse;

                // reshape
                if (dragTail)
                {
                    line.Tail = dragPos;
                    line.Head = head.transform.position;
                }
                else
                {
                    line.Tail = tail.transform.position;
                    line.Head = dragPos;
                }
                yield return null;
            }


            // after mouse release
            if (targetPort)
            {
                if (dragTail)
                    tail = targetPort;
                else
                    head = targetPort;

                targetPort.Edges.Add(this);
                Manager.ins.AddFlow(this);
                Manager.ins.state = Manager.State.idle;
                yield break;
            }
            else
            {
                RawRemove();
            }
            Manager.ins.state = Manager.State.idle;
        }
        protected override void Remove()
        {
            base.Remove();
            Manager.ins.RemoveFlow(this);
            RawRemove();
        }
        public void RawRemove()
        {
            if (tail) tail.Disconnect(this);
            if (head) head.Disconnect(this);

            Destroy(gameObject);
        }


        IEnumerator changeColor;
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!selected)
            {
                if (changeColor != null)
                    StopCoroutine(changeColor);
                StartCoroutine(changeColor = line.ChangeColor(colorHover));
            }
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!selected)
            {
                if (changeColor != null)
                    StopCoroutine(changeColor);
                StartCoroutine(changeColor = line.ChangeColor(colorUnselected));
            }
        }
        public override void Select()
        {
            base.Select();
            if (changeColor != null)
                StopCoroutine(changeColor);
            StartCoroutine(changeColor = line.ChangeColor(colorSelected));

        }
        public override void Unselect()
        {
            base.Unselect();
            if (changeColor != null)
                StopCoroutine(changeColor);
            StartCoroutine(changeColor = line.ChangeColor(colorUnselected));
        }
        // TODO !!!!!!!!!!!!!!!!!!!
        /*
        public void DisplayActivate()
        {
            if (changeColor != null)
                StopCoroutine(changeColor);
            StartCoroutine(changeOutlineColor = ChangeOutlineColor(outline_running, outlineRunningColor));
        }
        public void DisplayInactivate()
        {
            if (changeColor != null)
                StopCoroutine(changeColor);
            outline_running.effectColor = outlineRunningColor;
            StartCoroutine(changeOutlineColor = ChangeOutlineColor(outline_running, new Color(0, 0, 0, 0)));
        }
        public void DisplayPending()
        {
            if (changeColor != null)
                StopCoroutine(changeColor);
            StartCoroutine(changeOutlineColor = ChangeOutlineColor(outline_running, outlinePendingColor));
        }*/
    }
}