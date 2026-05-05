using System.Collections.Generic;
using UnityEngine;

public class FastTravelNext : MonoBehaviour
{
    [SerializeField] private Transform OVRCameraRig;     // OVRCameraRig transform (root)
    [SerializeField] private Transform centerEyeAnchor;  // TrackingSpace/CenterEyeAnchor

    [Header("Hotspots (TargetPose transforms)")]
    [SerializeField] private List<Transform> hotspots = new List<Transform>();

    [Header("Input")]
    [SerializeField] private OVRInput.Button nextButton = OVRInput.Button.One; // A on right controller.
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Debounce")]
    [SerializeField] private float cooldownSeconds = 0.25f;
    private float _nextAllowedTime = 0f;

    [Header("Fast Travel")]
    [SerializeField] private float travelDuration = 0.09f; // 90ms
    private bool isTraveling = false;

    [Header("Auto Generate Hotspots")]
    [SerializeField] private Transform floorTransform;
    [SerializeField] private GameObject hotspotPrefab;
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private Transform hotspotParent;

    private void Start()
    {
        GenerateHotspots();
    }

    private void Reset()
    {
        // If you drop this onto something in-scene, try to auto-find common names:
        if (OVRCameraRig == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig) OVRCameraRig = rig.transform;
        }
        if (centerEyeAnchor == null && OVRCameraRig != null)
        {
            var t = OVRCameraRig.Find("TrackingSpace/CenterEyeAnchor");
            if (t) centerEyeAnchor = t;
        }
    }

    private void Update()
    {
        if (Time.time < _nextAllowedTime) return;

        if (OVRInput.GetDown(nextButton, controller))
        {
            _nextAllowedTime = Time.time + cooldownSeconds;
            FastTravelToNext();
        }
    }

    // This method should update the hotspot index, get the target hotspot transform, and call FastTravelToNextHotspot with that target.
    public void FastTravelToNext()
    {
        if (OVRCameraRig == null || centerEyeAnchor == null || hotspots == null || hotspots.Count == 0)
        {
            Debug.LogWarning("Missing hotspots");
            return;
        }

        // Get the next hotspot in the direction the player is looking
        Transform target = GetHotspotInLookDirection();

        if (target != null)
        {
            FastTravelToNextHotspot(target);
        }
        else
        {
            Debug.Log("No hotspot found in look direction.");
        }
    }


    // This method should calculate the new rig position and rotation based on the target hotspot, then start the FastTravelRoutine coroutine to move there smoothly.
    private void FastTravelToNextHotspot(Transform target)
    {
        if (isTraveling) return;

        Vector3 headPosition = centerEyeAnchor.position;
        Vector3 rigPosition = OVRCameraRig.position;

        // Offset from head to rig
        Vector3 offset = rigPosition - headPosition;

        // Desired head XZ at hotspot
        Vector3 desiredHeadPosition = new Vector3(target.position.x, headPosition.y, target.position.z);
        Vector3 newRigPosition = desiredHeadPosition + offset;

        float targetY = target.eulerAngles.y;
        Quaternion newRotation = Quaternion.Euler(0f, targetY, 0f);

        StartCoroutine(FastTravelRoutine(newRigPosition, newRotation));
    }


    // Coroutine to smoothly move the rig to the target position and rotation over travelDuration seconds
    private System.Collections.IEnumerator FastTravelRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        isTraveling = true;

        Vector3 startPos = OVRCameraRig.position;
        Quaternion startRot = OVRCameraRig.rotation;

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / travelDuration); // linear time

            // Constant velocity (no easing)
            OVRCameraRig.position = Vector3.Lerp(startPos, targetPos, t);
            OVRCameraRig.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        // Ensure exact final placement
        OVRCameraRig.position = targetPos;
        OVRCameraRig.rotation = targetRot;

        isTraveling = false;
    }


    // This method iterates through all hotspots and finds the one that is within a certain angle of the player's look direction and is closest to the center of that cone.
    // It returns the best matching hotspot transform or null if none are found.
    private Transform GetHotspotInLookDirection(float maxAngle = 60f)
    {
        Transform bestHotspot = null;
        float bestAlignment = -1f; 
        float bestDistance = float.MaxValue;
        Vector3 origin = centerEyeAnchor.position;
        Vector3 lookDirection = new Vector3(centerEyeAnchor.forward.x, 0f, centerEyeAnchor.forward.z);

        if (lookDirection.sqrMagnitude < 0.001f)
            return null;

        lookDirection.Normalize();
        float cosThreshold = Mathf.Cos(maxAngle * Mathf.Deg2Rad);

        // Iterate through hotspots to find the best candidate in the look direction cone
        foreach (var hotspot in hotspots)
        {
            if (hotspot == null) continue;

            Vector3 toHotspot = hotspot.position - origin;

            // Flatten direction to XZ
            Vector3 toHotspotXZ = new Vector3(toHotspot.x, 0f, toHotspot.z);

            float distance = toHotspotXZ.magnitude;
            if (distance < 0.001f) continue;

            Vector3 dirToHotspot = toHotspotXZ / distance;

            // Dot product = alignment
            float alignment = Vector3.Dot(lookDirection, dirToHotspot);

            if (alignment >= cosThreshold)
            {
                // alignment first, distance second
                if (alignment > bestAlignment ||
                   (Mathf.Approximately(alignment, bestAlignment) && distance < bestDistance))
                {
                    bestAlignment = alignment;
                    bestDistance = distance;
                    bestHotspot = hotspot;
                }
            }
        }

        return bestHotspot;
    }


    // This method generates hotspots in a grid pattern across the floor area.
    // It first validates the necessary components and parameters, then calculates the bounds of the floor, determines how many hotspots can fit based on spacing and edge padding, and finally spawns the hotspots at the calculated positions.
    private void GenerateHotspots()
    {
        if (!ValidateGeneration()) return;

        hotspots.Clear();

        Bounds bounds = GetFloorBounds();

        Vector2Int gridCounts = CalculateGridCounts(bounds);

        SpawnGrid(bounds, gridCounts);
    }


    // This method checks that the necessary components for hotspot generation are assigned and that the spacing is valid. It logs warnings if any issues are found.
    private bool ValidateGeneration()
    {
        if (floorTransform == null || hotspotPrefab == null)
        {
            Debug.LogWarning("Missing floorTransform or hotspotPrefab.");
            return false;
        }

        if (spacing <= 0f)
        {
            Debug.LogWarning("Spacing must be > 0.");
            return false;
        }

        return true;
    }


    // This method attempts to get the bounds of the floor using its Renderer. If no Renderer is found, it falls back to using the floor's position and local scale as a rough estimate of bounds.
    private Bounds GetFloorBounds()
    {
        Renderer renderer = floorTransform.GetComponent<Renderer>();

        if (renderer != null)
            return renderer.bounds;

        Debug.LogWarning("No Renderer found on floor. Using fallback bounds.");
        return new Bounds(floorTransform.position, floorTransform.localScale);
    }


    // This method calculates how many hotspots can fit in the X and Z directions within the bounds, accounting for edge padding and spacing.
    // It ensures that the counts are odd numbers to allow for a center hotspot.
    private Vector2Int CalculateGridCounts(Bounds bounds)
    {
        float usableWidth = bounds.size.x - (2f * edgePadding);
        float usableLength = bounds.size.z - (2f * edgePadding);

        int countX = Mathf.FloorToInt(usableWidth / spacing);
        int countZ = Mathf.FloorToInt(usableLength / spacing);

        // Force odd numbers so center exists
        if (countX % 2 == 0) countX--;
        if (countZ % 2 == 0) countZ--;

        countX = Mathf.Max(1, countX);
        countZ = Mathf.Max(1, countZ);

        return new Vector2Int(countX, countZ);
    }


    // This method spawns a grid of hotspots within the given bounds, using the specified counts for X and Z directions. It checks each position against the bounds with edge padding before creating a hotspot.
    private void SpawnGrid(Bounds bounds, Vector2Int counts)
    {
        Vector3 center = bounds.center;
        int halfX = counts.x / 2;
        int halfZ = counts.y / 2;

        // Loop through grid positions and spawn hotspots
        for (int x = -halfX; x <= halfX; x++)
        {
            for (int z = -halfZ; z <= halfZ; z++)
            {
                Vector3 position = new Vector3(
                    center.x + x * spacing,
                    center.y,
                    center.z + z * spacing
                );

                if (IsInsideBounds(position, bounds))
                {
                    CreateHotspot(position);
                }
            }
        }
    }


    // This method checks if the given position is within the bounds of the floor, accounting for edge padding.
    private bool IsInsideBounds(Vector3 pos, Bounds bounds)
    {
        return (pos.x >= bounds.min.x + edgePadding) &&
               (pos.x <= bounds.max.x - edgePadding) &&
               (pos.z >= bounds.min.z + edgePadding) &&
               (pos.z <= bounds.max.z - edgePadding);
    }


    // This method instantiates a hotspot prefab at the given position and adds its transform to the hotspots list.
    private void CreateHotspot(Vector3 position)
    {
        GameObject obj = Instantiate(
            hotspotPrefab,
            position,
            Quaternion.identity,
            hotspotParent != null ? hotspotParent : transform
        );

        hotspots.Add(obj.transform);
    }

}
