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
        public List<Port> ports;
        public string id, Name;
        public Dictionary<string, INodeAttr> attributes;
        public Dictionary<string, Comp> components;
        NodeAttr<Vector3> Pos;
        protected string output = "";
        [SerializeField]  
        UnityEngine.UI.Outline outline_running;
        [SerializeField]
        Color unselectedColor, hoverColor, selectedColor,outlineRunningColor,outlinePendingColor;
        [SerializeField]
        List<UnityEngine.UI.Graphic> lights;
        [SerializeField] Transform componentPanel;

        public string type; // class name in python
        public bool isDemo;
        bool moveable = true;

        string newCommandJson;

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

        public struct API_atr { public string name; }
        
        public interface INodeAttr { public void Recieve(string Json); public void Update(); }
        public class NodeAttr<dataType>: INodeAttr
        {
            struct API_atr { public string id, command, name; public dataType value; }

            dataType value; // If delegate GetValue is not null, this field will not be used.
            readonly string name, nodeId;
            readonly CoolDown recvCD,sendCD;
            dataType recievedValue;

            // Delay recieving value after sending value
            readonly float delay = 1;

            // Like get set of a property
            public delegate void SetDel(dataType value);
            SetDel setDel;
            public delegate dataType GetDel();
            GetDel getDel;

            public NodeAttr(Node node, string name,SetDel setDel = null,GetDel getDel = null)
            {
                this.name = name;
                nodeId = node.id;
                this.setDel = setDel;
                this.getDel = getDel;
                recvCD = new CoolDown(3);
                sendCD = new CoolDown(2); // Avoid client to upload too frequently e.g. upload the code everytime the user key in a letter.
                node.attributes.Add(name,this);
            }
            public void Set(dataType value, bool send = true)
            {
                if(getDel == null)
                    this.value = value;
                setDel?.Invoke(value);
                if (send) Send();
            }
            public dataType Get()
            {
                if (getDel != null) return getDel();
                return value;
            }
            public void Recieve(string Json)
            {
                recievedValue = JsonUtility.FromJson<API_atr>(Json).value;
                print("recievedValue: " + recievedValue);
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
                    Manager.ins.SendToServer(new API_atr { id = nodeId, command = "atr", name = name, value = Get() });
                }
            }
        }
         
        public virtual void Init(string infoJSON,string id_ = null)
        {
            /*
             Though infoJson already contains id, but when cloning from a demo node, I am too lazy to change the id in json. So in that case
             id is passed as an argument.
            */

            API_new.Info info  = JsonUtility.FromJson<API_new>(infoJSON).info;

            type = info.type;
            name = Name = info.name;
            id = info.id;
            if (id_ != null) id = id_;

            attributes = new Dictionary<string, INodeAttr>();
            components = new Dictionary<string, Comp>();
            Pos = new NodeAttr<Vector3>(this, "pos", (v) => { transform.position = v; }, () => { return transform.position; });
            foreach(Comp.API_new comp_info in info.comp)
            {
                Comp newComp = Instantiate(Manager.ins.compPrefabDict[comp_info.type], componentPanel).GetComponent<Comp>();
                newComp.Init(this, comp_info);
                components.Add(comp_info.name, newComp);
            }

            isDemo = id == "-1";
            if(id != "-1")
            {
                transform.localScale = Vector3.one * 0.002f;
                Manager.ins.Nodes.Add(id,this);
            }
            else
            {
                newCommandJson = infoJSON;
                transform.localScale = Vector3.one * 0.7f;
                Manager.ins.DemoNodes.Add(type, this);
                transform.SetParent( Manager.ins.FindCategoryPanel(info.category));
            }
            int i = 0;
            foreach (var portInfo in info.portInfos)
            {

                GameObject prefab;
                if (portInfo.type == "ControlPort")
                    prefab = portInfo.isInput ? Manager.ins.inControlPortPrefab : Manager.ins.outControlPortPrefab;
                else
                    prefab = portInfo.isInput ? Manager.ins.inDataPortPrefab : Manager.ins.outDataPortPrefab;

                Port newPort = Instantiate(prefab, transform).GetComponent<Port>();
                ports.Add(newPort);
                ((RectTransform)newPort.transform).anchorMin = ((RectTransform)newPort.transform).anchorMax = new Vector2(portInfo.pos.x / 2 + .5f, portInfo.pos.y / 2 + .5f);
                newPort.port_id = i;
                newPort.Init(this, portInfo);
                i++;
                }
                
        }

        public virtual void Start()
        {
            targetPos = transform.position;
            outline_running.effectColor = new Color(0, 0, 0, 0);
        }

        public virtual void RecieveUpdateMessage(string message,string command){
            switch (command)
            {
                case "clr":
                    ClearOutput();
                    break;

                case "out":
                    AddOutput(JsonUtility.FromJson<API_update_message>(message).info);
                    break;

                case "act":
                    switch (JsonUtility.FromJson<API_update_message>(message).info)
                    {
                        case "0": DisplayInactivate(); break;
                        case "1": DisplayPending(); break;
                        case "2": DisplayActivate(); break;
                    }
                    break;
                case "atr":
                    attributes[JsonUtility.FromJson<API_atr>(message).name].Recieve(message);
                    break;
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


        Vector3 targetPos;

        bool dragging = false;


        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != 0) return;

            if (isDemo) // Create a new node
            {
                //Node newNode = Instantiate(gameObject).GetComponent<Node>();newNode.id = Manager.ins.GetNewID();newNode.isDemo = false;
                Node newNode = Manager.ins.CreateNode(newCommandJson, Manager.ins.GetNewID());
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
            //Manager.ins.Nodes.Add(id, this);
            Manager.ins.SendToServer(new API_new(this));
            Pos.Send();
        }
        public virtual IEnumerator Removing()// SAO-like?
        {
            yield return null;
            Manager.ins.Nodes.Remove(id);
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
