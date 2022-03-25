using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        //pending = false;
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
    public class Node : Selectable,  IBeginDragHandler,IEndDragHandler,IDragHandler,ObjectSync.IObjectClient
    {
        #region vars
        ObjectSync.Object syncObject;
        public List<Port> ports;
        public string id, Name;
        public List<Comp> comps = new List<Comp>();
        //public Dictionary<string, Comp> components{ get; set; };
        ObjectSync.Attribute<Vector3> Pos;
        ObjectSync.Attribute<string> Output;
        [SerializeField] Transform componentPanel;
        [SerializeField] UnityEngine.UI.Image outline;

        bool moveable = true;
        public ColorTransition selectColorTransition, runColorTransition;

        JToken info;
        #endregion

        public bool createByThisClient = false;
        public virtual void Init(JToken info)
        {
            /*
             * Parse the node info to setup the node.
            */

            if (syncObject.id != "-1")
            {
                transform.localScale = Vector3.one * 0.002f;
                SpaceClient.ins.Nodes.Add(id, this);
            }
            else
            {
                this.info = info;
                transform.localScale = Vector3.one * 0.3f;
                transform.SetParent(SpaceClient.ins.demoNodeContainer.FindCategoryPanel((string)info["category"], "CategoryPanelForNodeList"));
            }

            // If createByThisClient, set Pos attribute after the node is dropped to its initial position (in OnDragCreating()).
            Pos = syncObject.RegisterAttribute<Vector3>( "transform/pos", (v) => { transform.position = v; }, "space",transform.position);
            Output = syncObject.RegisterAttribute<string>("output", (v) => {OnOutputChanged(v);},"","");
            syncObject.RegisterAttribute<Vector3>("color", (v) => { var w = (Vector3)v; SetColor(new Color(w.x, w.y, w.z)); },"");
        }

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

        }

        public virtual void Start()
        {
            sendOnScrollTo = isDemo ?(MonoBehaviour)/*Unity needs this type conversion*/CamControl.ins.nodeList : CamControl.ins  ; 
        }

        public virtual void RecieveUpdateMessage(JToken message){
            switch ((string)message["command"])
            {
                case "clr":
                    ClearOutput();
                    break;

                case "out":
                    AddOutput((string)message["info"]);
                    break;

                case "act":
                    switch ((string)message["info"])
                    {
                        case "0": DisplayInactive(); break;
                        case "1": DisplayPending(); break;
                        case "2": DisplayActive(); break;
                    }
                    break;
                case "rmv":
                    StartCoroutine(Removing());
                    break;
            }
        }

        public override void Destroy()
        {
            // Remove node from this client
            // Invoked by X button
            base.Destroy();
            foreach (Port port in ports)
                port.Remove();
            if(id!="-1")
                SpaceClient.ins.SendToServer(new API_update_message(id,"rmv",""));
            StartCoroutine(Removing()); // Play removing animation and destroy the game objecct
        }

        public virtual void LateUpdate()
        {
            if (isDemo) return;
            if (dragging) 
                if (Input.GetMouseButton(0) && !EventSystem.current.currentSelectedGameObject)
                {
                    if (CamControl.worldMouseDelta.sqrMagnitude > 0)
                    {
                        desiredPosition += CamControl.worldMouseDelta;
                        Pos.Set(SpaceClient.ins.GetSnappedPosition(desiredPosition));
                        //Pos.Set(transform.position + CamControl.worldMouseDelta);
                        //transform.position += CamControl.worldMouseDelta;


                    }
                } 

            foreach(var attr in attributes)
            {
                attr.Value.Update();
            }
        }

        public virtual void AddOutput(string output)
        {
            Output.Set(Output.Value + output,send:false);
        }
        public virtual void ClearOutput()
        {
            Output.Set("",send:true);
        }

        public virtual void Reshape(float w, float l, float r) { }//Trapezoid shaped node

        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            SpaceClient.ins.Activate(this);
        }



        bool dragging = false;
        Vector3 desiredPosition;
 
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;

            if (isDemo) // Create a new node
            {
                //Node newNode = Instantiate(gameObject).GetComponent<Node>();newNode.id = Manager.ins.GetNewID();newNode.isDemo = false;
                info["id"] = SpaceClient.ins.GetNewID();
                Node newNode = SpaceClient.ins.CreateNode(info,true);
                newNode.transform.localScale = 0.002f * Vector3.one;
                StartCoroutine(newNode.DragCreating());
                
                return;
            }  
            else if(moveable)
            { 
                if (!selected) OnPointerClick(eventData); // if not selected, select it first
                dragged = true; // Prevent Selectable from selecting
                foreach (Selectable s in current)
                    if (s is Node node)
                        node.BeginDrag();
            }
        }
        public void BeginDrag() { dragging = true; desiredPosition = transform.position; }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!moveable) return;
            if (eventData.button != 0) return;
            foreach (Selectable s in current)
                if (s is Node node)
                {
                    node.EndDrag();
                }
        }
        public void EndDrag() { dragging = false; }
        public void OnDrag(PointerEventData eventData) { }

        public virtual IEnumerator DragCreating()//Drag and drop
        {
            moveable = false;
            
            
            while (Input.GetMouseButton(0))
            {
                transform.position = SpaceClient.ins.GetSnappedPosition( CamControl.worldMouse);
                yield return null;
            }
            moveable = true;

            Pos = Attribute.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; }, history_in: "env",initValue:transform.position);
            
        }
        public virtual IEnumerator Removing()// SAO-like?
        {
            yield return null;
            Unselect();
            SpaceClient.ins.Nodes.Remove(id);
            Destroy(gameObject);
        }


        public override void Select()
        {
            if (selected) return;
            base.Select();

            if(!isDemo)
                SpaceClient.ins.nodeInspector.Open(this);

            selectColorTransition.Switch("selected");
            
        }
        public override void Unselect()
        {
            SpaceClient.ins.nodeInspector.Clear();

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
            outline.color = color;
            foreach(Comp comp in comps)
            {
                comp.SetColor(color);
                
            }
            /*
            selectColorTransition.SetColor("selected", color*0.8f);
            selectColorTransition.SetColor("unselected", Color.black);
            selectColorTransition.SetColor("hover", Color.white*0.3f);
            if (Application.isEditor)
                selectColorTransition.SetDefault("selected");
            else
                selectColorTransition.SetDefault("unselected");*/
        }
        void OnOutputChanged(string output)
        {

        }

        public void RecieveMessage(JToken message)
        {
            throw new System.NotImplementedException();
        }

        public void OnCreate(JToken message, ObjectSync.Object obj)
        {
            syncObject = obj;
            Init(message["d"]);
        }

        public void OnDestroy()
        {
            throw new System.NotImplementedException();
        }
    }
}
