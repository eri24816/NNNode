using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CodeNode : Node,IEndDragHandler,IBeginDragHandler,IDragHandler
{
    public bool expanded = false;

    [SerializeField]
    TMPro.TMP_InputField nameInput;

    [SerializeField]
    GameObject CodeEditor;

    TMPro.CodeEditor CodeEditorScript;

    public string Code { get { return CodeEditorScript.text; } set { CodeEditorScript.text = value; } }

    public override void Start()
    {
        base.Start();
        nameInput.enabled = false;
        CodeEditorScript = CodeEditor.GetComponent<TMPro.CodeEditor>();
    }


    public override void Update()
    {
        base.Update();
        if (dragging) OnMouseDrag();
    }

    public override void Reshape(float w, float l, float r)
    {
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

    public void OnDrag(PointerEventData eventData){}
    public override Port GetPort(bool isInput = true, string var_name = "")
    {
        return isInput ? ins[0] : outs[0];
    }
    protected override void OnDoubleClick()
    {
        Manager.i.Activate(this);
    }
    public void SetCode()
    {
        Manager.i.SetCode(this);
    }
}
