using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectClient : MonoBehaviour, ObjectSync.IObjectClient, IScrollHandler
{
    protected SpaceClient spaceClient;

    public ObjectSync.Object syncObject;
    protected ObjectSync.Attribute<string> Output, ParentID;

    protected MonoBehaviour sendOnScrollTo;

    public string specifyParentName = null;
    public Transform specifyParent = null;
    public Transform specifyChildContainer = null;
    public virtual void OnCreate(JToken d, ObjectSync.Object obj)
    {
        syncObject = obj;
        spaceClient = obj.space.spaceClient as SpaceClient;
        spaceClient.objs.Add(syncObject.id,this);

        if(specifyParentName!="")specifyParent = GameObject.Find(specifyParentName).transform;

        if (syncObject.id == "0")
        {
            //transform.SetParent( spaceClient.transform);
            transform.position = Vector3.zero;
        }
        if(specifyParent)
            transform.SetParent(specifyParent, true);
        else
            ParentID = syncObject.RegisterAttribute<string>("parent_id", OnParentChanged, "none");
        transform.localScale = Vector3.one;


        Output = syncObject.RegisterAttribute<string>("output", (v) => { OnOutputChanged(v); }, "none");

    }
    public virtual void OnDestroy_(JToken message)
    {
        print($"destroying {syncObject.id}");
        spaceClient.objs.Remove(syncObject.id);
        print($"destroy{spaceClient.objs.Count}");

        Destroy(gameObject);
    }

    public virtual void RecieveMessage(JToken message)
    {
        /*
        switch ((string)message["command"])
        {

        }
        */
    }

    public virtual void OnParentChanged(string parent_id)
    {
        if (syncObject.id == "0") return;
        if (specifyParent) return;
        Transform parentTransform;
        if (spaceClient.objs[parent_id].specifyChildContainer)
            parentTransform = spaceClient.objs[parent_id].specifyChildContainer;
        else
            parentTransform = spaceClient.objs[parent_id].transform;
        transform.SetParent(parentTransform, true);
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
        syncObject.Undo();
    }
    public void Redo()
    {
        syncObject.Redo();
    }
}
