using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WebSocketSharp; 
using GraphUI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ObjectSync;

public static class JsonHelper
{
    public static object JToken2type(JToken j, string type)
    {
        if (type.Length>=8 && type.Substring(0, 8) == "dropdown") type = "string";
        return type switch
        {
            "string" => (string)j,
            "float" => (float)j,
            "Vector3" => j.ToObject<Vector3>(),
            "bool" => (bool)j,
            _ => throw new System.Exception($"Type {type} not supported"),
        };
    } 
}

public class SpaceClient : MonoBehaviour, ObjectSync.ISpaceClient
{
    public ObjectSync.Space space;

    public static SpaceClient ins;

    public Dictionary<string, Node> Nodes;
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

        space.SendMessage(new ObjectSync.API.Out.Create { parent = "0", d = {type="TestNode1" } }) ;
    }
    private void OnDestroy()
    {
        //space.Close();
    }
    private void LateUpdate()
    {
        space.Update();
    }

    public float snap = 0.02f;


    public void RecieveMessage(JToken message)
    {
        string command = (string)message["command"];
        print(message.ToString());
    }

    public IObjectClient CreateObjectClient(JToken message)
    {
        return theme.Create((string)message["d"]["type"]).GetComponent<IObjectClient>();
    }

    public object ConvertJsonToType(JToken j, string type)
    {
        return type switch
        {
            "Vector3" => j.ToObject<Vector3>(),
            _ => throw new System.Exception($"Type {type} not supported"),
        };
    }

    public Vector3 GetSnappedPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x / snap) * snap, Mathf.Round(pos.y / snap) * snap, pos.z);
    }
}
