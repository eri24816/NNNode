using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI
{
    public class Flow : Selectable
    {
        public class API_new
        {
            public string command = "new";
            public Info info;
            [System.Serializable]
            public struct Info
            {
                public string id;
                public string type;
                public string head;
                public string tail;
                public int head_port_id;
                public int tail_port_id;
            }
            
            public API_new(Flow flow)
            {
                info.id = flow.id; info.type = flow.GetType().Name; info.head = flow.head.node.id; info.tail = flow.tail.node.id;info.head_port_id =
                    flow.head.id;info.tail_port_id = flow.tail.id;
            }
        }
        public class API_update_message
        {
            public API_update_message(string id, string command, string info)
            {
                this.id = id; this.command = command; this.info = info;
            }
            public string command;
            public string id;
            public string info;
        }

        public string id;
        public Port tail, head;
        public Line line;
        public bool hard = false;

        public ColorTransition runColorTransition,selectColorTransition;
        [SerializeField]
        Material material;


        protected virtual void Start()
        {
            transform.SetParent(SpaceClient.ins.canvasTransform);
            line = GetComponent<Line>();
            if(tail)
                tail.RecalculateEdgeDir();
            if(head) 
                head.RecalculateEdgeDir();
            var graphic = GetComponent<UnityEngine.UI.Graphic>();
            material = graphic.material = Instantiate(graphic.material) ;
            sendOnScrollTo = CamControl.ins;
        }
        protected virtual void Update()
        {
            if (tail && head)
            {
                
                line.Tail = tail.transform.position;
                line.Head = head.transform.position;
            }
            material.SetFloat("_Intensity", runColorTransition.color.r);
        }

        public virtual void RecieveUpdateMessage(Newtonsoft.Json.Linq.JToken message)
        {
            switch ((string)message["command"])
            {

                case "act":
                    switch ((string)message["info"])
                    {
                        case "0": DisplayInactive(); break;
                        case "2": DisplayActive(); break;
                    }
                    break;
                case "rmv":
                    RawRemove();
                    SpaceClient.ins.Flows.Remove(id);
                    break;
            }
        }



        public void SetDir(bool isTail,Vector3 dir)
        {
            if (isTail) line.Tail_dir = dir;
            else line.Head_dir = dir;
        }

        public IEnumerator Creating()
        {
            SpaceClient.ins.state = SpaceClient.State.draggingFlow;
            line.raycastTarget = false;
            yield return null; // wait for next frame
            if (tail && head)
            {
                SpaceClient.ins.state = SpaceClient.State.idle;
                yield break;
            }
            bool dragTail = head;

            Port targetPort = null, p_targetPort = null;
            while (Input.GetMouseButton(0)) // while mouse hold
            {
                if(p_targetPort != targetPort && p_targetPort != null)
                {
                    p_targetPort.RecalculateEdgeDir();
                }
                p_targetPort = targetPort;
                //targetPort = null;
                //if (CamControl.colliderHover)
                    //targetPort = CamControl.colliderHover.GetComponent<Port>();
                targetPort = CamControl.portHover;

                if (targetPort)
                    if (!targetPort.AcceptEdge(this)) targetPort = null;
                if (targetPort)
                    targetPort.RecalculateEdgeDir(this, targetPort.GetNewEdgeOrder(CamControl.worldMouse));
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

                targetPort.Edges.Insert(targetPort.GetNewEdgeOrder(CamControl.worldMouse),this);
                id = SpaceClient.ins.GetNewID();
                SpaceClient.ins.SendToServer(new API_new(this));
                SpaceClient.ins.Flows.Add(id, this);
                SpaceClient.ins.state = SpaceClient.State.idle;
                targetPort.RecalculateEdgeDir();
                line.raycastTarget = true;
                yield break;
            }
            else
            {
                RawRemove();
            }
            SpaceClient.ins.state = SpaceClient.State.idle;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            SpaceClient.ins.SendToServer(new API_update_message(id,"rmv",""));
            RawRemove();
        }
        public void RawRemove()
        {
            if (tail) { tail.Disconnect(this); tail.RecalculateEdgeDir(); }
            if (head) { head.Disconnect(this); head.RecalculateEdgeDir(); }
            SpaceClient.ins.Flows.Remove(id);

            Destroy(gameObject);
        }


        IEnumerator changeColor;
        public override void Select()
        {
            if (selected) return;
            base.Select();

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
    }
}