//----------------------------------------------
//            Debug Helper
//  Attach to the car root to diagnose physics issues.
//----------------------------------------------

using UnityEngine;

[AddComponentMenu("BoneCracker Games/Simple Car Controller/SCC Debug Info")]
public class SCC_DebugInfo : MonoBehaviour {

    private Rigidbody rigid;
    private SCC_Wheel[] wheels;

    private void Start() {

        rigid = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<SCC_Wheel>();

        Debug.Log("===== SCC DEBUG START =====");
        Debug.Log("Vehicle: " + gameObject.name);
        Debug.Log("Transform Scale: " + transform.lossyScale);
        Debug.Log("Rigidbody Mass: " + rigid.mass);
        Debug.Log("Rigidbody COM (local): " + rigid.centerOfMass);
        Debug.Log("Rigidbody COM (world): " + rigid.worldCenterOfMass);

        //  Log all colliders on the vehicle.
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        Debug.Log("Total Colliders on vehicle: " + allColliders.Length);

        foreach (Collider col in allColliders) {

            if (col is WheelCollider wc) {

                Debug.Log("[WheelCollider] " + col.gameObject.name +
                    " | Pos: " + col.transform.position +
                    " | LocalPos: " + col.transform.localPosition +
                    " | Radius: " + wc.radius +
                    " | SuspDist: " + wc.suspensionDistance +
                    " | Spring: " + wc.suspensionSpring.spring +
                    " | Damper: " + wc.suspensionSpring.damper +
                    " | ForceAppPoint: " + wc.forceAppPointDistance +
                    " | Center: " + wc.center);

            } else {

                Debug.Log("[" + col.GetType().Name + "] " + col.gameObject.name +
                    " | Pos: " + col.transform.position +
                    " | Bounds: " + col.bounds.size +
                    " | IsTrigger: " + col.isTrigger);

            }

        }

        //  Check for scale issues.
        if (transform.lossyScale != Vector3.one)
            Debug.LogWarning("WARNING: Vehicle scale is NOT (1,1,1). This causes WheelCollider issues! Scale: " + transform.lossyScale);

        //  Check wheel positions relative to body.
        foreach (SCC_Wheel w in wheels) {

            float distFromBody = w.transform.localPosition.y;
            Debug.Log("[Wheel Position] " + w.gameObject.name +
                " | LocalY: " + distFromBody +
                " | Bottom of wheel (world Y): " + (w.transform.position.y - w.WheelCollider.radius));

        }

        Debug.Log("===== SCC DEBUG END =====");

    }

    private void FixedUpdate() {

        //  Log forces for first 2 seconds.
        if (Time.time < 2f) {

            Debug.Log("T=" + Time.time.ToString("F3") +
                " | Speed: " + (rigid.linearVelocity.magnitude * 3.6f).ToString("F1") + " km/h" +
                " | Velocity: " + rigid.linearVelocity +
                " | AngVel: " + rigid.angularVelocity +
                " | Y-Pos: " + transform.position.y.ToString("F3"));

        }

    }

    private void OnGUI() {

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("Y-Pos: " + transform.position.y.ToString("F3"));
        GUILayout.Label("Velocity: " + rigid.linearVelocity);
        GUILayout.Label("Speed: " + (rigid.linearVelocity.magnitude * 3.6f).ToString("F1") + " km/h");
        GUILayout.Label("Scale: " + transform.lossyScale);

        foreach (SCC_Wheel w in wheels) {

            GUILayout.Label(w.gameObject.name +
                " grounded: " + w.isGrounded +
                " | rpm: " + w.WheelCollider.rpm.ToString("F0"));

        }

        GUILayout.EndArea();

    }

}