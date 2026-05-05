using UnityEngine;
using System.Collections;

public class Widget : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public GameObject movementWidget;
    public GameObject panelWidget;
    public GameObject creationWidget;

    [Header("Movement Settings")]
    public GameObject GlobalMovmentSettings;
    private MovmentSettings movementSettings;
    private int WidgetMode = 0; // 0 = movement, 1 = panel, 2 = creation

    private GameObject currentWidget;
    private bool isOn = false;
    public float distanceInFront = 1.0f;
    public float rotationOffsetY = 45f; 
    public float verticalOffset = -0.2f;

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
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            isOn = !isOn;

            if (!isOn)
            {
                currentWidget.SetActive(false);
                return;
            }

            if (movementSettings != null)
            {
                WidgetMode = movementSettings.WidgetMode;
            }

            // Switch widget based on mode
            switch (WidgetMode) 
            { 
                case 0:
                    currentWidget = movementWidget;
                    break;
                case 1:
                    currentWidget = panelWidget;
                    break;
                case 2:
                    currentWidget = creationWidget;
                    break;
            }

            // Position and orient the widget in front of the camera
            if (currentWidget != null && cameraTransform != null)
            {

                Vector3 forward = cameraTransform.forward;
                forward = Quaternion.Euler(0, rotationOffsetY, 0) * forward;

                // Set position in front of camera with vertical offset
                Vector3 targetPos = cameraTransform.position + forward * distanceInFront;
                targetPos.y += verticalOffset; // apply vertical adjustment
                currentWidget.transform.position = targetPos;

                // Make widget face the camera
                currentWidget.transform.LookAt(cameraTransform);
                currentWidget.transform.Rotate(0, 180f, 0);
            }

            if (currentWidget != null)
            {
                currentWidget.SetActive(true);
            }
        }
    }
}
