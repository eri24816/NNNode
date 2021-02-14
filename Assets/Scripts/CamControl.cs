using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour
{
    public float scroll = 0.1f,zoom=0.1f;
    public bool touchPadMode=false;
    public static Vector3 mouseDelta, mouse,worldMouseDelta,worldMouse;
    public static Collider colliderHover;
    Plane focusPlane;
    Camera cam;
    private void Start()
    {
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

            if(CompareTag(colliderHover, "background") || !CompareTag(selectedGameObject, "InputField"))
                transform.Translate(new Vector3(0, 0, Input.mouseScrollDelta.y * zoom*focusPlane.GetDistanceToPoint(transform.position)));
            transform.Translate(worldMouse - WorldPoint());
        }

        //touch pad mode
        else
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                transform.position += new Vector3(0, 0, Input.mouseScrollDelta.y * zoom/2);
            }
            if (!(selectedGameObject.CompareTag("InputField")))
                transform.position += (Vector3)Input.mouseScrollDelta * scroll;
        }
        Physics.Raycast(cam.ScreenPointToRay(mouse), out RaycastHit hit);
        colliderHover = hit.collider;

        if (Input.GetKeyDown(KeyCode.C))
        {
            Manager.i.CreateNode("CodeNode",worldMouse);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            Manager.i.CreateNode("FunctionNode", worldMouse);
        }
        bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrlDown && Input.GetKeyDown(KeyCode.Z)) Manager.i.Undo();
        if (ctrlDown && Input.GetKeyDown(KeyCode.Y)) Manager.i.Redo();
        
    }
}
