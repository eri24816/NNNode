using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp; 
using NNNode;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ObjectSync;


public class SpaceClient : MonoBehaviour, ISpaceClient
{
    public ObjectSync.Space space;

    public static SpaceClient ins;


    //public Dictionary<string, Flow> Flows;
    public Dictionary<string, Node> DemoNodes;

    public GameObject[] prefabs;
    public Dictionary<string, GameObject> PrefabDict;

    public GameObject inDataPortPrefab, outDataPortPrefab,inControlPortPrefab,outControlPortPrefab;

    public Transform canvasTransform;
    //public Inspector nodeInspector;
    public GameObject categoryPanelPrefab;
    public Theme theme;

    public Dictionary<string,ObjectClient> objs = new();
    public ObjectClient Root { get { return objs["0"]; } }
    public ObjectClient this[string index]
    {
        get { return objs[index]; }
    }
    public enum State
    {
        idle,  
        draggingFlow
    }
    public State state;
    readonly string WSPath = "ws://localhost:1000/";
    readonly string spaceName = "my_space";
    private void Start()
    {
        Application.targetFrameRate = 60;

        // Connect to lobby and open a 
        WebSocket lobbyWS = new WebSocket(WSPath + "lobby");
        lobbyWS.Connect();
        lobbyWS.Send("stt " + spaceName);
        lobbyWS.Close(CloseStatusCode.Normal, "disconnect");

        space = new ObjectSync.Space(this, WSPath + "space" + "/" + spaceName);

        PrefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in prefabs)
            PrefabDict.Add(prefab.name, prefab);

        ins = this;
    }

    private void LateUpdate()
    {
        space.Update();
    }

    public float snap = 0.02f;
    public void RecieveMessage(JToken message)
    {
        string command = (string)message["command"];
    }


    public IObjectClient CreateObjectClient(JToken d)
    {
        GameObject newObject = theme.Create((string)d["frontend_type"]);
        ObjectClient objectClient = newObject.GetComponent<ObjectClient>();
        objectClient.name = $"{d["type"]} #{d["id"]} ({d["frontend_type"]})";
        return objectClient;
    }

    /*
     * Wait for an Objet with a specific tag to be create, then invoke a callback. 
     * Example usage:
     * creationWaiter.Add(new("sometag", (o) => {print($"{o} created.")}));
     */
    public List<System.Tuple<string, System.Action<ObjectClient>>> creationWaiter = new();

    public void OnObjectCreated(ObjectSync.Object obj)
    {
        foreach(var p in creationWaiter.ToArray())
        {
            if (obj.TaggedAs(p.Item1))
            {
                p.Item2((ObjectClient)obj.objectClient);
                creationWaiter.Remove(p);
            }
        }
    }

    public Vector3 GetSnappedPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x / snap) * snap, Mathf.Round(pos.y / snap) * snap, pos.z);
    }


}
