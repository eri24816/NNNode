using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GraphUI
{
    public class Port : MonoBehaviour, IDragHandler
    {
        /*
            Child game object of a node.
            Handle edges connected to the node.
        */

        public Node node;
        public List<Flow> Edges; // some type of port accepts multiple edges
        public int maxEdges;
        public bool isInput; // true: input false: output
        public System.Type flowType;
        [SerializeField]
        private List<GameObject> toHideOnMinimize;


        protected virtual void Start()
        {
            //node = transform.parent.GetComponent<Node>();
        }
        public void SetExpanded(bool expanded) // currently only DataPort uses this
        {
            foreach (GameObject g in toHideOnMinimize)
                g.SetActive(expanded);
        }

        protected virtual void Update()
        {

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
                    Flow newEdge = Instantiate(Manager.ins.prefabDict[flowType.Name]).GetComponent<Flow>();
                    if (isInput) newEdge.head = this;
                    else newEdge.tail = this;
                    Edges.Add(newEdge);
                    StartCoroutine(newEdge.Creating());
                }
            }
        }
    }
}