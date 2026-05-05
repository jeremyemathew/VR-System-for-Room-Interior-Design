using UnityEngine;

public class MovmentSettings : MonoBehaviour
{
    public int MovementMode = 0; // 0 = normal, 1 = fine, 2 = snap
    public int WidgetMode = 0; // 0 = movement, 1 = panel, 2 = creation
    public GameObject myDisabledObject;
    public bool Rotating = false;
    public float RotateAngle = 0.0f;
    public bool SelectWall = false;
    public bool ChangePanel = false;
    public bool AddPanelOnRight = false;
    public bool AddPanelOnLeft = false;
    public bool DeletePanel = false;
    public Wall.PanelCategory NewPanelType = Wall.PanelCategory.Plain;
    public bool CreateWall = false;

    private void Update()
    {
        if (myDisabledObject == null) return;

        bool shouldBeActive = (MovementMode == 2);

        if (myDisabledObject.activeSelf != shouldBeActive)
        {
            myDisabledObject.SetActive(shouldBeActive);
        }
    }

    // Button particle and main target reset
    public void OnNormalPressed()
    {
        MovementMode = 0;
    }

    public void OnFinePressed()
    {
        MovementMode = 1;
    }

    public void OnGridPressed()
    {
        MovementMode = 2;
    }

    public void OnRotatePressed()
    {
        Rotating = true;
    }

    public void OnRotateSlide(float t)
    {
        RotateAngle = t * 360.0f;
    }

    public void OnSelectWallPressed()
    {
        WidgetMode = 0;
    }
    public void onPlainPanelPressed()
    {
        NewPanelType = Wall.PanelCategory.Plain;
        ChangePanel = true;
    }
    public void onDoorPanelPressed()
    {
        NewPanelType = Wall.PanelCategory.Door;
        ChangePanel = true;
    }
    public void onWindowPanelPressed()
    {
        NewPanelType = Wall.PanelCategory.Window;
        ChangePanel = true;
    }
    public void onAddPanelRightPressed()
    {
        AddPanelOnRight = true;
    }
    public void onAddPanelLeftPressed()
    {
        AddPanelOnLeft = true;
    }
    public void onDeletePanelPressed()
    {
        DeletePanel = true;
    }
    public void onCreateWallPressed()
    {
        CreateWall = true;
    }
}
