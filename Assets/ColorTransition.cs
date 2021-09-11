using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTransition : MonoBehaviour
{
    public float speed = 15;

    Dictionary<string, Color> colors = new Dictionary<string, Color>();
    [SerializeField]
    List<UnityEngine.UI.Graphic> graphics;
    string defaultColor;
    IEnumerator routine;

    public void AddColor(string name, Color color)
    {
        if (colors.ContainsKey(name))
            colors[name] = color;
        else
            colors.Add(name, color);
    }

    public void Switch(string name)
    {
        if (routine != null)
            StopCoroutine(routine);
        StartCoroutine(routine = SmoothlyChangeColor(graphics,colors[name],speed));
    }

    public void ImmidiateSwitch(string name)
    {
        foreach (UnityEngine.UI.Graphic g in graphics)
            g.color = colors[name];
    }

    public void SetDefault(string name)
    {
        defaultColor = name;
        ImmidiateSwitch(name);
    }

    private IEnumerator SmoothlyChangeColor(List<UnityEngine.UI.Graphic> graphics, Color target, float speed = 15)
    {
        if (graphics.Count == 0) yield break;
        Color original = graphics[0].color;
        float t = 1f;
        while (t > 0.02f)
        {
            t *= Mathf.Pow(0.5f, Time.deltaTime * speed);
            var c = Color.Lerp(target, original, t);
            foreach (UnityEngine.UI.Graphic g in graphics)
            {
                g.color = new Color(c.r, c.g, c.b, g.color.a);
            }

            yield return null;
        }
        foreach (UnityEngine.UI.Graphic g in graphics)
            g.color = new Color(target.r, target.g, target.b, g.color.a);
    }
}
