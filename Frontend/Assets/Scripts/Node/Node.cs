using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace GraphUI
{
    public class Node : Selectable,  IBeginDragHandler,IEndDragHandler,IDragHandler,ObjectSync.IObjectClient
    {
        #region vars
        //public List<Port> ports;
        public string  Name;
        //public Dictionary<string, Comp> components{ get; set; };

        ObjectSync.Attribute<bool> Draggable,IsDemo;

        [SerializeField] Transform componentPanel;
        [SerializeField] UnityEngine.UI.Image outline;

        public ColorTransition selectColorTransition, runColorTransition;


        #endregion

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


        // ObjectSync.IObjectClient ====================
        public override void OnDestroy_(JToken message)
        { 
            base.OnDestroy_(message);
            StartCoroutine(Removing());
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
            if (IsDemo.Value)
            {/*
                GameObject clone = Instantiate(gameObject);
                clone.transform.SetParent(spaceClient.transform);
                clone.GetComponent<Node>().enabled = false;
                clone.AddComponent<>*/
                return;
            }
            else if(Draggable.Value)
            { 
                if (!selected) OnPointerClick(eventData); // if not selected, select it first
                dragged = true; // Prevent Selectable from selecting
                foreach (Selectable s in current)
                    if (s is Node node)
                        node.BeginDrag();
            }
        }
        public void BeginDrag() { dragging = true; desiredPosition = transform.localPosition; }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Draggable.Value) return;
            if (eventData.button != 0) return;
            foreach (Selectable s in current)
                if (s is Node node)
                {
                    node.EndDrag();
                }
        }
        public void EndDrag() { dragging = false; }
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


    }
}
