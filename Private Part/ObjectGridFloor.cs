using UnityEngine;

public class ObjectGridFloor : MonoBehaviour
{
    [Header("References")]
    public Transform objectTransform;
    public Transform cameraTransform;

    [Header("Movement")]
    private float lockedYObject = 0f;
    private Vector3 lastCameraPosition;
    private float inputCooldown = 0.5f;
    private float lastInputTime = 0f;
    float threshold = 0.01f;


    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.SetColor("_BaseColor", Color.yellow);
        }

        lockedYObject = objectTransform.position.y;

        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
        }
    }

    void Update()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null || cameraTransform == null) return;

        float grip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);

        if (grip > 0.5f)
        {
            rend.material.SetColor("_BaseColor", Color.cyan);

            if (Time.time - lastInputTime < inputCooldown)
            {
                lastCameraPosition = cameraTransform.position;
                return;
            }

            Vector3 delta = cameraTransform.position - lastCameraPosition;

            if (delta.z > threshold)
            {
                MoveGrid(0, 1); // forward
            }
            else if (delta.z < -threshold)
            {
                MoveGrid(0, -1); // backward
            }
            else if (delta.x > threshold)
            {
                MoveGrid(1, 0); // right
            }
            else if (delta.x < -threshold)
            {
                MoveGrid(-1, 0); // left
            }

            lastCameraPosition = cameraTransform.position;
        }
        else
        {
            rend.material.SetColor("_BaseColor", Color.yellow);
            lastCameraPosition = cameraTransform.position;
        }
    }

    // Move the grid and object in the specified direction
    void MoveGrid(int xDir, int zDir)
    {
        float newX = objectTransform.position.x;
        float newZ = objectTransform.position.z;

        if (xDir > 0)
            newX = Mathf.Floor(newX) + 1f;
        else if (xDir < 0)
            newX = Mathf.Ceil(newX) - 1f;

        if (zDir > 0)
            newZ = Mathf.Floor(newZ) + 1f;
        else if (zDir < 0)
            newZ = Mathf.Ceil(newZ) - 1f;

        // Move object
        objectTransform.position = new Vector3(newX, lockedYObject, newZ);

        // Move grid floor
        transform.position = new Vector3(newX, transform.position.y, newZ);

        lastInputTime = Time.time;
    }
}
