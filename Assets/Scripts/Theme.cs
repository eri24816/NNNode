using UnityEngine;
using GraphUI;
using System.Collections.Generic;

//[ExecuteAlways]
public class Theme : MonoBehaviour
{
    public static Theme ins;
    
    Color _main, _secondary;

    Color gray = new Color(.5f, .5f, .5f);
    public List<GameObject> prefabs;
    Dictionary<string, GameObject> prefabDict;

    [SerializeField] Color mainForInspector, secondaryForInspector;
    [SerializeField] float bgBriteness;

    [SerializeField] GameObject bg;


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
        if(mainForInspector != _main)
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
        foreach(GameObject g in prefabs)
            prefabDict.Add(g.name, g);
    }

    public GameObject Create(string prefabName)
    {
        GameObject newObject = Instantiate(prefabDict[prefabName]);
        Setup(newObject,prefabName);
        return newObject;
    }

    public void ResetAll()
    {
        foreach(var p in FindObjectsOfType<Node>())
        {
            Setup(p.gameObject,p.GetType().Name);
        }
        bg.GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", C1(8)*bgBriteness);
    }

    public void Setup(GameObject o, string prefabName)
    {
        switch (prefabName)
        {
            case "GeneralNode":
            case "SimpleNode":
            case "RoundNode":
                Node node = o.GetComponent<Node>();
                node.selectColorTransition.AddColor("selected", C1(10));
                node.selectColorTransition.AddColor("unselected", C0(0));
                node.selectColorTransition.AddColor("hover", C0(3));
                if (Application.isEditor)
                    node.selectColorTransition.SetDefault("selected");
                else
                    node.selectColorTransition.SetDefault("unselected");

                node.runColorTransition.AddColor("pending", C2(1));
                node.runColorTransition.AddColor("active", C2(5));
                node.runColorTransition.AddColor("inactive", C0(1));
                node.runColorTransition.SetDefault("inactive");

                node.SetMainColor(C1(10));
                
                break;
            case "DataFlow":
                Flow flow = o.GetComponent<Flow>();
                flow.selectColorTransition.AddColor("selected", C1(1));
                flow.selectColorTransition.AddColor("unselected", C0(7));
                flow.selectColorTransition.AddColor("hover", C0(8));
                flow.selectColorTransition.SetDefault("unselected");

                flow.runColorTransition.AddColor("active", C2(10));
                flow.runColorTransition.AddColor("inactive", C0(0));
                flow.runColorTransition.SetDefault("inactive");

                break;
        }
    }

    Color C1(float t)
    {
        return Color.Lerp(gray, _main, t / 10);
    }
    Color C2(float t)
    {
        return Color.Lerp(gray, _secondary, t / 10);
    }
    Color C0(float t)
    {
        return new Color(t / 10, t / 10, t / 10);
    }
}

