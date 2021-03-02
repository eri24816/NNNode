using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CodeNode : Node,IEndDragHandler,IBeginDragHandler,IDragHandler
{
    public bool expanded = false;

    [SerializeField]
    TMPro.TMP_InputField nameInput;

    public GameObject CodeEditor;

    public override void Start()
    {
        base.Start();
        Reshape(1.3f, 1f, 1f);
        nameInput.enabled = false;
    }


    public override void Update()
    {
        base.Update();
        if (dragging) OnMouseDrag();
    }

    public override void Reshape(float w, float l, float r)//Trapezoid shaped node
    {

        float h = l < r ? l : r;
        upPad = h / 2;
        
        /*
        if (ins.Count > 0)
            for (int i = 0; i < ins.Count; i++)
            {
                ins[i].transform.position = new Vector3(-w / 2,- .1f*i -.1f) + transform.position;
            }

        if (outs.Count > 0)
            for (int i = 0; i < outs.Count; i++)
            {
                outs[i].transform.position = new Vector3(w / 2, - .1f * i - .1f) + transform.position;
            }
        */
    }
    public void Rename()
    {
        nameInput.enabled = true;
        nameInput.Select();
    }
    public void RenameEnd()
    {
        nameInput.enabled = false;
    }
    public void CollapseOrExpand()
    {
        expanded ^= true;
        CodeEditor.SetActive(expanded);
    }
    public bool dragging = false;
    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }
    public override Port GetPort(bool isInput = true, string var_name = "")
    {
        return isInput ? ins[0] : outs[0];
    }
}
