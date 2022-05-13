using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NNNode;
using UnityEngine.EventSystems;

public class CamControl : MonoBehaviour
{
    public static CamControl ins;
    public static bool ctrlDown, shiftDown;
    public float scroll = 0.1f,zoom=0.1f;
    public bool touchPadMode=false;
    public static Vector3 mouseDelta, mouse,worldMouseDelta,worldMouse;
    public static Collider colliderHover;
    //public static Port portHover;
    public UnityEngine.UI.ScrollRect nodeList;
    public MyInputModule inputModule;
    Plane focusPlane; 
    Camera cam;
    private void Start()
    {
        ins = this; 
        focusPlane = new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, 0));
        cam = GetComponent<Camera>();
        inputModule.scrollFallback += OnScroll;
        inputModule.pressFallback += OnClick;
    }
    public Vector3 WorldPoint()
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

                transform.position += (Vector3)Input.mouseScrollDelta * scroll*0.05f;
        }
        Physics.Raycast(cam.ScreenPointToRay(mouse), out RaycastHit hit);
        colliderHover = hit.collider;

        if (ctrlDown && Input.GetKeyDown(KeyCode.Z)) {
            var o = Selectable.TheOnlySelectedNode();
            if (o) o.Undo();
            else SpaceClient.ins.Root.Undo();
        }
        if (ctrlDown && Input.GetKeyDown(KeyCode.Y))
        {
            var o = Selectable.TheOnlySelectedNode();
            if (o) o.Redo();
            else SpaceClient.ins.Root.Redo();
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Selectable.Delete();
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Break();
        }


    }

    public void OnScroll(PointerEventData e)
    {
        worldMouse = WorldPoint();
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null)
            transform.Translate(new Vector3(0, 0, e.scrollDelta.y * zoom * focusPlane.GetDistanceToPoint(transform.position)));
        transform.Translate(worldMouse - WorldPoint()); // make center of the camera stick to a point
    }

    public void OnClick(PointerEventData e)
    {
        if(!(shiftDown || ctrlDown))
            Selectable.ClearSelection();
    }

}
