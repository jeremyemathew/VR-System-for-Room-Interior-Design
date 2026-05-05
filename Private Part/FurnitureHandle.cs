using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class FurnitureHandle : Interactable
{
    [Header("References")]
    public Transform parentTransform;           // Parent object object

    [Header("Movement")]
    private int MovementMode;
    private Vector3 fineAxis = Vector3.forward;   // Local axis for Fine mode
    public float breakDistance = 0.5f;           // Max distance before breaking grip
    private float lockedYObject = 0f;


    [Header("Haptics")]
    [Range(0f, 1f)] public float dragHaptics = 0.20f;
    public float tickEvery = 0.02f;

    // --- internal state ---
    private OVRController controller;
    private Vector3 objectGrabOffset;
    private Quaternion objectOriginalRotation;
    private float controllerStartYaw;
    private Vector3 lastObjectPosition;

    [Header("Spawn Animation")]
    public float animationOffset = 0.1f;
    public float animationDuration = 0.25f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Start()
    {
        parentTransform = transform.parent;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.SetColor("_BaseColor", Color.yellow);
        }

        StartCoroutine(PopAnimation());
    }

    void Update()
    {
        if (controller == null)
        {
            return;
        }

        // Break grip if hand moves too far from handle
        float dist = Vector3.Distance(controller.transform.position, transform.position);

        if (dist > breakDistance)
        {
            controller.HapticClick(0.6f, 0.03f);
            Debug.Log("HAPTIC: LARGE (Break Grip)");
            OnGripEnd(controller);
            return;
        }


        switch (MovementMode)
        {
            case 0:
                CoarseMovment();
                break;
            case 1:
                FineMovment();
                break;
            default:
                break;
        }

        // DRAG HAPTICS 
        float moved = Vector3.Distance(lastObjectPosition, parentTransform.position);

        if (moved >= tickEvery)
        {
            controller.HapticTick(Mathf.Clamp01(dragHaptics), 0.015f);
            Debug.Log("HAPTIC: SMALL (Drag Tick)");

            lastObjectPosition = parentTransform.position;
        }
    }


    // Initialize grip state on grip begin
    public override void OnGripBegin(OVRController ctrl)
    {
        controller = ctrl;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.SetColor("_BaseColor", Color.cyan);
        }

        Vector3 handPosition = controller.transform.position;
        Vector3 targetObject = parentTransform.position;
        objectGrabOffset = new Vector3(targetObject.x - handPosition.x, 0f, targetObject.z - handPosition.z);
        objectOriginalRotation = parentTransform.rotation;
        lockedYObject = targetObject.y;
        Furniture parentFurniture = parentTransform.gameObject.GetComponent<Furniture>();
        if (parentFurniture != null)
        {
            MovementMode = parentFurniture.MovementMode;
        }
        else
        {
            Wall parentWall = parentTransform.gameObject.GetComponent<Wall>();
            if (parentWall != null)
            {
                MovementMode = parentWall.MovementMode;
            }
        }
        controllerStartYaw = controller.transform.eulerAngles.y;

        lastObjectPosition = parentTransform.position;

        //  INITIAL GRAB HAPTIC 
        controller.HapticClick(0.15f, 0.02f);
        Debug.Log("HAPTIC: SMALL (Initial Grab)");
    }


    // Reset state on grip end
    public override void OnGripEnd(OVRController ctrl)
    {
        if (controller != ctrl)
        {
            return;
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.SetColor("_BaseColor", Color.yellow);
        }

        controller = null;
    }

    // Fine movement: move along local axis with no snapping
    public void FineMovment()
    {

        // Determine movement axis (handle forward in world space)
        Vector3 axis = transform.TransformDirection(fineAxis).normalized;

        // Object movement - same axis and movement as handle
        Vector3 proposedObject = controller.transform.position + objectGrabOffset;

        // Project movement onto axis
        Vector3 objectDeltaVec = proposedObject - parentTransform.position;
        float movement2 = Vector3.Dot(objectDeltaVec, axis);

        // Move object along axis and lock Y
        Vector3 newObjectPosition = parentTransform.position + axis * movement2;
        newObjectPosition.y = lockedYObject;
        parentTransform.position = newObjectPosition;
    }


    // Coarse movement: rotate around Y based on controller yaw, then position based on hand with grab offset
    public void CoarseMovment()
    {
        // Rotate
        float currentYaw = controller.transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(controllerStartYaw, currentYaw);
        Quaternion rotationOffset = Quaternion.Euler(0f, yawDelta, 0f);
        parentTransform.rotation = rotationOffset * objectOriginalRotation;

        // Position
        Vector3 handPosition = controller.transform.position;

        // rotate the original grab offset by the same yaw
        Vector3 rotatedOffset = rotationOffset * objectGrabOffset;

        Vector3 newObjectPosition = new Vector3(
            handPosition.x + rotatedOffset.x,
            lockedYObject,
            handPosition.z + rotatedOffset.z
        );

        parentTransform.position = newObjectPosition;
    }

    // Pop Animation Coroutine
    IEnumerator PopAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * animationOffset;
        float time = 0f;

        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.position = Vector3.Lerp(startPos, upPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = upPos;

        time = 0f;
        while (time < animationDuration)
        {
            float t = animationCurve.Evaluate(time / animationDuration);
            transform.position = Vector3.Lerp(upPos, startPos, t);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;
    }

}
