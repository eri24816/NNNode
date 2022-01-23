using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffect : MonoBehaviour
{
    public static SoundEffect i;
    public AudioSource source;
    public AudioClip click,hover;
    public void Start()
    {
        i = this;
        source.volume = 0.1f;
    }
    public static void Click(MonoBehaviour o = null)
    {
        if (o) i.transform.position = o.transform.position;
        i.source.PlayOneShot(i.click);
    }
    public static void Hover(MonoBehaviour o = null)
    {
        if (o) i.transform.position = o.transform.position;
        i.source.PlayOneShot(i.hover);
    }
}
