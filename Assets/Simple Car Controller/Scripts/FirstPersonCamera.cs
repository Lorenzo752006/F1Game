using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The Rigidbody of your F1 car to calculate speed.")]
    public Rigidbody carRigidbody;
    private Camera cam;

    [Header("Look Settings")]
    public float lookSensitivity = 2f;
    [Tooltip("HANS devices restrict neck movement. Keep these tight.")]
    public float maxLookLeftRight = 60f;
    public float maxLookUp = 20f;
    public float maxLookDown = -15f;

    [Header("Speed Effects - Field of View")]
    public float baseFOV = 60f;
    public float maxFOV = 85f;
    [Tooltip("Speed (in meters per second) where max FOV is reached. 90m/s is roughly 324km/h.")]
    public float topSpeed = 90f;
    public float fovSmoothSpeed = 5f;

    [Header("Speed Effects - Camera Shake")]
    [Tooltip("How violent the shake gets at top speed.")]
    public float maxShakeIntensity = 0.03f;
    [Tooltip("How fast the vibration rattles.")]
    public float shakeFrequency = 25f;

    private SCC_InputActions inputActions;

    // Internal state variables
    private float mouseX, mouseY;
    private Vector3 originalLocalPosition;

    void OnEnable()
    {
        inputActions = new SCC_InputActions();
        inputActions.Camera.Enable();
    }

    void OnDisable()
    {
        inputActions.Camera.Disable();
        inputActions.Dispose();
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        originalLocalPosition = transform.localPosition;

        // Lock the cursor to the center of the screen for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleHeadLook();
    }

    void LateUpdate()
    {
        // LateUpdate is best for camera movement to prevent jittering
        ApplySpeedEffects();
    }

    private void HandleHeadLook()
    {
        // Gather mouse input via new Input System
        Vector2 lookDelta = inputActions.Camera.Orbit.ReadValue<Vector2>();
        mouseX += lookDelta.x * lookSensitivity;
        mouseY -= lookDelta.y * lookSensitivity;

        // Clamp the rotation to simulate the HANS device constraints
        mouseX = Mathf.Clamp(mouseX, -maxLookLeftRight, maxLookLeftRight);
        mouseY = Mathf.Clamp(mouseY, maxLookDown, maxLookUp);

        // Apply rotation
        transform.localRotation = Quaternion.Euler(mouseY, mouseX, 0f);
    }

    private void ApplySpeedEffects()
    {
        if (carRigidbody == null) return;

        // Get the car's forward speed (magnitude is meters per second)
        float currentSpeed = carRigidbody.linearVelocity.magnitude;

        // --- 1. Dynamic FOV ---
        // Calculate the percentage of our top speed (0.0 to 1.0)
        float speedPercent = Mathf.Clamp01(currentSpeed / topSpeed);

        // Find our target FOV based on speed
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedPercent);

        // Smoothly transition to the target FOV
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);

        // --- 2. Camera Shake ---
        // Only shake if we are actually moving
        if (currentSpeed > 1f)
        {
            // Use Perlin Noise for organic, random vibration
            float shakeOffsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f) * maxShakeIntensity * speedPercent;
            float shakeOffsetY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f) * maxShakeIntensity * speedPercent;

            // Apply the offset on top of the original local position
            transform.localPosition = originalLocalPosition + new Vector3(shakeOffsetX, shakeOffsetY, 0f);
        }
        else
        {
            // Reset position when stopped
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * 5f);
        }
    }
}