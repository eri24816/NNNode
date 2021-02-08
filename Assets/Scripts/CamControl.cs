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
    private void Update()
    {
        mouseDelta= Input.mousePosition-mouse;
        mouse = Input.mousePosition;
        worldMouseDelta = WorldPoint()-worldMouse;
        worldMouse = WorldPoint();

        //mouse mode
        if (!touchPadMode)
        {
            if (Input.GetMouseButton(2))
            {
                transform.Translate(-worldMouseDelta);
                worldMouse = WorldPoint();
            }
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
            transform.position += (Vector3)Input.mouseScrollDelta * scroll;
        }
        Physics.Raycast(cam.ScreenPointToRay(mouse), out RaycastHit hit);
        colliderHover = hit.collider;
    }
}
