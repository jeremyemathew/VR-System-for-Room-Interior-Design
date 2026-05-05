using UnityEngine;
using System.Collections;

public class RaySelect : MonoBehaviour
{
    [Header("References")]
    public Transform controller;
    public Transform controllerTip;  
    public Transform holdPoint;      

    [Header("Laser")]
    public LineRenderer laser;
    public Material laserMaterial;
    public float laserWidth = 0.01f;
    public float laserMaxDistance = 1f;

    [Header("Animation")]
    public float moveDuration = 0.6f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private LayerMask laserMask;

    private GameObject selectedObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    [Header("Movement Settings")]
    public GameObject GlobalMovmentSettings;
    private MovmentSettings movementSettings;

    void Awake()
    {
        // Create Laser
        if (laser == null)
        {
            laser = gameObject.AddComponent<LineRenderer>();
            ConfigureLaser(laser);
        }
        else
        {
            // Make sure it's configured for our use
            ConfigureLaser(laser, keepMaterialIfAssigned: true);
        }

        laser.enabled = false;
        // Laser can hit layers: Selectable
        laserMask = LayerMask.GetMask("Selectable");

        if (GlobalMovmentSettings != null)
        {
            movementSettings = GlobalMovmentSettings.GetComponent<MovmentSettings>();
        }
    }


    void Update()
    {
        float trigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        if (trigger > 0.5f)
        {
            DeselectObject();
            UpdateLaser();
            HoverHighlight();
        }
        else
        {
            laser.enabled = false;

            if (selectedObject != null)
            {
                SelectHighlight();
                SelectObject();
            }
        }
    }

    // Configures the LineRenderer for laser use
    private void ConfigureLaser(LineRenderer lr, bool keepMaterialIfAssigned = false)
    {
        if (lr == null)
        {
            return;
        }

        // Assign material if not keeping existing
        if (!keepMaterialIfAssigned || lr.material == null)
        {
            lr.material = laserMaterial;
        }

        // Set laser width
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;

        // Use world space coordinates
        lr.useWorldSpace = true;

        // Optional: set colors (red glow)
        lr.startColor = Color.red;
        lr.endColor = Color.red;

        // Make laser initially disabled
        lr.enabled = false;

        // Set all the line renderer (lr) paramters
        UnityEngine.Debug.Log("Laser Created");
    }


    // Updates laser position and direction based on controller
    private void UpdateLaser()
    {
        if (controllerTip == null || laser == null)
        {
            return;
        }

        // Calculate laser origin, direction, and end position - use controller position
        // and controller coordinate system
        Vector3 origin = controller.position;
        Vector3 direction = controller.forward;
        Vector3 endPosition = origin + direction * laserMaxDistance;

        // Enable laser and set laser (lineRenderer) end points
        laser.enabled = true;
        laser.SetPosition(0, origin);
        laser.SetPosition(1, endPosition);
    }


    // Casts a ray from the controller tip and highlights the object if it has changed since last frame
    void HoverHighlight()
    {
        RaycastHit hit;

        if (Physics.Raycast(controllerTip.position, controllerTip.forward, out hit, laserMaxDistance, laserMask))
        {
            GameObject currentObject = hit.collider.gameObject;

            if (selectedObject == currentObject)
            {
                return;
            }

            ClearHighlight();
            selectedObject = currentObject;
            Renderer renderer = selectedObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                // Set outline scale to 1.1f and color to yellow for all materials that have the _OutlineScale property
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty("_OutlineScale"))
                    {
                        mat.SetFloat("_OutlineScale", 1.1f);
                        mat.SetColor("_OutlineColor", Color.yellow);
                    }
                }
            }
        }
        else
        {
            ClearHighlight();
        }
    }


    // Clear highlight from previously selected object
    void ClearHighlight()
    {
        if (selectedObject == null)
        {
            return;
        }

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Reset outline scale to 1.0f for all materials that have the _OutlineScale property
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_OutlineScale"))
                {
                    mat.SetFloat("_OutlineScale", 0.0f);
                }
            }
        }

        selectedObject = null;
    }


    // When object is selected, change outline color to cyan
    void SelectHighlight()
    {
        Renderer renderer = selectedObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            // Change outline color to cyan for all materials that have the _OutlineScale property
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_OutlineScale"))
                {
                    mat.SetFloat("_OutlineScale", 1.1f);
                    mat.SetColor("_OutlineColor", Color.cyan);
                }
            }
        }
    }


    // If the object has a Furniture component, set its Selected property to true
    void SelectObject()
    {
        if (movementSettings == null)
        {
            return;
        }

        Furniture furniture = selectedObject.GetComponent<Furniture>();

        if (furniture != null && furniture.Selected != true)
        {
            movementSettings.WidgetMode = 0;
            furniture.Selected = true;
            return;
        }

        WallPanel panel = selectedObject.GetComponent<WallPanel>();

        if (panel != null && panel.Selected != true)
        {
            movementSettings.WidgetMode = 1;
            panel.Selected = true;
            return;
        }

    }


    // If the object has a Furniture component, set its Selected property to false
    void DeselectObject()
    {

        if (movementSettings != null && movementSettings.WidgetMode != 2)
        {
            movementSettings.WidgetMode = 2;
        }

        if (selectedObject == null)
        {
            return;
        }

        Furniture furniture = selectedObject.GetComponent<Furniture>();

        if (furniture != null && furniture.Selected == true)
        {
            furniture.Selected = false;
        }
        else
        {
            WallPanel panel = selectedObject.GetComponent<WallPanel>();
            if (panel != null && panel.Selected == true)
            {
                panel.Selected = false;
            }
        }

    }

   
}