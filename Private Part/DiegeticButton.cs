using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class DiegeticButton : Interactable
{
    public enum LocalAxis { X, Y, Z }

    [Header("Button Motion Direction")]
    public LocalAxis pressAxis = LocalAxis.Y;

    [Tooltip("How far the cap can move when fully pressed (in parent-local units).")]
    public float maxPressDepth = 0.02f;   // 2 cm if your parent is 1 unit = 1 meter

    [Tooltip("Ignore tiny touch jitter (in parent-local units).")]
    public float deadZone = 0.003f;    // 3 mm

    [Range(0.1f, 0.95f)]
    [Tooltip("How far (0..1) you must press before the button *arms*.")]
    public float pressThreshold = 0.7f;

    [Tooltip("How quickly the button returns to rest when released.")]
    public float springSpeed = 25f;

    [Tooltip("Allowed minimum time between presses.")]
    public float cooldown = 0.15f;

    [Header("Haptics")]
    public bool haptics = true;
    public float armedTickAmplitude = 0.15f;
    public float armedTickDuration = 0.015f;
    public float clickAmplitude = 0.40f;
    public float clickDuration = 0.035f;

    [Header("Events")]
    public UnityEvent onPressed; // call using onPressed?.Invoke();

    // Stored rest position of the button cap (local to parent).
    Vector3 restLocalPos;

    // Which controller is currently touching this button cap?
    OVRController touchingCtrl;

    // How deep are we *currently* pressed (local units)? 0..maxPressDepth
    float pressDepth;

    // "Armed" means we crossed the threshold and will fire event on release.
    bool armed;
    bool armedTicked;
    float nextAllowedTime;

    void Awake()
    {
        // Set restLocalPos to remember where the cap starts (its rest position).
        restLocalPos = transform.localPosition;

        // Kinematic RB is a nice stable setup for trigger-based interaction.
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (touchingCtrl != null) // If we are touching button cap
        {
            // --- TODO - Convert controller position into the buttonCap PARENT'S local space ---
            // this makes the math simple and avoids all world/local scaling issues.
            Vector3 controllerLocalPosition = transform.parent.InverseTransformPoint(touchingCtrl.transform.position);

            // --- TODO - Compute how far "into" the button the controller is ---
            // the button travels in the NEGATIVE press axis direction:
            // So the press direction is (-axisLocal).
            Vector3 axisLocal = GetAxisLocal();
            Vector3 pressDirection = -axisLocal;
            // --- TODO - Vector from the cap's rest position to the controller, in parent-local space
            Vector3 toController = controllerLocalPosition - restLocalPos;

            // --- TODO - Signed distance along press direction (use dot product) 
            float signedDistance = Vector3.Dot(toController, pressDirection);

            // --- TODO - "Apply deadzone" (see hint), clamp signed distance to [0..maxPressDepth] and
            // update pressDepth
            // Hint: first subtract deadZone from signed distance then clamp this distance 
            signedDistance -= deadZone;
            pressDepth = Mathf.Clamp(signedDistance, 0f, maxPressDepth);

            // --- TODO - Arm when deep enough (but DO NOT "fire" yet - that will happen in OnTouchExit)
            // Before arming, make sure not already armed,
            // make sure Time.time > next allowed time,
            // and make sure pressDepth >= maxPressDepth * pressThreshold 
            // Also, If haptics turned on, fire off hapticTick
            if (!armed && Time.time >= nextAllowedTime && pressDepth >= maxPressDepth * pressThreshold)
            {
                armed = true;

                if (haptics && !armedTicked)
                {
                    touchingCtrl?.HapticTick(armedTickAmplitude, armedTickDuration);
                    armedTicked = true;
                }
            }
        }
        else // Not touching: spring back toward rest. 
        {
            // --- TODO - use Mathf.Movetowards to move pressDepth back to 0 depth
            // delta is given by springSpeed * Time.deltaTime
            pressDepth = Mathf.MoveTowards(pressDepth, 0f, springSpeed * Time.deltaTime);

            // --- TODO - If pressDepth is <= 0.005f, reset armed and armedTick
            if (pressDepth <= 0.005f)
            {
                armed = false;
                armedTicked = false;
            }
        }

        // --- TODO - Apply the button cap motion (i.e. update cap LOCAL position
        // Hint: use restLocalPos and subtract how far it was pressed along pressAxis
        Vector3 axisLocalFinal = GetAxisLocal();
        transform.localPosition = restLocalPos - axisLocalFinal * pressDepth;
    }

    public override void OnTouchEnter(OVRController ctrl)
    {
        // Track the controller that is touching.
        touchingCtrl = ctrl;

        // Reset press state.
        armed = false;
        armedTicked = false;
    }

    public override void OnTouchExit(OVRController ctrl)
    {
        // Only respond if the controller leaving is the one we were tracking.
        if (ctrl != touchingCtrl) return;

        // --- TODO - Fire on RELEASE only if we were armed and Time.time >= nextAllowedTime.
        // Also - update nextAllowedTime based on current time plus coolDown
        //      - if haptics turned on, send HapticClick
        if (armed && Time.time >= nextAllowedTime)
        {
            onPressed?.Invoke();
            nextAllowedTime = Time.time + cooldown;

            if (haptics)
            {
                ctrl?.HapticClick(clickAmplitude, clickDuration);
            }
        }
        // Stop tracking controller. Update() will spring back.
        touchingCtrl = null;
        armed = false;
        armedTicked = false;
    }

    Vector3 GetAxisLocal()
        => pressAxis == LocalAxis.X ? Vector3.right
         : pressAxis == LocalAxis.Y ? Vector3.up
         : Vector3.forward;
}

