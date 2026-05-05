using UnityEngine;

public class Furniture : BasicObject
{

    void Start()
    {
        if (GlobalMovmentSettings != null)
        {
            movementSettings = GlobalMovmentSettings.GetComponent<MovmentSettings>();
        }
    }

    void Update()
    {
        if (movementSettings == null)
        {
            return;
        }

        BasicMovement();
    }
}