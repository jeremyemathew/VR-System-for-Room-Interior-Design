using UnityEngine;
using System.Collections;

public class BasicObject : MonoBehaviour
{
    // State
    public bool Selected = false;
    public bool rotating = false;

    [Header("References")]
    public Transform cameraTransform;
    public GameObject HandlePrefab;
    public GameObject ObjectGridPrefab;
    public GameObject GlobalMovmentSettings;

    [Header("Offsets")]
    public float XZoffset = 0.7f;
    public float Yoffset = 0.5f;

    [Header("Movement Settings")]
    public MovmentSettings movementSettings;
    public int MovementMode = 0;
    public float rotationSpeed = 60f;
    public float RotateAngle = 0.0f;
    public float PreviousRotateAngle = 0.0f;

    // Runtime Objects
    public GameObject[] spawnedHandles;
    public GameObject spawnedFloor;


    // Handle Spawning
    public virtual void SpawnHandles()
    {
        spawnedHandles = new GameObject[4];
        spawnedHandles[0] = CreateHandle(new Vector3(0, Yoffset, XZoffset), Quaternion.identity);
        spawnedHandles[1] = CreateHandle(new Vector3(0, Yoffset, -XZoffset), Quaternion.identity);
        spawnedHandles[2] = CreateHandle(new Vector3(XZoffset, Yoffset, 0), Quaternion.Euler(0, 90, 0));
        spawnedHandles[3] = CreateHandle(new Vector3(-XZoffset, Yoffset, 0), Quaternion.Euler(0, 90, 0));
    }


    // Handle Clearing
    public void ClearHandles()
    {
        if (spawnedHandles == null) return;

        foreach (GameObject handle in spawnedHandles)
        {
            if (handle != null)
            {
                Destroy(handle);
            }
        }

        spawnedHandles = null;
    }


    // Handle Creation
    public GameObject CreateHandle(Vector3 localOffset, Quaternion rotation)
    {
        Vector3 worldPos = transform.position + localOffset;
        GameObject handle = Instantiate(HandlePrefab, worldPos, rotation);
        handle.transform.SetParent(transform, true);

        return handle;
    }


    // Floor Spawning
    public void SpawnFloor()
    {
        spawnedFloor = new GameObject();
        spawnedFloor = CreateFloor();
    }


    // Floor Clearing
    public void ClearFloor()
    {
        if (spawnedFloor != null)
        {
            Destroy(spawnedFloor);
        }

        spawnedFloor = null;
    }


    // Floor Creation
    public GameObject CreateFloor()
    {
        // Snap parent X and Z to nearest integer
        float snappedX = Mathf.Round(transform.position.x);
        float snappedZ = Mathf.Round(transform.position.z);

        // Y position slightly above floor
        float yPosition = 0.05f;

        Vector3 spawnPosition = new Vector3(snappedX, yPosition, snappedZ);
        transform.position = new Vector3(snappedX, transform.position.y, snappedZ);

        // Instantiate the prefab
        GameObject gridFloor = Instantiate(ObjectGridPrefab, spawnPosition, ObjectGridPrefab.transform.rotation);

        // Assign the furniture reference
        ObjectGridFloor floorScript = gridFloor.GetComponent<ObjectGridFloor>();
        floorScript.objectTransform = transform;
        floorScript.cameraTransform = cameraTransform;

        return gridFloor;
    }


    // Rotation Coroutine
    public IEnumerator RotateCoroutine()
    {
        Selected = false;

        Quaternion startRot = transform.rotation;

        // Snap Y to nearest 90°
        float currentY = transform.eulerAngles.y;
        float nextY = Mathf.Round(currentY / 90f) * 90f + 90f;
        nextY %= 360f;

        Quaternion targetRot = Quaternion.Euler(transform.eulerAngles.x, nextY, transform.eulerAngles.z);

        // Smoothly rotate towards the target rotation
        while (Quaternion.Angle(transform.rotation, targetRot) > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Ensure exact final rotation
        transform.rotation = targetRot;

        Selected = true;
    }


    // Basic Movement Logic
    public void BasicMovement()
    {
        if (Selected)
        {
            if (movementSettings != null)
            {
                MovementMode = movementSettings.MovementMode;
                rotating = movementSettings.Rotating;
                RotateAngle = movementSettings.RotateAngle;
            }

            if (spawnedHandles == null && MovementMode != 2)
            {
                SpawnHandles();
            }
            else if (spawnedHandles != null && MovementMode == 2)
            {
                ClearHandles();
            }

            if (spawnedFloor == null && MovementMode == 2)
            {
                SpawnFloor();
            }
            else if (spawnedFloor != null && MovementMode != 2)
            {
                ClearFloor();
            }

            if (rotating)
            {
                movementSettings.Rotating = false;
                movementSettings.RotateAngle = 0.0f;
                PreviousRotateAngle = 0.0f;
                StartCoroutine(RotateCoroutine());
            }

            float rotateDelta = Mathf.DeltaAngle(PreviousRotateAngle, RotateAngle);
            transform.Rotate(0f, rotateDelta, 0f, Space.World);
            PreviousRotateAngle = RotateAngle;
        }
        else
        {
            if (spawnedHandles != null)
            {
                ClearHandles();
            }

            if (spawnedFloor != null)
            {
                ClearFloor();
            }
        }
    }
}
