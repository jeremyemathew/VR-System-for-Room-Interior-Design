using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wall : BasicObject
{
    public GameObject plainWallPrefab;
    public GameObject doorPrefab;
    public GameObject windowPrefab;

    public List<Transform> panels = new List<Transform>();
    public enum PanelCategory { Plain, Door, Window }

    public bool ChangePanel = false;
    public int panelIndex = 0;
    public PanelCategory NewPanelType = PanelCategory.Plain;

    public bool AddPanelOnRight = false;
    public bool AddPanelOnLeft = false;
    public bool deletePanel = false;

    // Start 
    void Start()
    {
        RefreshPanels();

        if (GlobalMovmentSettings != null)
        {
            movementSettings = GlobalMovmentSettings.GetComponent<MovmentSettings>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (movementSettings == null)
        {
            return;
        }

        UpdateWallSelectionFromPanels();

        if (Selected)
        {
            if (movementSettings.WidgetMode == 0)
            {
                BasicMovement();
            }
            else if (movementSettings.WidgetMode == 1)
            {
                PanelManipulation();
            }
        }
    }


    // Handle panel manipulation based on movement settings flags
    public void PanelManipulation()
    {
        if (movementSettings != null)
        {
            ChangePanel = movementSettings.ChangePanel;
            NewPanelType = movementSettings.NewPanelType;
            AddPanelOnRight = movementSettings.AddPanelOnRight;
            AddPanelOnLeft = movementSettings.AddPanelOnLeft;
            deletePanel = movementSettings.DeletePanel;
        }
        if (ChangePanel)
        {
            ReplacePanel();
            movementSettings.ChangePanel = false;
        }
        if (AddPanelOnRight)
        {
            AddRight();
            movementSettings.AddPanelOnRight = false;
        }
        if (AddPanelOnLeft)
        {
            AddLeft();
            movementSettings.AddPanelOnLeft = false;
        }
        if (deletePanel)
        {
            DeletePanel();
            movementSettings.DeletePanel = false;
        }
        if (panels.Count == 0)
        {
            Destroy(gameObject);
        }
    }


    // Refresh the list of panels based on current children and sort by localPosition.x
    public void RefreshPanels()
    {
        panels.Clear();

        foreach (Transform child in transform)
        {
            panels.Add(child);
        }

        panels.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        // Assign index to each panel
        for (int i = 0; i < panels.Count; i++)
        {
            WallPanel panel = panels[i].GetComponent<WallPanel>();
            if (panel != null)
            {
                panel.Index = i;
            }
        }
    }


    // Replace panel at index with new type, preserving position and rotation
    public void ReplacePanel()
    {
        // Safety check
        if (panelIndex < 0 || panelIndex >= panels.Count)
        {
            Debug.LogWarning("Invalid panel index");
            return;
        }

        Transform oldPanel = panels[panelIndex];

        // Store transform info BEFORE destroying
        Vector3 localPos = oldPanel.localPosition;
        Quaternion localRot = oldPanel.localRotation;

        // Choose correct prefab
        GameObject prefab = null;

        switch (NewPanelType)
        {
            case PanelCategory.Door:
                prefab = doorPrefab;
                break;
            case PanelCategory.Window:
                prefab = windowPrefab;
                break;
            default:
                prefab = plainWallPrefab;
                break;
        }

        // Destroy old panel
        Destroy(oldPanel.gameObject);

        // Instantiate new panel
        GameObject newPanel = Instantiate(prefab, transform);

        // Restore transform
        newPanel.transform.localPosition = localPos;
        newPanel.transform.localRotation = localRot;

        WallPanel panelScript = newPanel.GetComponent<WallPanel>();
        if (panelScript != null)
        {
            panelScript.Selected = true;
        }


        // Refresh list
        RefreshPanels();
    }



    // Handle Spawning
    public override void SpawnHandles()
    {
        spawnedHandles = new GameObject[2];
        spawnedHandles[0] = CreateHandle(new Vector3(0, Yoffset, XZoffset), Quaternion.identity);
        spawnedHandles[1] = CreateHandle(new Vector3(0, Yoffset, -XZoffset), Quaternion.identity);
    }


    // Handle Panel Addition
    public void AddRight()
    {
        // Safety check
        if (panelIndex < 0 || panelIndex >= panels.Count)
        {
            Debug.LogWarning("Invalid panel index");
            return;
        }

        Transform basePanel = panels[panelIndex];

        // Move all panels to the right of index
        for (int i = panelIndex + 1; i < panels.Count; i++)
        {
            WallPanel panelScript = panels[i].GetComponent<WallPanel>();
            if (panelScript != null)
            {
                panelScript.MovePanelRight(); // calls coroutine wrapper
            }
        }

        // Create new panel to the right of selected index
        Vector3 newLocalPos = basePanel.localPosition + Vector3.right * 1f;

        GameObject newPanel = Instantiate(plainWallPrefab, transform);
        newPanel.transform.localPosition = newLocalPos;
        newPanel.transform.localRotation = basePanel.localRotation;


        // Refresh list
        RefreshPanels();
    }


    // Similar to AddRight but in opposite direction
    public void AddLeft()
    {
        // Safety check
        if (panelIndex < 0 || panelIndex >= panels.Count)
        {
            Debug.LogWarning("Invalid panel index");
            return;
        }

        Transform basePanel = panels[panelIndex];

        // Move all panels to the left of index
        for (int i = 0; i < panelIndex; i++)
        {
            WallPanel panelScript = panels[i].GetComponent<WallPanel>();
            if (panelScript != null)
            {
                panelScript.MovePanelLeft();
            }
        }

        // Create new panel to the left of selected index
        Vector3 newLocalPos = basePanel.localPosition + Vector3.left * 1f;

        GameObject newPanel = Instantiate(plainWallPrefab, transform);
        newPanel.transform.localPosition = newLocalPos;
        newPanel.transform.localRotation = basePanel.localRotation;

        // Refresh list
        RefreshPanels();
    }


    // Handle Panel Deletion
    public void DeletePanel()
    {
        // Safety check
        if (panelIndex < 0 || panelIndex >= panels.Count)
        {
            Debug.LogWarning("Invalid panel index");
            return;
        }

        Transform targetPanel = panels[panelIndex];

        // Get position relative to wall center
        float localX = targetPanel.localPosition.x;

        // Destroy panel
        Destroy(targetPanel.gameObject);

        // Right side → shift left
        if (localX >= 0f)
        {
            for (int i = panelIndex + 1; i < panels.Count; i++)
            {
                WallPanel panelScript = panels[i].GetComponent<WallPanel>();
                if (panelScript != null)
                {
                    panelScript.MovePanelLeft();
                }
            }
        }
        // Left side → shift right
        else
        {
            for (int i = 0; i < panelIndex; i++)
            {
                WallPanel panelScript = panels[i].GetComponent<WallPanel>();
                if (panelScript != null)
                {
                    panelScript.MovePanelRight();
                }
            }
        }

        RefreshPanels();
    }


    // Update wall selection state based on whether any panel is selected
    private void UpdateWallSelectionFromPanels()
    {
        bool anySelected = false;

        foreach (Transform panelTransform in panels)
        {
            if (panelTransform == null) continue;

            WallPanel panel = panelTransform.GetComponent<WallPanel>();
            if (panel != null && panel.Selected)
            {
                anySelected = true;
                break;
            }
        }

        Selected = anySelected;
    }

}