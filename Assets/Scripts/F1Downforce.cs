using UnityEngine;

/// <summary>
/// Simulates aerodynamic downforce for an F1 car.
/// Downforce scales with the square of speed, pushing the car onto the track
/// for increased grip at high speeds. Force is applied at configurable front
/// and rear positions so you can tune aero balance (understeer / oversteer).
/// </summary>
[AddComponentMenu("F1/F1 Downforce")]
[RequireComponent(typeof(Rigidbody))]
public class F1Downforce : MonoBehaviour
{
    private Rigidbody rigid;

    [Header("Downforce Settings")]
    [Tooltip("Total downforce coefficient. Higher = more downforce. Real F1 cars produce ~3.5x their weight at top speed.")]
    public float downforceCoefficient = 3.5f;

    [Tooltip("Speed (m/s) at which the car produces maximum downforce. Above this, downforce is capped.")]
    public float referenceSpeed = 90f;

    [Header("Aero Balance")]
    [Tooltip("0 = all downforce on rear, 1 = all on front, 0.45 = realistic F1 balance (slightly rear-biased).")]
    [Range(0f, 1f)]
    public float frontAeroBalance = 0.45f;

    [Header("Force Application Points (local space)")]
    [Tooltip("Local position where front downforce is applied. Set this near the front axle.")]
    public Vector3 frontForcePoint = new Vector3(0f, 0f, 1.5f);

    [Tooltip("Local position where rear downforce is applied. Set this near the rear axle.")]
    public Vector3 rearForcePoint = new Vector3(0f, 0f, -1.5f);

    [Header("Drag")]
    [Tooltip("Aerodynamic drag coefficient. Adds realistic air resistance that increases with speed squared.")]
    public float dragCoefficient = 0.8f;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rigid == null) return;

        float speed = rigid.linearVelocity.magnitude;

        // Downforce scales with speed^2 (like real aerodynamics), capped at reference speed
        float speedRatio = Mathf.Clamp01(speed / referenceSpeed);
        float speedSquared = speedRatio * speedRatio;

        // Total downforce in Newtons: coefficient * weight * speedRatio^2
        float totalDownforce = downforceCoefficient * rigid.mass * 9.81f * speedSquared;

        // Split between front and rear based on aero balance
        float frontForce = totalDownforce * frontAeroBalance;
        float rearForce = totalDownforce * (1f - frontAeroBalance);

        // Apply downforce at the specified local positions, pushing the car down
        Vector3 worldFrontPoint = transform.TransformPoint(frontForcePoint);
        Vector3 worldRearPoint = transform.TransformPoint(rearForcePoint);

        rigid.AddForceAtPosition(-transform.up * frontForce, worldFrontPoint);
        rigid.AddForceAtPosition(-transform.up * rearForce, worldRearPoint);

        // Aerodynamic drag: opposes velocity, scales with speed^2
        if (speed > 0.5f)
        {
            Vector3 dragForce = -rigid.linearVelocity.normalized * dragCoefficient * speed * speed;
            // Clamp drag so it never exceeds the force needed to stop the car in one frame
            float maxDrag = rigid.mass * speed / Time.fixedDeltaTime;
            if (dragForce.magnitude > maxDrag)
                dragForce = dragForce.normalized * maxDrag;

            rigid.AddForce(dragForce);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize force application points in the editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(frontForcePoint), 0.15f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(rearForcePoint), 0.15f);
    }
}
