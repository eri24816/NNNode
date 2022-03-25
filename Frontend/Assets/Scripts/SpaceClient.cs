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
    public Dictionary<string, Flow> Flows;
    public Dictionary<string, Node> DemoNodes;

    public GameObject[] nodePrefabs;
    public Dictionary<string, GameObject> nodePrefabDict;

    public GameObject inDataPortPrefab, outDataPortPrefab,inControlPortPrefab,outControlPortPrefab;

    public Transform canvasTransform;
    public Hierachy demoNodeContainer;
    public Inspector nodeInspector;
    public GameObject categoryPanelPrefab;
    
    
    public enum State
    {
        idle,  
        draggingFlow
    }
    public State state;
    readonly string WSPath = "ws://localhost:1000/";
    readonly string spaceName = "my_space";
    void Start()
    {
        Application.targetFrameRate = 60;

        // Connect to lobby and open a 
        WebSocket lobbyWS = new WebSocket(WSPath + "lobby");
        lobbyWS.Connect();
        lobbyWS.Send("stt " + spaceName);
        lobbyWS.Close();

        space = new ObjectSync.Space(this, WSPath + "space" + "/" + spaceName);

        DemoNodes = new Dictionary<string, Node>();
        nodePrefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in nodePrefabs)
            nodePrefabDict.Add(prefab.name, prefab);

        ins = this;
    
    }



    private void Update()
    {
        space.Update();
    }
    public Node CreateNode(JToken info,bool createByThisClient = false)
    {
        if (Nodes.ContainsKey((string)info["id"]))return null;/*
        GameObject prefab = nodePrefabDict[message.info.shape+"Node"];
        var node = Instantiate(prefab).GetComponent<Node>();*/
        var node = Theme.ins.Create((string)info["shape"] + "Node").GetComponent<Node>(); ;
        node.createByThisClient = createByThisClient;
        node.Init(info);
        return node;
    }

    public float snap = 0.02f;
    public Vector3 GetSnappedPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round( pos.x/snap)*snap, Mathf.Round(pos.y / snap) * snap, pos.z);
    }

    public void RecieveMessage(JToken message)
    {
        string command = (string)message["command"];
    }

    public IObjectClient CreateObjectClient(JToken message)
    {
        throw new System.NotImplementedException();
    }

    IObjectClient ISpaceClient.CreateObjectClient(JToken message)
    {
        throw new System.NotImplementedException();
    }

    public object ConvertJsonToType(JToken j, string type)
    {
        return type switch
        {
            "Vector3" => j.ToObject<Vector3>(),
            _ => throw new System.Exception($"Type {type} not supported"),
        };
    }
}
