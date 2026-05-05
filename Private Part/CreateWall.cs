using UnityEngine;

public class CreateWall : MonoBehaviour
{
    [Header("References")]
    public GameObject WallPrefab;
    public Transform WallParent;
    public Transform cameraTransform;
    public GameObject GlobalMovmentSettings;
    public MovmentSettings movementSettings;

    [Header("Spawn Settings")]
    public bool CanCreateWall = false;
    public float spawnDistance = 5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GlobalMovmentSettings != null)
        {
            movementSettings = GlobalMovmentSettings.GetComponent<MovmentSettings>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (movementSettings == null)
            return;

        CanCreateWall = movementSettings.CreateWall;

        if (!CanCreateWall)
            return;

        movementSettings.CreateWall = false;

        CreateWallInFrontOfPlayer();
    }


    // Main function to create a wall in front of the player
    private void CreateWallInFrontOfPlayer()
    {
        Vector3 forward = GetFlattenedForward();
        Vector3 spawnPos = GetSpawnPosition(forward);

        GameObject newWall = SpawnWall(spawnPos, forward);

        SetupWall(newWall);
    }


    // Get the camera's forward direction, but flattened to the horizontal plane (y=0) to avoid tilting the wall up or down
    private Vector3 GetFlattenedForward()
    {
        Vector3 forward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }


    // Calculate the spawn position based on the camera's position and forward direction, at a fixed distance
    private Vector3 GetSpawnPosition(Vector3 forward)
    {
        Vector3 spawnPos = cameraTransform.position + forward * spawnDistance;

        spawnPos.y = 0f; // force ground level

        return spawnPos;
    }


    // Instantiate the wall prefab at the given position and rotation, and parent it if needed
    private GameObject SpawnWall(Vector3 position, Vector3 forward)
    {
        Quaternion rotation = Quaternion.LookRotation(forward); // face away from player

        GameObject newWall = Instantiate(WallPrefab, position, rotation);

        if (WallParent != null)
        {
            newWall.transform.SetParent(WallParent);
        }

        return newWall;
    }


    // After spawning, set up the wall's references and settings
    private void SetupWall(GameObject wallObj)
    {
        Wall wallScript = wallObj.GetComponent<Wall>();

        if (wallScript == null) return;

        wallScript.GlobalMovmentSettings = GlobalMovmentSettings;

        if (cameraTransform != null)
        {
            wallScript.cameraTransform = cameraTransform;
        }
    }
}
