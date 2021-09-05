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
    public class Node : Selectable, IEndDragHandler, IBeginDragHandler, IDragHandler, IUpdateMessageReciever
    {
        #region vars
        public List<Port> ports;
        public string id, Name;
        public Dictionary<string, NodeAttr> attributes;
        public Dictionary<string, Comp> components;
        NodeAttr Pos;
        protected string output = "";
        [SerializeField]  
        UnityEngine.UI.Outline outline_running;
        [SerializeField]
        List<UnityEngine.UI.Graphic> lights;
        [SerializeField] Transform componentPanel;

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
        public class API_update_message
        {
            public API_update_message(string id, string command, string info)
            {
                this.id = id; this.command = command; this.info = info;
            }
            public string command = "cod";
            public string id;
            public string info;
        }
        public class NodeAttr
        {
            struct API_atr<T> { public string id, command, name; public T value; }
            struct API_nat<T> { public string command, id, name, type, h;public T value; } // new attribute

            object value; // If delegate GetValue is not null, this field will not be used.
            public string type; // For inspector to know how to generate the attribute editor
            public readonly string name, nodeId;
            readonly CoolDown recvCD,sendCD;
            object recievedValue;

            // Delay recieving value after sending value
            readonly float delay = 1;

            // Like get set of property
            public delegate void SetDel(object value);
            public List<System.Tuple<object,SetDel>> setDel;
            public delegate object GetDel();
            public GetDel getDel;

            public static NodeAttr Register(Node node, string name, string type, SetDel setDel = null, GetDel getDel = null, object initValue = null, object comp = null, string history_in = "node")
            {
                if (comp == null) comp = 0;
                if (node.attributes.ContainsKey(name)) 
                {
                    NodeAttr attr = node.attributes[name];
                    if (setDel != null)
                    {
                        attr.setDel.Add(new System.Tuple<object, SetDel>(comp, setDel));
                        setDel(attr.Get());
                    }
                    if (getDel != null)
                       attr.getDel = getDel;

                    return attr;
                }
                else
                {
                    void SendNat <T>() => Manager.ins.SendToServer(new API_nat<T> { command = "nat", id = node.id, name = name, type = type, h = history_in, value = initValue == null?default(T): (T)initValue });
                    if (!node.isDemo)
                        switch (type)
                        {
                            case "string":
                                SendNat<string>(); break;
                            case "float":
                                SendNat<float>(); break;
                            case "Vector3":
                                SendNat<Vector3>(); break;
                        }

                    NodeAttr a = new NodeAttr(node, name, type, setDel, getDel, comp);
                    a.Set(initValue,false);
                    return a;
                }
            }
                

            public NodeAttr(Node node, string name,string type,SetDel setDel,GetDel getDel,object comp)
            {
                this.name = name; // Name format: category1/category2/.../attr_name
                this.type = type;
                nodeId = node.id;
                this.setDel=new List<System.Tuple<object, SetDel>>();
                if(setDel != null)
                    this.setDel.Add(new System.Tuple<object, SetDel>(comp, setDel));
                this.getDel=getDel;
                recvCD = new CoolDown(3);
                sendCD = new CoolDown(2); // Avoid client to upload too frequently e.g. upload the code everytime the user key in a letter.
                node.attributes.Add(name,this);
            }
            bool setLock = false;
            class SetLock : System.IDisposable
            {
                NodeAttr attr;
                public SetLock(NodeAttr attr) { attr.setLock = true;this.attr = attr; }
                public void Dispose()
                {
                    attr.setLock = false;
                }
            }
            public void Set(object value = null, bool send = true)
            {
                if (setLock) return;
                using (new SetLock(this)) // Avoid recursive Set() call
                {
                    if (value != null)
                        this.value = value;
                    else 
                        this.value = getDel();
                
                    var toBeRemoved = new List<System.Tuple<object, SetDel>>();
                    foreach (var i in setDel)
                    {
                        if (i.Item1 == null)
                        {
                            toBeRemoved.Add(i);
                        }
                    }
                    foreach (var i in toBeRemoved)
                        setDel.Remove(i);

                    foreach (var i in setDel)
                        i.Item2(this.value);
                    if (send) Send();
                }
            }
            public object Get()
            {
                if (getDel != null) return getDel();
                return value;
            }
            public void Recieve(JToken message)
            {
                recievedValue = JsonHelper.JToken2type(message["value"], type);
                recvCD.Request();
            } 
            public void Send()
            {
                sendCD.Request();
                recvCD.Delay(delay);
            }

            // Call this constantly
            public void Update()
            {
                if (recvCD.Update())
                {
                    Set(recievedValue, false);
                }
                if (sendCD.Update())
                {
                    if (type == "Vector3")
                        Send<Vector3>();
                    else if (type == "float")
                        Send<float>();
                    else if (type == "string"||(type.Length >= 8 && type.Substring(0, 8) == "dropdown"))
                        Send<string>();
                }
            }
            public void Send<T>()
            {
                Manager.ins.SendToServer(new API_atr<T> { id = nodeId, command = "atr", name = name, value = (T)Get() });
            }

        }
        public bool createByThisClient = false;
        public virtual void Init(JToken info)
        {
            /*
             * Parse the node info to setup the node.
             * 
            */


            type = (string)info["type"];
            name = Name = (string)info["name"];
            id = (string)info["id"];

            attributes = new Dictionary<string, NodeAttr>();
            components = new Dictionary<string, Comp>(); 

            isDemo = id == "-1";
            if(id != "-1")
            {
                transform.localScale = Vector3.one * 0.002f;
                Manager.ins.Nodes.Add(id,this);
            }
            else
            {
                this.info = info;
                transform.localScale = Vector3.one * 0.7f;
                Manager.ins.DemoNodes.Add(type, this);
                transform.SetParent( Manager.ins.FindCategoryPanel((string)info["category"],Manager.ins.demoNodeContainer,Manager.ins.categoryPanelPrefab));
            }

            if (createByThisClient)
                Manager.ins.SendToServer(new API_new(this));
            else
                // If createByThisClient, set Pos attribute after the node is dropped to its initial position (in OnDragCreating()).
                Pos = NodeAttr.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; },history_in : "env");
            
            foreach (var attr_info in info["attr"])
            {
                var new_attr = new NodeAttr(this, (string)attr_info["name"], (string)attr_info["type"], null, null, null);
                new_attr.Set(JsonHelper.JToken2type((string)attr_info["value"],new_attr.type), false);
            }

            foreach (var comp_info in info["comp"])
            {
                Comp newComp;
                string type = (string)comp_info["type"];
                if (type.Length>=8 && type.Substring(0, 8) == "Dropdown")
                    newComp = Instantiate(Manager.ins.compPrefabDict["Dropdown"], componentPanel).GetComponent<Comp>();
                else
                    newComp = Instantiate(Manager.ins.compPrefabDict[type], componentPanel).GetComponent<Comp>();
                if (!isDemo)
                    newComp.Init(this, comp_info);
                components.Add(newComp.name, newComp);
            }

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
            outline_running.effectColor = new Color(0, 0, 0, 0);
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
                case "rmv":
                    StartCoroutine(Removing());
                    break;
                    
            }
        }

        public override void Remove()
        {
            // Remove node from this client
            // Invoked by X button
            base.Remove();
            foreach (Port port in ports)
                port.Remove();
            Manager.ins.SendToServer(new API_update_message(id,"rmv",""));
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
                        Pos.Set(transform.position + CamControl.worldMouseDelta);
                    }
                } 

            foreach(var attr in attributes)
            {
                attr.Value.Update();
            }
        }

        public virtual void AddOutput(string output)
        {
            this.output += output;
        }
        public virtual void ClearOutput()
        {
            output = "";
        }

        public virtual void Reshape(float w, float l, float r) { }//Trapezoid shaped node

        protected override void OnDoubleClick()
        {
            base.OnDoubleClick();
            Manager.ins.Activate(this);
        }



        bool dragging = false;

 
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
                foreach (Selectable s in current)
                    if (s is Node node)
                    {
                        node.dragging = true;
                        dragged = true; // Prevent Selectable from selecting
                    }
            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!moveable) return;
            if (eventData.button != 0) return;
            foreach (Selectable s in current)
                if (s is Node node)
                {
                    node.dragging = false;
                }
        }
        public void OnDrag(PointerEventData eventData) { }

        public virtual IEnumerator DragCreating()//Drag and drop
        {
            moveable = false;
            
            
            while (Input.GetMouseButton(0))
            {
                transform.position = CamControl.worldMouse;
                yield return null;
            }
            moveable = true;

            Pos = NodeAttr.Register(this, "transform/pos", "Vector3", (v) => { transform.position = (Vector3)v; }, () => { return transform.position; }, history_in: "env",initValue:transform.position);
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
            Manager.ins.nodeInspector.Close();

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

    }
}
