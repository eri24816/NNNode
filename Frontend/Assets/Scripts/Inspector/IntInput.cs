using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class IntInput : MonoBehaviour
{
    ObjectSync.Attribute<int> attribute;
    TMP_InputField field;
    void Handler(int value)
    {
        field.SetTextWithoutNotify(value.ToString());
    }
    public void SetAttribute(ObjectSync.Attribute<int> attribute)
    {
        this.attribute = attribute;
        field = GetComponent<TMP_InputField>();
        attribute.OnSet += Handler;
        field.onEndEdit.AddListener((v) => { attribute.Set(int.Parse(v)); });
    }
    public void OnDestroy()
    {
        if (attribute != null)
            attribute.OnSet -= Handler;
    }
}
