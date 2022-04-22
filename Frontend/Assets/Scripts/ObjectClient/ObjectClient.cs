using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectClient : MonoBehaviour, ObjectSync.IObjectClient
{
    protected SpaceClient spaceClient;

    public ObjectSync.Object syncObject;
    protected ObjectSync.Attribute<string> Output, ParentID;

    protected MonoBehaviour sendOnScrollTo;

    public Transform specifyChildContainer = null;

    public string category;
    public virtual void OnCreate(JToken d, ObjectSync.Object obj)
    {
        syncObject = obj;
        spaceClient = obj.space.spaceClient as SpaceClient;
        spaceClient.objs.Add(syncObject.id,this);

        if (d["category"] != null)
            category = (string)d["category"];

        if (syncObject.id == "0")
        {
            //transform.SetParent( spaceClient.transform);
            transform.position = Vector3.zero;
        }

        if (syncObject.Attributes.ContainsKey("parent_object"))
        {
            transform.SetParent(GameObject.Find(((ObjectSync.Attribute<string>)syncObject.Attributes["parent_object"]).Value).transform, true);
        }

        else
            ParentID = syncObject.RegisterAttribute<string>("parent_id", OnParentChanged, "none");
        transform.localScale = Vector3.one;


        Output = syncObject.RegisterAttribute<string>("output", (v) => { OnOutputChanged(v); }, "none");

    }
    public virtual void OnDestroy_(JToken message)
    {
        print($"destroying {syncObject.id}");
        spaceClient.objs.Remove(syncObject.id);

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
        Transform parentTransform;
        if (spaceClient[parent_id].specifyChildContainer)
            parentTransform = spaceClient.objs[parent_id].specifyChildContainer;
        else
            parentTransform = spaceClient.objs[parent_id].transform;
        transform.SetParent(parentTransform, true);
        spaceClient[parent_id].OnAddChild(this);
    }
    public virtual void OnAddChild(ObjectClient child)
    {

    }

    protected virtual void OnOutputChanged(string output)
    {

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
