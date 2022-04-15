using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace NNNode
{
    public class Node : Selectable,  IBeginDragHandler,IEndDragHandler,IDragHandler,ObjectSync.IObjectClient,IDroppable,ISlot
    {
        #region vars
        //public List<Port> ports;
        public string  Name;
        //public Dictionary<string, Comp> components{ get; set; };

        public ObjectSync.Attribute<bool> Draggable,IsDemo;
        protected ObjectSync.Attribute<Vector3> Pos;

        [SerializeField] Transform componentPanel;
        [SerializeField] UnityEngine.UI.Image outline;

        public ColorTransition selectColorTransition, runColorTransition;

        Droppable droppable;

        #endregion

        private void Start()
        {
            droppable = GetComponent<Droppable>();
            if (IsDemo.Value)
            {
                foreach (var d in GetComponentsInChildren<Droppable>())
                {
                    if (d.gameObject == gameObject) continue;
                    Destroy(d);
                }
                foreach (var d in GetComponentsInChildren<Slot>())
                {
                    if (d.gameObject == gameObject) continue;
                    Destroy(d);
                }
                foreach (var d in GetComponentsInChildren<Selectable>())
                {
                    if (d.gameObject == gameObject) continue;
                    if(d is Node node)
                    {
                        node.Draggable.Set(false);
                    }
                    //Destroy(d);
                }
            }
        }

        /*
        protected virtual void CreatePort(JToken portInfo)
        {
            GameObject prefab;
            if ((string)portInfo["type"] == "ControlPort")
                prefab = (bool)portInfo["isInput"] ? SpaceClient.ins.inControlPortPrefab : SpaceClient.ins.outControlPortPrefab;
            else
                prefab = (bool)portInfo["isInput"] ? SpaceClient.ins.inDataPortPrefab : SpaceClient.ins.outDataPortPrefab;

            Port newPort = Instantiate(prefab, transform).GetComponent<Port>();
            ports.Add(newPort);
            newPort.Init(this, portInfo);
        }

        public virtual void SetupPort(Port port, JToken portInfo)
        {
            // Called by Port.Init()
            ((RectTransform)port.transform).anchorMin = ((RectTransform)port.transform).anchorMax = new Vector2((float)portInfo["pos"]["x"] / 2 + .5f, (float)portInfo["pos"]["y"] / 2 + .5f);

        }*/

        public override void OnParentChanged(string parent_id)
        {
            base.OnParentChanged(parent_id);
            if(Pos != null)
                TrySetPosition(Pos.Value);
        }

        // ObjectSync.IObjectClient ====================
        public override void OnDestroy_(JToken message)
        { 
            base.OnDestroy_(message);
            //StartCoroutine(Removing());
        }
        public override void RecieveMessage(JToken message)
        {
            switch ((string)message["command"])
            {
                case "act":
                    switch ((string)message["info"])
                    {
                        case "0": DisplayInactive(); break;
                        case "1": DisplayPending(); break;
                        case "2": DisplayActive(); break;
                    } 
                    break;
                default: base.RecieveMessage(message); break;
            }
        }
        public override void OnCreate(JToken message, ObjectSync.Object obj)
        {
            base.OnCreate(message,obj);

            syncObject.RegisterAttribute<Vector3>("color", (v) => { var w = (Vector3)v; SetColor(new Color(w.x, w.y, w.z)); });

            Pos = syncObject.RegisterAttribute<Vector3>("transform/pos", (v)=> { TrySetPosition(v); }, "parent", Vector3.zero);
            Draggable = syncObject.RegisterAttribute<bool>("draggable", initValue: true);
            IsDemo = syncObject.RegisterAttribute<bool>("is_demo", initValue: false);

        }
        //============================================================

        public virtual void LateUpdate()
        {
            if (!Draggable.Value) return;
            if (dragging) 
                if (Input.GetMouseButton(0) && !EventSystem.current.currentSelectedGameObject)
                {
                    if (CamControl.worldMouseDelta.sqrMagnitude > 0)
                    {
                        desiredPosition += transform.worldToLocalMatrix.MultiplyVector(CamControl.worldMouseDelta);
                        Pos.Set(SpaceClient.ins.GetSnappedPosition(desiredPosition));
                    }
                }
        }

        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            // send activate
        }

        bool dragging = false;
        Vector3 desiredPosition;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;
            if (Draggable.Value)
            {
                if (IsDemo.Value)
                {
                    string creationTag = $"create/{ Random.Range(0, 10000000) }";
                    spaceClient.space.SendMessage(new ObjectSync.API.Out.Create
                    {
                        parent = "0",
                        d = {
                        type=syncObject.type ,
                        attributes = new Dictionary<string, ObjectSync.API.Out.Create.Attribute>{
                            //new ObjectSync.API.Out.Create.Attribute{name="transform/pos",value=new Vector3(0,0,-1),history_object = "parent"},
                            ["tag/"+creationTag]=new ObjectSync.API.Out.Create.Attribute{value="",history_object = "none"}
                        }
                    }
                    });
                    spaceClient.creationWaiter.Add(new(creationTag, (o) =>
                    {
                        ClearSelection();
                        ((Selectable)o).Select();
                        o.syncObject.Tag("creating_drag");
                        Droppable.BeginDrag_s();
                    }));
                }
                else
                {
                    Droppable.BeginDrag_s();
                }
            }
            else
            {
                transform.parent.GetComponentInParent<IBeginDragHandler>().OnBeginDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData){}
        public void OnDrag(PointerEventData eventData) { }

        public virtual IEnumerator Removing()// SAO-like?
        {
            yield return null;
            Unselect();
            Destroy(gameObject);
        }


        public override void Select()
        {
            if (selected) return;

            selectColorTransition.Switch("selected");
            base.Select();

        }
        public override void Unselect()
        {
            //SpaceClient.ins.nodeInspector.Clear();

            base.Unselect();

            selectColorTransition.Switch("unselected");
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!selected)
                selectColorTransition.Switch("hover");
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!selected)
                selectColorTransition.Switch("unselected");
        }
        public void DisplayActive()
        {
            runColorTransition.Switch("active");
        }
        public void DisplayInactive()
        {
            runColorTransition.ImmidiateSwitch("active");
            runColorTransition.Switch("inactive");
        }
        public void DisplayPending()
        {
            runColorTransition.Switch("pending");
        }
        public virtual void SetColor(Color color)
        {
            // Called by the color attribute setter

            /*
            selectColorTransition.SetColor("selected", color*0.8f);
            selectColorTransition.SetColor("unselected", Color.black);
            selectColorTransition.SetColor("hover", Color.white*0.3f);
            if (Application.isEditor)
                selectColorTransition.SetDefault("selected");
            else
                selectColorTransition.SetDefault("unselected");*/
        }

        void TrySetPosition(Vector3 pos,Space space = Space.Self)
        {
            //if(space == Space.World) pos = transform.worldToLocalMatrix.MultiplyPoint(pos);
            if (transform.parent.GetComponentInParent<UnityEngine.UI.LayoutGroup>() == null)
            {
                if(space == Space.Self)
                    transform.localPosition = pos;
                else
                    transform.position = pos;
            }
        }

        #region IDroppable
        Vector3 dragOrigin;
        public void BeginDrag()
        {
            if (syncObject.TaggedAs("creating_drag"))
            {
                var tf = transform as RectTransform;
                TrySetPosition( CamControl.worldMouse - tf.localToWorldMatrix.MultiplyVector(tf.rect.center));
            }
            dragOrigin = transform.position;
        }
        bool IDroppable.AcceptsDropOn(ISlot target)
        {
            return true;
        }

        void IDroppable.EnterSlot(ISlot slot)
        {
            transform.SetParent(((MonoBehaviour)slot).transform);
        }

        void IDroppable.ExitSlot(ISlot slot)
        {
            transform.SetParent(spaceClient.Root.transform);
        }

        void IDroppable.Drag(Vector3 delta)
        {
            if (syncObject.TaggedAs("creating_drag"))
            {
                var tf = transform as RectTransform;
                TrySetPosition( CamControl.worldMouse - tf.localToWorldMatrix.MultiplyVector(tf.rect.center),Space.World);
            }
            else
            {
                TrySetPosition(dragOrigin + delta, Space.World);
            }
            
        }
        void IDroppable.EndDrag(ISlot slot)
        {
            if (syncObject.TaggedAs("creating_drag"))
            {
                syncObject.Untag("creating_drag");
                using (syncObject.NoHistory_())
                {
                    if (transform.parent.GetComponentInParent<UnityEngine.UI.LayoutGroup>() == null)
                        Pos.Set(transform.localPosition);
                    if (slot != null)
                    {
                        ParentID.Set(((ObjectClient)slot).syncObject.id);
                    }
                    else
                    {
                        ParentID.Set(spaceClient.Root.syncObject.id);
                    }
                }
            }
            else
                using (spaceClient.space.CommandSequence_())
                {
                    if (transform.parent.GetComponentInParent<UnityEngine.UI.LayoutGroup>() == null)
                        Pos.Set(transform.localPosition);
                    if (slot != null)
                    {
                        ParentID.Set(((ObjectClient)slot).syncObject.id);
                    }
                    else
                    {
                        ParentID.Set(spaceClient.Root.syncObject.id);
                    }
                }
        }
        #endregion

        public bool AcceptsDrop(IDroppable dropped)
        {
            if (IsDemo.Value) return false;
            return true;
        }

    }
}
