using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CoolDown
{
    float span;
    float waitTime = 0;
    bool pending = false;
    public CoolDown(float hz)
    {
        span = 1f / hz;
    }
    public void Request()
    {
        pending = true;
    }
    public void Delay(float t=-1)
    {
        waitTime = Time.time + (t == -1 ? span : t);
        pending = false;
    }
    public bool Update()
    {
        if (pending && Time.time > waitTime)
        {
            pending = false;
            waitTime = Time.time + span;
            return true;
        }
        return false;
    }
}
namespace GraphUI
{
    public class Node : Selectable, IEndDragHandler, IBeginDragHandler, IDragHandler
    {
        
        public List<Port> ins, outs;
        public RectTransform panel;
        public string id, Name;
        [SerializeField] 
        UnityEngine.UI.Outline outline_select,outline_running;
        [SerializeField]
        Color outlineUnselectedColor, outlineHoverColor, outlineSelectedColor,outlineRunningColor,outlinePendingColor;
        [SerializeField]
        public List<UnityEngine.UI.Graphic> changeColorOnSelect;
        public virtual Port GetPort(bool isInput = true, string var_name = "")
        {
            return isInput ? ins[0] : outs[0];
        }
        public virtual void Start()
        {
            targetPos = transform.position;
            outline_select.effectColor = outlineUnselectedColor;
            outline_running.effectColor = new Color(0, 0, 0, 0);
        }

        protected override void Remove()
        {
            // Remove node from this client
            // Invoked by X button
            base.Remove();
            for (int i = 0; i < ins.Count; i++)
                ins[i].Remove();

            for (int i = 0; i < outs.Count; i++)
                outs[i].Remove();

            Manager.ins.RemoveNode(this);
            StartCoroutine(Removing()); // Play removing animation and destroy the game objecct
        }

        public virtual void Update()
        {
            if (dragging) 
                if (Input.GetMouseButton(0) && !EventSystem.current.currentSelectedGameObject)
                {
                    if (CamControl.worldMouseDelta.sqrMagnitude > 0)
                    {
                        Move(CamControl.worldMouseDelta);
                    }
                }
            if (recvMoveCD.Update())
            {
                transform.position = targetPos;
            }
            if (sendMoveCD.Update())
            {
                Manager.ins.MoveNode(this, transform.position);//Ask the server to move the node.
            }
        }

        public float upPad = 0.1f, downPad = 0.1f;
        public virtual void Reshape(float w, float l, float r) { }//Trapezoid shaped node


        readonly CoolDown recvMoveCD = new CoolDown(hz: 10);
        readonly CoolDown sendMoveCD = new CoolDown(hz: 10);
        Vector3 targetPos;

        public bool dragging = false;
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;
            foreach (Selectable s in Selectable.current)
                if (s is Node node)
                {
                    node.dragging = false;
                }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;
            if (!selected) OnPointerClick(eventData); // if not selected, select it first
            foreach (Selectable s in Selectable.current)
                if (s is Node node)
                {
                    node.dragging = true;
                    dragged = true; // Prevent Selectable from selecting
                }
        }

        public void OnDrag(PointerEventData eventData) { }

        public void Move(Vector3 movement) // override this if some node classes shouldn't be moveable
        {
            transform.Translate(movement);// Although the server will send a move message back soon, to avoid a bad UX, the node in frontend moves in advance.
            sendMoveCD.Request();
            recvMoveCD.Delay(1);
        }
        public void RawMove(Vector3 p)
        {
            recvMoveCD.Request();
            targetPos = p; // can't set transform.position outside update
        }
        public void OnMouseDrag()
        {
            
        }


        public virtual IEnumerator Creating()//Drag and drop?
        {
            yield return null;
            /*
            while (Input.GetMouseButton(0))
            {

                yield return null;
            }*/
            Manager.ins.AddNode(this);
        }
        public virtual IEnumerator Removing()// SAO-like?
        {
            yield return null;
            Destroy(gameObject);
        }



        IEnumerator SmoothChangeColor(UnityEngine.UI.Outline outline,Color target,float speed=15)
        {
            Color original = outline.effectColor;
            float t = 1f;
            while (t >0.02f)
            {
                t *= Mathf.Pow(0.5f, Time.deltaTime*speed);
                outline.effectColor = Color.Lerp(target, original, t);
                yield return null;
            }
            outline.effectColor = target;
        }

        IEnumerator SmoothChangeColor(List<UnityEngine.UI.Graphic> graphics, Color target, float speed = 15)
        {
            if (graphics.Count == 0) yield break;
            Color original = graphics[0].color;
            float t = 1f;
            while (t > 0.02f)
            {
                t *= Mathf.Pow(0.5f, Time.deltaTime * speed);
                var c = Color.Lerp(target, original, t);
                foreach (UnityEngine.UI.Graphic g in graphics)
                {
                    g.color = new Color(c.r, c.g, c.b, g.color.a);
                }
                    
                yield return null;
            }
            foreach(UnityEngine.UI.Graphic g in graphics)
                g.color = new Color(target.r, target.g, target.b, g.color.a);
        }

        IEnumerator changeColor1, changeColor2;
        public override void Select()
        {
            base.Select();
            if(changeColor1!=null)
                StopCoroutine(changeColor1);
            StartCoroutine(changeColor1 = SmoothChangeColor(changeColorOnSelect, outlineSelectedColor));
            
        }
        public override void Unselect()
        {
            base.Unselect();
            if (changeColor1 != null)
                StopCoroutine(changeColor1);
            StartCoroutine(changeColor1 = SmoothChangeColor(changeColorOnSelect, outlineUnselectedColor));
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!selected)
            {
                if (changeColor1 != null)
                    StopCoroutine(changeColor1);
                StartCoroutine(changeColor1 = SmoothChangeColor(changeColorOnSelect, outlineHoverColor));
            }
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!selected)
            {
                if (changeColor1 != null)
                    StopCoroutine(changeColor1);
                StartCoroutine(changeColor1 = SmoothChangeColor(changeColorOnSelect, outlineUnselectedColor));
            }
        }
        public void DisplayActivate()
        {
            if (changeColor2 != null)
                StopCoroutine(changeColor2);
            StartCoroutine(changeColor2 = SmoothChangeColor(outline_running, outlineRunningColor));
        }
        public void DisplayInactivate()
        {
            if (changeColor2 != null)
                StopCoroutine(changeColor2);
            outline_running.effectColor = outlineRunningColor;
            StartCoroutine(changeColor2 = SmoothChangeColor(outline_running, new Color(0, 0, 0, 0)));
        }
        public void DisplayPending()
        {
            if (changeColor2 != null)
                StopCoroutine(changeColor2);
            StartCoroutine(changeColor2 = SmoothChangeColor(outline_running, outlinePendingColor));
        }
        public virtual void ShowOutput(string output)
        {
            throw new System.NotImplementedException();
        }
    }
}
