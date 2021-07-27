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
    }
    public static void Click()
    {
        i.source.PlayOneShot(i.click);
    }
    public static void Hover()
    {
        i.source.PlayOneShot(i.hover);
    }
}
