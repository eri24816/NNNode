using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp; 
using GraphUI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ObjectSync;


public class SpaceClient : MonoBehaviour, ObjectSync.ISpaceClient
{
    public ObjectSync.Space space;

    public static SpaceClient ins;


    //public Dictionary<string, Flow> Flows;
    public Dictionary<string, Node> DemoNodes;

    public GameObject[] prefabs;
    public Dictionary<string, GameObject> PrefabDict;

    public GameObject inDataPortPrefab, outDataPortPrefab,inControlPortPrefab,outControlPortPrefab;

    public Transform canvasTransform;
    public Hierachy demoNodeContainer;
    //public Inspector nodeInspector;
    public GameObject categoryPanelPrefab;
    public Theme theme;

    public Dictionary<string,ObjectClient> objs = new Dictionary<string, ObjectClient>();
    
    
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

        StartCoroutine(C());
    }
    public IEnumerator C()
    {
        yield return null;
        space.SendMessage(new ObjectSync.API.Out.Create
        {
            parent = "0",
            d = {
                type="TestNode1" ,
                attributes = new List<ObjectSync.API.Out.Create.Attribute>{
                    new ObjectSync.API.Out.Create.Attribute{name="transform/pos",value=new Vector3(0,0,-1),history_object = "parent"}
                }
            }
        }); ;
        yield break;
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
        return objectClient;
    }

    public Vector3 GetSnappedPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x / snap) * snap, Mathf.Round(pos.y / snap) * snap, pos.z);
    }
}
