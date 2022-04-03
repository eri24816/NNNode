using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphUI
{
    public abstract class ObjectClient : MonoBehaviour, ObjectSync.IObjectClient, IScrollHandler
    {
        protected SpaceClient spaceClient;

        protected ObjectSync.Object syncObject;
        protected ObjectSync.Attribute<Vector3> Pos;
        protected ObjectSync.Attribute<string> Output, ParentID;

        protected MonoBehaviour sendOnScrollTo;
        public virtual void OnCreate(JToken d, ObjectSync.Object obj)
        {
            syncObject = obj;
            spaceClient = obj.space.spaceClient as SpaceClient;
            spaceClient.objs.Add(syncObject.id,gameObject);

            if (syncObject.id == "0")
            {
                transform.SetParent( spaceClient.transform);
                transform.position = Vector3.zero;
            }

            ParentID = syncObject.RegisterAttribute<string>("parent_id", OnParentChanged, "none");
            transform.localScale = Vector3.one;

            Pos = syncObject.RegisterAttribute<Vector3>("transform/pos", (v) => { transform.localPosition = v;}, "parent", Vector3.zero);  
            Output = syncObject.RegisterAttribute<string>("output", (v) => { OnOutputChanged(v); }, "none");

            
        }
        public virtual void OnDestroy_(JToken message)
        {
            Destroy(gameObject);
        }

        public virtual void RecieveMessage(JToken message)
        {
            switch ((string)message["command"])
            {
                case "rmv":
                    Destroy(gameObject);
                    break;
            }
        }

        public virtual void OnParentChanged(string parent_id)
        {
            if (syncObject.id == "0") return;
            transform.SetParent(spaceClient.objs[parent_id].transform,true);
        }

        protected virtual void OnOutputChanged(string output)
        {

        }
        public void OnScroll(PointerEventData eventData)
        {
            // Send scroll event ahead to the background, instead of blocking it.
            if (sendOnScrollTo)
                sendOnScrollTo.SendMessage("OnScroll", eventData);
        }

        public void Undo()
        {
            syncObject.SendMessage("{\"command\":\"undo\",\"id\":\"" + syncObject.id + "\"}");
        }
        public void Redo()
        {
            syncObject.SendMessage("{\"command\":\"redo\",\"id\":\"" + syncObject.id + "\"}");
        }
    }
}