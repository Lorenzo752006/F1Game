using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class F1CameraController : MonoBehaviour
{
    public enum CameraView { FirstPerson, TCam, ThirdPerson }

    [Header("Core References")]
    public Rigidbody carRigidbody;
    private Camera cam;

    [Tooltip("The parent transform of the car to align rotations correctly.")]
    public Transform carTransform;

    [Header("Camera Mounts (Create Empty GameObjects on the Car)")]
    public Transform fpvMount;
    public Transform tCamMount;

    [Header("Camera States")]
    public CameraView currentView = CameraView.FirstPerson;

    [Header("First Person / T-Cam Look Settings")]
    public float lookSensitivity = 2f;
    [Tooltip("HANS devices restrict neck movement. Keep these tight.")]
    public float maxLookLeftRight = 60f;
    public float maxLookUp = 20f;
    public float maxLookDown = -15f;
    public float returnDelay = 1.5f;
    public float returnSpeed = 5f;

    [Header("Third Person Settings")]
    public float chaseDistance = 5f;
    public float chaseHeight = 2f;
    public float chaseDamping = 10f;
    [Tooltip("How high above the center of the car the camera looks.")]
    public float lookAtHeightOffset = 1f;

    [Header("Speed Effects - Field of View")]
    public float baseFOV = 60f;
    public float maxFOV = 85f;
    public float topSpeed = 90f;
    public float fovSmoothSpeed = 5f;

    [Header("Speed Effects - Camera Shake")]
    public float maxShakeIntensity = 0.03f;
    public float shakeFrequency = 25f;

    private SCC_InputActions inputActions;

    // Internal state variables
    private float mouseX, mouseY;
    private float timeSinceLastInput = 0f;

    // Tracking variables for the new Third Person Camera
    private float currentLookAngle;
    private float currentHeight;
    private float heightVelocity;
    private float angleVelocity;

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

        // Detach camera from the car in the hierarchy! 
        // We will move it entirely via script to make Third Person smooth.
        transform.parent = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Listen for the "C" key to switch cameras
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SwitchCamera();
        }

        HandleInput();
    }

    void LateUpdate()
    {
        // LateUpdate ensures the car has finished moving in FixedUpdate before we move the camera
        ApplySpeedEffects(out Vector3 shakeOffset, out float fovVal);
        cam.fieldOfView = fovVal;

        switch (currentView)
        {
            case CameraView.FirstPerson:
                UpdateMountedCamera(fpvMount, shakeOffset, 1f);
                break;
            case CameraView.TCam:
                // T-Cam shakes less than First Person (multiplier 0.4)
                UpdateMountedCamera(tCamMount, shakeOffset, 0.4f);
                break;
            case CameraView.ThirdPerson:
                UpdateThirdPersonCamera(shakeOffset);
                break;
        }
    }

    private void SwitchCamera()
    {
        // Cycle through the views
        currentView++;
        if ((int)currentView > 2) currentView = 0; // Loop back to First Person

        // Reset look angles when switching
        mouseX = 0;
        mouseY = 0;
    }

    private void HandleInput()
    {
        bool isRightClicking = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (isRightClicking)
        {
            timeSinceLastInput = 0f;
            Vector2 lookDelta = inputActions.Camera.Orbit.ReadValue<Vector2>();

            mouseX += lookDelta.x * lookSensitivity;
            mouseY -= lookDelta.y * lookSensitivity;

            // Clamp rotation for helmet/TCam
            if (currentView != CameraView.ThirdPerson)
            {
                mouseX = Mathf.Clamp(mouseX, -maxLookLeftRight, maxLookLeftRight);
                mouseY = Mathf.Clamp(mouseY, maxLookDown, maxLookUp);
            }
        }
        else
        {
            timeSinceLastInput += Time.deltaTime;

            if (timeSinceLastInput >= returnDelay)
            {
                mouseX = Mathf.Lerp(mouseX, 0f, Time.deltaTime * returnSpeed);
                mouseY = Mathf.Lerp(mouseY, 0f, Time.deltaTime * returnSpeed);
            }
        }
    }

    private void UpdateMountedCamera(Transform mount, Vector3 shake, float shakeMultiplier)
    {
        if (mount == null) return;

        // Position matches the mount exactly, plus speed shake
        transform.position = mount.position + (shake * shakeMultiplier);

        // CHANGE HERE: Use the mount's rotation instead of the carTransform.rotation.
        // This allows you to tilt the Mount_TCam or Mount_Helmet in the Unity Editor!
        Quaternion baseRotation = mount.rotation;

        // Add the player's mouse look on top of the base rotation
        Quaternion headLook = Quaternion.Euler(mouseY, mouseX, 0f);
        transform.rotation = baseRotation * headLook;
    }

    private void UpdateThirdPersonCamera(Vector3 shake)
    {
        if (carTransform == null) return;

        // 1. Calculate the exact angles we WANT to be at
        float wantedLookAngle = carTransform.eulerAngles.y;
        float wantedHeight = carTransform.position.y + chaseHeight;

        // Apply right-click orbit offset
        if (mouseX != 0)
        {
            wantedLookAngle += mouseX;
        }

        // 2. Smoothly follow the steering (Y rotation) and the height (Y position)
        float smoothTime = 1f / chaseDamping;
        currentLookAngle = Mathf.SmoothDampAngle(currentLookAngle, wantedLookAngle, ref angleVelocity, smoothTime);
        currentHeight = Mathf.SmoothDamp(currentHeight, wantedHeight, ref heightVelocity, smoothTime * 1.5f);

        // 3. Convert that smoothed angle into a rotation direction
        Quaternion currentRotation = Quaternion.Euler(0, currentLookAngle, 0);

        // 4. Set the position EXACTLY at the car's location, then pull it back using our smoothed rotation
        Vector3 finalPosition = carTransform.position;
        finalPosition -= currentRotation * Vector3.forward * chaseDistance;

        // 5. Apply our smoothed height
        finalPosition.y = currentHeight;

        // Apply the position and speed shake
        transform.position = finalPosition + (shake * 0.1f);

        // 6. Look at a stable point slightly above the car
        Vector3 lookTarget = carTransform.position + (Vector3.up * lookAtHeightOffset);
        transform.LookAt(lookTarget);
    }

    private void ApplySpeedEffects(out Vector3 shakeOffset, out float fovVal)
    {
        shakeOffset = Vector3.zero;
        fovVal = baseFOV;

        if (carRigidbody == null) return;

        float currentSpeed = carRigidbody.linearVelocity.magnitude;
        float speedPercent = Mathf.Clamp01(currentSpeed / topSpeed);

        // --- 1. Dynamic FOV ---
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedPercent);
        fovVal = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);

        // --- 2. Camera Shake ---
        if (currentSpeed > 1f)
        {
            float shakeX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f) * maxShakeIntensity * speedPercent;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f) * maxShakeIntensity * speedPercent;
            shakeOffset = new Vector3(shakeX, shakeY, 0f);
        }
    }
}