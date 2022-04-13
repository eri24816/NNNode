using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour
{
    public static Helper helper;

    private void Start()
    {
        helper = this;
    }
    public static void WaitOneFrame(System.Action a)
    {
        helper.StartCoroutine(helper.WaitOneFrame_(a));
    }
    IEnumerator WaitOneFrame_(System.Action a)
    {
        yield return null;
        a();
    }
}
