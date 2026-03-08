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
using UnityEngine.InputSystem;

/// <summary>
/// Input receiver through the Unity's new Input System.
/// </summary>
public class SCC_InputManager : SCC_Singleton<SCC_InputManager> {

    public SCC_Inputs inputs;       //  Actual inputs.
    private static SCC_InputActions inputActions;

    private void Awake() {

        //  Hiding this gameobject in the hierarchy.
        gameObject.hideFlags = HideFlags.HideInHierarchy;

        //  Creating inputs.
        inputs = new SCC_Inputs();

    }

    private void Update() {

        //  Creating inputs.
        if (inputs == null)
            inputs = new SCC_Inputs();

        //  Receive inputs from the controller.
        GetInputs();

    }

    /// <summary>
    /// Gets all inputs and registers button events.
    /// </summary>
    /// <returns></returns>
    public void GetInputs() {

        if (inputActions == null) {

            inputActions = new SCC_InputActions();
            inputActions.Enable();

        }

        inputs.throttleInput = inputActions.Vehicle.Throttle.ReadValue<float>();
        inputs.brakeInput = inputActions.Vehicle.Brake.ReadValue<float>();
        inputs.steerInput = inputActions.Vehicle.Steering.ReadValue<float>();
        inputs.handbrakeInput = inputActions.Vehicle.Handbrake.ReadValue<float>();

        // New: camera change - detect D-pad down or keyboard C as a one-frame press
        bool camPressed = false;

        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            camPressed = true;

        var gp = Gamepad.current;
        if (!camPressed && gp != null && gp.dpad != null && gp.dpad.down.wasPressedThisFrame)
            camPressed = true;

        inputs.cameraChangePressed = camPressed;

        // New: camera orbit using right stick OR mouse delta
        // Requirement: ignore mouse input unless the right mouse button is held.
        Vector2 orbit = Vector2.zero;

        // Prefer gamepad right stick if it exists and is active
        if (gp != null)
        {
            Vector2 gpOrbit = gp.rightStick.ReadValue();
            // consider deadzone
            if (gpOrbit.sqrMagnitude > 0.0001f)
            {
                orbit = gpOrbit;
            }
        }

        // Only sample mouse delta if right mouse button is held
        var mouse = Mouse.current;
        if (orbit.sqrMagnitude <= 0.0001f && mouse != null && mouse.rightButton.isPressed)
        {
            orbit = mouse.delta.ReadValue();
        }

        inputs.cameraOrbit = orbit;

    }

}
