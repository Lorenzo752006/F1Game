using UnityEngine;
using UnityEngine.InputSystem;

public class steeringMov : MonoBehaviour
{
    // Maximum steering angle in degrees (F1 cars have limited rotation)
    [SerializeField] private float maxSteeringAngle = 90f;
    // Speed at which the wheel turns
    [SerializeField] private float steeringSpeed = 10f;
    // Current steering angle
    private float currentSteeringAngle = 0f;

    private SCC_InputActions inputActions;

    void OnEnable()
    {
        inputActions = new SCC_InputActions();
        inputActions.Vehicle.Enable();
    }

    void OnDisable()
    {
        inputActions.Vehicle.Disable();
        inputActions.Dispose();
    }

    void Update()
    {
        // Get horizontal input (-1 to 1)
        float input = inputActions.Vehicle.Steering.ReadValue<float>();
        // Target angle based on input
        float targetAngle = input * maxSteeringAngle;
        // Smoothly interpolate to target angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetAngle, Time.deltaTime * steeringSpeed);
        // Apply local rotation around Z axis (typical for steering wheels)
        transform.localRotation = Quaternion.Euler(0f, 0f, -currentSteeringAngle);
    }
}
