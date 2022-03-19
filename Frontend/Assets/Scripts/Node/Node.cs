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
    public class Node : Selectable,  IUpdateMessageReciever,IBeginDragHandler,IEndDragHandler,IDragHandler
    {
        #region vars
        public List<Port> ports;
        public string id, Name;
        public Dictionary<string, Attribute> attributes ;
        public List<Comp> comps = new List<Comp>();
        //public Dictionary<string, Comp> components{ get; set; };
        Attribute Pos, Output;
        [SerializeField] Transform componentPanel;
        [SerializeField] UnityEngine.UI.Image outline;

        public string type; // class name in python
        public bool isDemo;
        bool moveable = true;
        public ColorTransition selectColorTransition, runColorTransition;

        JToken info;
        #endregion

        public class API_new
        {
            public string command = "new";
            public Info info;
            [System.Serializable]
            public struct Info
            {
                public string id;
                public string name;
                public string category;
                public string type;
                public Vector3 pos;
                public string frontend_type;
                public Port.API_new[] portInfos; // PortInfo classes (or structs?) are defined in each node classes
                public Comp.API_new[] comp;
            }
            
            // TODO: directly take node as argument
            public API_new(Node node) {info.id = node.id; info.name = node.name; info.type = node.type; info.pos = node.transform.position; }
        }

        public bool createByThisClient = false;
        public virtual void Init(JToken info)
        {
            /*
             * Parse the node info to setup the node.
            */


            type = (string)info["type"];
            name = Name = (string)info["name"];
            id = (string)info["id"];

            attributes = new Dictionary<string, Attribute>();
            //components = new Dictionary<string, Comp>();

            isDemo = id == "-1";
            if (id != "-1")
            {
                transform.localScale = Vector3.one * 0.002f;
                Manager.ins.Nodes.Add(id, this);
            }
            else
            {
                this.info = info;
                transform.localScale = Vector3.one * 0.3f;
                Manager.ins.DemoNodes.Add(type, this);
                transform.SetParent(Manager.ins.demoNodeContainer.FindCategoryPanel((string)info["category"], "CategoryPanelForNodeList"));
            }

            if (createByThisClient)
                Manager.ins.SendToServer(new API_new(this));
            else
                // If createByThisClient, set Pos attribute after the node is dropped to its initial position (in OnDragCreating()).
                Pos = Attribute.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; }, history_in: "env");
                Output = Attribute.Register(this, "output", "string", (v) => {OnOutputChanged((string)v);}, history_in: "",initValue:"");

            foreach (var attr_info in info["attr"])
            {
                var new_attr = new Attribute(this, (string)attr_info["name"], (string)attr_info["type"], null, null, null);
                new_attr.Set(JsonHelper.JToken2type(attr_info["value"], new_attr.type), false);
            }

            foreach (var comp_info in info["comp"])
            {
                Comp newComp;
                string type = (string)comp_info["type"];
                if (type.Length >= 8 && type.Substring(0, 8) == "Dropdown")
                    newComp = Instantiate(Manager.ins.compPrefabDict["Dropdown"], componentPanel).GetComponent<Comp>();
                else
                    newComp = Instantiate(Manager.ins.compPrefabDict[type], componentPanel).GetComponent<Comp>();
                if (!isDemo)
                    newComp.InitWithInfo(this, comp_info);
                comps.Add(newComp);
            }

            Attribute.Register(this, "color", "Vector3", (v) => { var w = (Vector3)v; SetColor(new Color(w.x, w.y, w.z)); }, history_in: "");


            foreach (var portInfo in info["portInfos"])
            {
                CreatePort(portInfo);
            }
        }

        protected virtual void CreatePort(JToken portInfo)
        {
            GameObject prefab;
            if ((string)portInfo["type"] == "ControlPort")
                prefab = (bool)portInfo["isInput"] ? Manager.ins.inControlPortPrefab : Manager.ins.outControlPortPrefab;
            else
                prefab = (bool)portInfo["isInput"] ? Manager.ins.inDataPortPrefab : Manager.ins.outDataPortPrefab;

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
                case "atr":
                    attributes[(string)message["name"]].Recieve(message); // Forward the message to the attribute
                    break;
                case "nat":
                    throw new System.Exception("don't use nat");

                case "npt":
                    CreatePort(message["info"]);
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
                Manager.ins.SendToServer(new API_update_message(id,"rmv",""));
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
                        Pos.Set(Manager.ins.GetSnappedPosition(desiredPosition));
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
            Output.Set((string)Output.Get()+ output,send:false);
        }
        public virtual void ClearOutput()
        {
            Output.Set("",send:true);
        }

        public virtual void Reshape(float w, float l, float r) { }//Trapezoid shaped node

        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }



        bool dragging = false;
        Vector3 desiredPosition;
 
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;

            if (isDemo) // Create a new node
            {
                //Node newNode = Instantiate(gameObject).GetComponent<Node>();newNode.id = Manager.ins.GetNewID();newNode.isDemo = false;
                info["id"] = Manager.ins.GetNewID();
                Node newNode = Manager.ins.CreateNode(info,true);
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
                transform.position = Manager.ins.GetSnappedPosition( CamControl.worldMouse);
                yield return null;
            }
            moveable = true;

            Pos = Attribute.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; }, history_in: "env",initValue:transform.position);
            
        }
        public virtual IEnumerator Removing()// SAO-like?
        {
            yield return null;
            Unselect();
            Manager.ins.Nodes.Remove(id);
            Destroy(gameObject);
        }


        public override void Select()
        {
            if (selected) return;
            base.Select();

            if(!isDemo)
                Manager.ins.nodeInspector.Open(this);

            selectColorTransition.Switch("selected");
            
        }
        public override void Unselect()
        {
            Manager.ins.nodeInspector.Clear();

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
    }
}
