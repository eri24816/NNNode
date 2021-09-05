using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GraphUI
{
    public class Port : MonoBehaviour, IDragHandler,IPointerEnterHandler,IPointerExitHandler
    {
        /*
            Child game object of a node.
            Handle edges connected to the node.
        */
        [System.Serializable]
        public struct API_new
        {
            public int id;
            public string type;
            public bool isInput;
            public int max_connections;
            public bool with_order;
            public string name;
            public string discription;
            public Vector3 pos;
        }
        public int id;
        public Node node;
        public List<Flow> Edges; // some type of port accepts multiple edges
        public int maxEdges;
        public bool isInput; // true: input false: output
        public bool with_order;
        public System.Type flowType;
        public TMPro.TMP_Text nameText;
        public float minDir, maxDir;
        [SerializeField]
        private List<GameObject> toHideOnMinimize;
        [SerializeField]
        GameObject knob;
        float pi = Mathf.PI;

        public void DisplayKnob()
        {
            knob.SetActive(true);
        }

        public void Init(Node node,Newtonsoft.Json.Linq.JToken info)
        {
            this.node = node;
            id = (int)info["id"];
            isInput = (bool)info["isInput"];
            maxEdges = (int)info["max_connections"];
            with_order = (bool)info["with_order"];
            

            RectTransform rt = (RectTransform)transform;
            float a, b, c, d;
            a = rt.anchorMin.x;
            b = 1 - rt.anchorMin.x;
            c = rt.anchorMin.y;
            d = 1 - rt.anchorMin.y;

            if (a < b && a < c && a < d)
            {

                minDir = pi;
                maxDir = pi;
            }
            else if (b < a && b < c && b < d)
            {
                minDir = 0;
                maxDir = 0;
            }
            else if (c < b && c < a && c < d)
            {
                minDir = pi * 3 / 2;
                maxDir = pi * 3 / 2;
            }
            else
            {
                minDir = pi / 2;
                maxDir = pi / 2;
            }
            node.SetupPort(this,info);
        }
        public Vector3 dirVec(float dir)
        {
            return new Vector3(Mathf.Cos(dir), Mathf.Sin(dir), 0);
        }
        public float Cosine(float a,float b)
        {
            return Vector3.Dot(dirVec(a), dirVec(b));
        }
        

        protected virtual void Start()
        {
            if (with_order)
            {
                minDir -= pi / 3;
                maxDir += pi / 3;
            }

        }
        public void SetExpanded(bool expanded) // currently only DataPort uses this
        {
            foreach (GameObject g in toHideOnMinimize)
                g.SetActive(expanded);
        }

        public void RecalculateEdgeDir(Flow addedFlow = null, int order = int.MaxValue)
        {
            int intervalCount = Edges.Count + 1;
            if (addedFlow != null) intervalCount += 1;
            float interval = (maxDir - minDir) / intervalCount;
            float currentDir = minDir;
            for(int i = 0; i < intervalCount-1; i++)
            {
                currentDir += interval;
                if (i < order) Edges[i].SetDir(!isInput, new Vector3(Mathf.Cos(currentDir), Mathf.Sin(currentDir), 0));
                else if (i == order) addedFlow.SetDir(!isInput, new Vector3(Mathf.Cos(currentDir), Mathf.Sin(currentDir), 0));
                else  Edges[i-1].SetDir(!isInput, new Vector3(Mathf.Cos(currentDir), Mathf.Sin(currentDir), 0));
            }
        } 

        public int GetNewEdgeOrder(Vector3 pos)
        {
            if (maxDir == minDir) return Edges.Count;

            var delta = pos - transform.position;
            delta.z = 0;
            float dir = Vector3.SignedAngle(Vector3.right, delta,Vector3.forward)*Mathf.Deg2Rad;
            var mid = (maxDir + minDir)/ 2;
            if (Mathf.Abs(dir + 2 * pi - mid) < Mathf.Abs(dir - mid)) dir += 2 * pi;
            else if (Mathf.Abs(dir - 2 * pi - mid) < Mathf.Abs(dir - mid)) dir -= 2 * pi;

            if (dir < minDir) return 0;
            if (dir >= maxDir) return Edges.Count;
            return (int)((dir-minDir)/(maxDir-minDir)*(1f+Edges.Count));
        }
         
        public virtual bool AcceptEdge(Flow flow)
        {
            if (node.isDemo) return false;
            return (flow.GetType() == flowType) && (Edges.Count < maxEdges) && (isInput ? flow.head == null : flow.tail == null);
        }
        public virtual void Remove() // Called when the node is going to be removed
        {

            foreach (Flow f in Edges)
            {
                if (isInput) f.tail.Disconnect(f);
                else f.head.Disconnect(f);
                Manager.ins.Flows.Remove(f.id);
                Destroy(f.gameObject);
            }
        }

        public void Disconnect(Flow flow)
        {
            Edges.Remove(flow);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (node.isDemo) return;
            if (Manager.ins.state == Manager.State.idle && Input.GetMouseButton(0))
            {
                if (Edges.Count < maxEdges)
                {
                    Flow newEdge = Instantiate(Manager.ins.nodePrefabDict[flowType.Name]).GetComponent<Flow>();
                    if (isInput) newEdge.head = this;
                    else newEdge.tail = this;
                    RecalculateEdgeDir(newEdge, GetNewEdgeOrder(CamControl.worldMouse));
                    Edges.Add(newEdge);
                    StartCoroutine(newEdge.Creating());
                    
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            CamControl.portHover = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (CamControl.portHover == this) CamControl.portHover = null;
        }
    }
}