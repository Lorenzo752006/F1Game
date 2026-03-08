//----------------------------------------------
//            Simple Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SCC_Inputs {

    public float throttleInput;
    public float steerInput;
    public float brakeInput;
    public float handbrakeInput;

    // New: camera change request (pressed this frame)
    public bool cameraChangePressed;

    // New: camera orbit input (right stick / mouse delta)
    public Vector2 cameraOrbit;

}
