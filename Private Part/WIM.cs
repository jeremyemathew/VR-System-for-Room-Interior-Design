using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WIM : MonoBehaviour
{
    [Header("References")]
    public GameObject room;
    public Transform cameraTransform;

    [Header("WIM Settings")]
    public float spawnDistance = 2f;
    public float heightOffset = -0.3f;
    public float scaleFactor = 0.01f;

    [Header("Refresh Settings")]
    public float refreshInterval = 0.2f;

    private GameObject wimInstance;
    private bool isActive = false;
    private Coroutine refreshCoroutine;


    void Start()
    {
        if (cameraTransform == null)
        {
            // Try to find OVR camera rig automatically
            var ovrRig = FindObjectOfType<OVRCameraRig>();
            if (ovrRig != null)
                cameraTransform = ovrRig.centerEyeAnchor;
            else
                cameraTransform = Camera.main?.transform;
        }
    }

    void Update()
    {
        HandleInput();

        if (isActive && wimInstance != null)
        {
            PositionWIM();
        }
    }

    
    // Check for input to toggle the WIM on/off
    void HandleInput()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
        {
            ToggleWIM();
        }
    }


    // Toggle the WIM on/off
    void ToggleWIM()
    {
        if (isActive)
            HideWIM();
        else
            ShowWIM();
    }


    // Show the WIM, creating it if necessary, and start the refresh coroutine for dynamic objects
    void ShowWIM()
    {
        if (room == null)
        {
            return;
        }

        if (wimInstance == null)
            CreateWIM();

        wimInstance.SetActive(true);
        isActive = true;

        // Start periodic deep-sync for dynamic objects
        if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
        refreshCoroutine = StartCoroutine(RefreshLoop());

        Debug.Log("WIM: shown");
    }


    // Hide the WIM and stop any ongoing refresh coroutines
    void HideWIM()
    {
        if (wimInstance != null)
            wimInstance.SetActive(false);

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        isActive = false;
        Debug.Log("WIM: hidden");
    }


    // Create a deep clone of the room, remove colliders/rigidbodies, and prepare it for display as a WIM
    void CreateWIM()
    {
        // Deep-clone the room so we get all children, renderers, meshes, materials
        wimInstance = Instantiate(room);
        wimInstance.name = "WIM_Instance";

        // Scale down
        wimInstance.transform.localScale = room.transform.localScale * scaleFactor;

        // Remove any colliders and rigidbodies so the miniature is inert
        foreach (var col in wimInstance.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var rb in wimInstance.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        // Disable scripts that shouldn't run on the copy
        foreach (var mono in wimInstance.GetComponentsInChildren<MonoBehaviour>())
        {
            // Keep this script active but kill anything else that might move things
            if (mono is WIM) Destroy(mono);
        }


        // Place it immediately
        PositionWIM();
    }


    //  Position the WIM in front of the player and orient it to match the player's facing direction
    void PositionWIM()
    {
        if (cameraTransform == null) return;

        // Place the WIM in front of and slightly below the camera
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPosition = cameraTransform.position
                          + forward * spawnDistance
                          + Vector3.up * heightOffset;

        wimInstance.transform.position = targetPosition;

        // Rotate the miniature to match the player's facing direction
        wimInstance.transform.rotation = Quaternion.LookRotation(forward);
    }


    //  Sync child transforms (for dynamic objects inside the room)
    IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            SyncChildren(room.transform, wimInstance.transform);
        }
    }


    // Recursively copy localPosition and localRotation from source to target for all children
    void SyncChildren(Transform source, Transform target)
    {
        if (source.childCount != target.childCount) return; // structural mismatch – skip

        for (int i = 0; i < source.childCount; i++)
        {
            Transform srcChild = source.GetChild(i);
            Transform tgtChild = target.GetChild(i);
            tgtChild.localPosition = srcChild.localPosition;
            tgtChild.localRotation = srcChild.localRotation;
            SyncChildren(srcChild, tgtChild);
        }
    }
}