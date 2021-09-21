using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTransition : MonoBehaviour
{
    public float speed = 15;
    public Color color;
    Dictionary<string, Color> colors = new Dictionary<string, Color>();
    [SerializeField]
    List<UnityEngine.UI.Graphic> graphics;
    IEnumerator routine;

    public void SetColor(string name, Color color)
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
        color = colors[name];
        foreach (UnityEngine.UI.Graphic g in graphics)
            g.color = color;
    }

    public void SetDefault(string name)
    {
        ImmidiateSwitch(name);
    }

    private IEnumerator SmoothlyChangeColor(List<UnityEngine.UI.Graphic> graphics, Color target, float speed = 15)
    {
        float t = 1f;
        Color original = color;
        while (t > 0.02f)
        {
            t *= Mathf.Pow(0.5f, Time.deltaTime * speed);
            color = Color.Lerp(target, original, t);
            foreach (UnityEngine.UI.Graphic g in graphics)
            {
                g.color = new Color(color.r, color.g, color.b, g.color.a);
            }

            yield return null;
        }
        color = target;
        foreach (UnityEngine.UI.Graphic g in graphics)
            g.color = new Color(color.r, color.g, color.b, g.color.a);
    }
}
