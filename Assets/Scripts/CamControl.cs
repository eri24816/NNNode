﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphUI;
using UnityEngine.EventSystems;

public class CamControl : MonoBehaviour
{
    public static CamControl ins;
    public static bool ctrlDown, shiftDown;
    public float scroll = 0.1f,zoom=0.1f;
    public bool touchPadMode=false;
    public static Vector3 mouseDelta, mouse,worldMouseDelta,worldMouse;
    public static Collider colliderHover;
    Plane focusPlane;
    Camera cam;
    private void Start()
    {
        ins = this;
        focusPlane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));
        cam = GetComponent<Camera>();
    }
    Vector3 WorldPoint()
    {
        Ray ray= cam.ScreenPointToRay(Input.mousePosition);
        focusPlane.Raycast(ray, out float enter);
        return ray.GetPoint(enter);
    }
    bool CompareTag(GameObject g,string tag)
    {
        return g && g.CompareTag(tag);
    }
    bool CompareTag(Collider g, string tag)
    {
        return g && g.CompareTag(tag);
    }
    private void Update()
    {
        mouseDelta= Input.mousePosition-mouse;
        mouse = Input.mousePosition;
        worldMouseDelta = WorldPoint()-worldMouse;
        worldMouse = WorldPoint();
        ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
        //mouse mode
        if (!touchPadMode)
        {
            if (Input.GetMouseButton(2))
            {
                transform.Translate(-worldMouseDelta);
                worldMouse = WorldPoint();
            }

            
            transform.Translate(worldMouse - WorldPoint()); // make center of the camera stick to a point
        }

        //touch pad mode
        else
        {
            if (ctrlDown)
            {
                transform.position += new Vector3(0, 0, Input.mouseScrollDelta.y * zoom/2);
            }
            if (!(selectedGameObject.CompareTag("InputField")))
                transform.position += (Vector3)Input.mouseScrollDelta * scroll;
        }
        Physics.Raycast(cam.ScreenPointToRay(mouse), out RaycastHit hit);
        colliderHover = hit.collider;
        /*
        if (Input.GetKeyDown(KeyCode.C))
        {
            Node newNode = Instantiate(Manager.ins.prefabDict["CodeNode"]).GetComponent<Node>();
            newNode.Name = newNode.name = $"CodeNode {Manager.ins.nameNum++}";
            newNode.transform.position = worldMouse;
            StartCoroutine(newNode.Creating());
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            Node newNode = Instantiate(Manager.ins.prefabDict["FunctionNode"]).GetComponent<Node>();
            newNode.Name = newNode.name = $"FunctionNode {Manager.ins.nameNum++}";
            newNode.transform.position = worldMouse;
            StartCoroutine(newNode.Creating());
        }
        */
        if (ctrlDown && Input.GetKeyDown(KeyCode.Z)) Manager.ins.Undo(Selectable.TheOnlySelectedNode());
        if (ctrlDown && Input.GetKeyDown(KeyCode.Y)) Manager.ins.Redo(Selectable.TheOnlySelectedNode());
        if (Input.GetKeyDown(KeyCode.Delete))
            Selectable.Delete();
    }

    public void OnBackgroundScroll(PointerEventData e)
    {
        worldMouse = WorldPoint();
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
            transform.Translate(new Vector3(0, 0, e.scrollDelta.y * zoom * focusPlane.GetDistanceToPoint(transform.position)));
        transform.Translate(worldMouse - WorldPoint()); // make center of the camera stick to a point
    }
}
