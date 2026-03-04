//----------------------------------------------
//            Simple Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;

/// <summary>
/// Anti roll bars.
/// </summary>
[AddComponentMenu("BoneCracker Games/Simple Car Controller/SCC Antiroll")]
public class SCC_AntiRoll : MonoBehaviour {

    //  Rigidbody.
    private Rigidbody rigid;
    private Rigidbody Rigid {

        get {

            if (rigid == null)
                rigid = GetComponent<Rigidbody>();

            return rigid;

        }

    }

    //  Custom class for wheels.
    [System.Serializable]
    public class Wheels {

        public SCC_Wheel leftWheel;
        public SCC_Wheel rightWheel;
        public float force = 1000f;

    }

    //  All wheels.
    public Wheels[] wheels;

    private void FixedUpdate() {

        //  Getting all wheels for loop.
        for (int i = 0; i < wheels.Length; i++) {

            //  If left and right wheels are selected...
            if (wheels[i].leftWheel && wheels[i].rightWheel) {

                WheelHit wheelHitLeft;
                WheelHit wheelHitRight;

                //  Travel values for left and right wheels.
                float travelFL = 1.0f;
                float travelFR = 1.0f;

                //  Is left wheel grounded?
                bool groundedFL = wheels[i].leftWheel.WheelCollider.GetGroundHit(out wheelHitLeft);

                //  If so, calculate the travel distance. Otherwise distance will be 1.0 (fully extended).
                if (groundedFL)
                    travelFL = (-wheels[i].leftWheel.transform.InverseTransformPoint(wheelHitLeft.point).y - wheels[i].leftWheel.WheelCollider.radius) / wheels[i].leftWheel.WheelCollider.suspensionDistance;

                //  Is right wheel grounded?
                bool groundedFR = wheels[i].rightWheel.WheelCollider.GetGroundHit(out wheelHitRight);

                //  If so, calculate the travel distance. Otherwise distance will be 1.0 (fully extended).
                if (groundedFR)
                    travelFR = (-wheels[i].rightWheel.transform.InverseTransformPoint(wheelHitRight.point).y - wheels[i].rightWheel.WheelCollider.radius) / wheels[i].rightWheel.WheelCollider.suspensionDistance;

                //  Only apply anti-roll when both wheels are grounded.
                if (!groundedFL || !groundedFR)
                    continue;

                //  Calculating the antiroll force.
                float antiRollForce = (travelFL - travelFR) * wheels[i].force;

                //  Clamp to prevent extreme forces.
                antiRollForce = Mathf.Clamp(antiRollForce, -wheels[i].force, wheels[i].force);

                //  Apply forces along world up to prevent angled forces from launching the car.
                Rigid.AddForceAtPosition(Vector3.up * -antiRollForce, wheels[i].leftWheel.transform.position);
                Rigid.AddForceAtPosition(Vector3.up * antiRollForce, wheels[i].rightWheel.transform.position);

            }

        }

    }

}
