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
        public ControlPort inControl,outControl;
        public List<DataPort> inData, outData;

        public string id, Name;
        [SerializeField] 
        UnityEngine.UI.Outline outline_running;
        [SerializeField]
        Color unselectedColor, hoverColor, selectedColor,outlineRunningColor,outlinePendingColor;
        [SerializeField]
        List<UnityEngine.UI.Graphic> lights;

        public string type; // class name in python
        public bool isDemo;

        APIMessage.NewNode.Info demoInfo;

        public virtual void Init(APIMessage.NewNode.Info info)
        {
            demoInfo = info;
            type = info.type;
            name = Name = info.name;
            id = info.id;
            transform.position = info.pos;

            isDemo = id == "-1";
            if(id != "-1")
            {
                transform.localScale = Vector3.one * 0.002f;
                Manager.ins.Nodes.Add(id,this);
            }
            else
            {
                transform.localScale = Vector3.one * 0.5f;
                Manager.ins.DemoNodes.Add(type, this);
                transform.SetParent(Manager.ins.demoNodeContainer);
            }
        }

        public virtual void Start()
        {
            targetPos = transform.position;
            outline_running.effectColor = new Color(0, 0, 0, 0);
        }

        public override void Remove()
        {
            // Remove node from this client
            // Invoked by X button
            base.Remove();
            inControl.Remove();
            outControl.Remove();
            for (int i = 0; i < inData.Count; i++)
                inData[i].Remove();

            for (int i = 0; i < outData.Count; i++)
                outData[i].Remove();

            Manager.ins.RemoveNode(this);
            StartCoroutine(Removing()); // Play removing animation and destroy the game objecct
        }

        public virtual void Update()
        {
            if (isDemo) return;
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


        public virtual void Reshape(float w, float l, float r) { }//Trapezoid shaped node


        readonly CoolDown recvMoveCD = new CoolDown(hz: 10);
        readonly CoolDown sendMoveCD = new CoolDown(hz: 10);
        Vector3 targetPos;

        public bool dragging = false;


        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;
            if (isDemo) // Create a new node
            {
                //ClearSelection();
                Node newNode = Instantiate(gameObject).GetComponent<Node>();
                newNode.id = Manager.ins.GetNewID();
                newNode.isDemo = false;
                //newNode.transform.position = CamControl.worldMouse;
                newNode.transform.localScale = 0.002f * Vector3.one;
                StartCoroutine(newNode.DragCreating());
                return;
            }
            if (!selected) OnPointerClick(eventData); // if not selected, select it first
            foreach (Selectable s in current)
                if (s is Node node)
                {
                    node.dragging = true;
                    dragged = true; // Prevent Selectable from selecting
                }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;
            foreach (Selectable s in current)
                if (s is Node node)
                {
                    node.dragging = false;
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

        public virtual IEnumerator Creating()
        {
            yield return null;
            /*
            while (Input.GetMouseButton(0))
            {

                yield return null;
            }*/
            Manager.ins.AddNode(this);
        }
        public virtual IEnumerator DragCreating()//Drag and drop
        {
            yield return null;
            
            while (Input.GetMouseButton(0))
            {
                transform.position = CamControl.worldMouse;
                yield return null;
            }
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
            StartCoroutine(changeColor1 = SmoothChangeColor(lights, selectedColor));
            
        }
        public override void Unselect()
        {
            base.Unselect();
            if (changeColor1 != null)
                StopCoroutine(changeColor1);
            StartCoroutine(changeColor1 = SmoothChangeColor(lights, unselectedColor));
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!selected)
            {
                if (changeColor1 != null)
                    StopCoroutine(changeColor1);
                StartCoroutine(changeColor1 = SmoothChangeColor(lights, hoverColor));
            }
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!selected)
            {
                if (changeColor1 != null)
                    StopCoroutine(changeColor1);
                StartCoroutine(changeColor1 = SmoothChangeColor(lights, unselectedColor));
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

    }
}
