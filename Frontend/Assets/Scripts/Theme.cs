using UnityEngine;
using GraphUI;
using System.Collections.Generic;
using UnityEngine.UI;

//[ExecuteAlways]
public class Theme : MonoBehaviour
{
    public static Theme ins;

    Color _main, _secondary;

    Color gray = new Color(.5f, .5f, .5f);
    public List<GameObject> prefabs;
    Dictionary<string, GameObject> prefabDict;
    struct TypeObjectPair
    {
        public string type;
        public GameObject obj;
        public TypeObjectPair(string type, GameObject obj) { this.type = type; this.obj = obj; }
    }
    List<TypeObjectPair> instantiated = new List<TypeObjectPair>();

    [SerializeField] Color mainForInspector, secondaryForInspector;
    [SerializeField] float bgBriteness;

    [SerializeField] GameObject bg;
    [SerializeField] List<Image> buttons;


    private void OnValidate()
    {
        CheckColorChange();
    }
    private void Update()
    {
        CheckColorChange();
    }
    void CheckColorChange()
    {
        if (mainForInspector != _main)
        {
            _main = mainForInspector;
            ResetAll();
        }
        if (secondaryForInspector != _secondary)
        {
            _secondary = secondaryForInspector;
            ResetAll();
        }
    }

    public void Start()
    {
        ins = this;
        prefabDict = new Dictionary<string, GameObject>();
        foreach (GameObject g in prefabs)
            prefabDict.Add(g.name, g);
        instantiated = new List<TypeObjectPair>();
    }

    public GameObject Create(string prefabName)
    {
        GameObject newObject = Instantiate(prefabDict[prefabName]);
        Setup(newObject, prefabName);
        instantiated.Add(new TypeObjectPair(prefabName, newObject));
        return newObject;
    }

    public void ResetAll()
    {
        List<TypeObjectPair> toDelete = new List<TypeObjectPair>();
        foreach (var p in instantiated)
        {
            if (p.obj)
                Setup(p.obj, p.type);
            else { toDelete.Add(p); }
        }
        foreach (var p in toDelete) instantiated.Remove(p);
        bg.GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", C1(8) * bgBriteness);
        foreach (var b in buttons) b.color = C1(10, 5);
    }

    public void Setup(GameObject o, string prefabName)
    { 
        switch (prefabName)
        {
            case "GeneralNode":
            case "SimpleNode":
            case "RoundNode":
            case "VerticalSimpleNode":
                Node node = o.GetComponent<Node>();
                
                node.selectColorTransition.SetColor("selected", C2(8));
                node.selectColorTransition.SetColor("unselected", C0(0,0));
                node.selectColorTransition.SetColor("hover", C2(5,5));
                    node.selectColorTransition.SetDefault("unselected");
                

                node.runColorTransition.SetColor("pending", C2(1));
                node.runColorTransition.SetColor("active", C2(5));
                node.runColorTransition.SetColor("inactive", C0(1));
                node.runColorTransition.SetDefault("inactive"); // TODO : Maybe it is running 

                //node.SetColor(C1(10));

                break;
            case "DataFlow":
                Flow flow = o.GetComponent<Flow>();
                flow.selectColorTransition.SetColor("selected", C1(10));
                flow.selectColorTransition.SetColor("unselected", C0(7,4));
                flow.selectColorTransition.SetColor("hover", C0(8,6));
                flow.selectColorTransition.SetDefault("unselected");

                flow.runColorTransition.SetColor("active", C0(10));
                flow.runColorTransition.SetColor("inactive", C0(0));
                flow.runColorTransition.SetDefault("inactive");

                break;
            case "CategoryPanel":
            case "CategoryPanelForNodeList":
                o.GetComponentInChildren<RawImage>().color = C1(7,7);
                break;
        }
    }

    Color C1(float t, float a = 10)
    {
        var c = Color.Lerp(gray, _main, t / 10);
        c.a = a / 10;
        return c;
    }
    Color C2(float t, float a = 10)
    {
        var c = Color.Lerp(gray, _secondary, t / 10);
        c.a = a / 10;
        return c;
    }
    Color C0(float t, float a = 10)
    {
        return new Color(t / 10, t / 10, t / 10, a / 10);
    }
}

