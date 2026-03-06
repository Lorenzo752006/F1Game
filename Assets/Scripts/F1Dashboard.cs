using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class F1Dashboard : MonoBehaviour
{
    [Header("Car Data Sources")]
    public Rigidbody carRigidbody;
    public int currentGear = 1;
    public float currentRPM = 4000f;

    [Header("Engine & Gear Settings")]
    public bool useAutomaticGears = true;
    public float maxSpeedKmh = 320f;
    public int totalGears = 8;
    public float idleRPM = 800f;
    public float redlineRPM = 12000f;
    public float rpmSmoothing = 8f;

    [Header("Shift Light Settings")]
    [Tooltip("Drag all your individual LED Image objects here.")]
    public Image[] shiftLightLEDs;
    public float minLightRPM = 8000f;
    public float optimalShiftRPM = 11500f;
    public Gradient shiftLightColors;
    [Tooltip("The color of the LEDs when they are turned off.")]
    public Color ledOffColor = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Dark, semi-transparent gray

    [Header("Camera Reference")]
    public F1CameraController cameraController;

    [Header("Steering Wheel (Always Active)")]
    public TMP_Text wheelGearText;

    [Header("FPV Displays (Chassis)")]
    public GameObject fpvDisplayContainer;
    public TMP_Text fpvSpeedText;
    public TMP_Text fpvRpmText;

    [Header("TCam Displays (Crown)")]
    public GameObject tCamDisplayContainer;
    public TMP_Text tCamSpeedText;
    public TMP_Text tCamRpmText;

    [Header("3rd Person Displays (Overlay)")]
    public GameObject thirdPersonContainer;
    public TMP_Text thirdPersonSpeedText;
    public TMP_Text thirdPersonGearText;
    public TMP_Text thirdPersonRpmText;

    void Update()
    {
        UpdateDashboardData();
        HandleDisplayVisibility();
        UpdateShiftLights();
    }

    private void UpdateShiftLights()
    {
        // Safety check to make sure we actually assigned LEDs in the inspector
        if (shiftLightLEDs == null || shiftLightLEDs.Length == 0) return;

        // Calculate how far along the RPM range we are (0.0 to 1.0)
        float currentLightRange = Mathf.Clamp(currentRPM - minLightRPM, 0, redlineRPM - minLightRPM);
        float maxLightRange = redlineRPM - minLightRPM;

        float fillPercentage = 0f;
        if (maxLightRange > 0)
        {
            fillPercentage = currentLightRange / maxLightRange;
        }

        // Figure out exactly how many LEDs should be turned on based on the percentage
        int ledsToTurnOn = Mathf.RoundToInt(fillPercentage * shiftLightLEDs.Length);

        // Are we above the optimal shift point? 
        bool isFlashing = currentRPM >= optimalShiftRPM;
        bool flashState = Mathf.PingPong(Time.time * 20f, 1f) > 0.5f;

        // Loop through every LED in our array
        for (int i = 0; i < shiftLightLEDs.Length; i++)
        {
            if (isFlashing)
            {
                // Flashing mode: Alternate between Blue and Off
                shiftLightLEDs[i].color = flashState ? Color.blue : ledOffColor;
            }
            else
            {
                // Normal mode: Turn on LEDs up to our target number
                if (i < ledsToTurnOn)
                {
                    // Evaluate the gradient based on the LED's position in the row
                    float ledPosition = (float)i / (shiftLightLEDs.Length - 1);
                    shiftLightLEDs[i].color = shiftLightColors.Evaluate(ledPosition);
                }
                else
                {
                    // Turn off the LEDs we haven't reached yet
                    shiftLightLEDs[i].color = ledOffColor;
                }
            }
        }
    }

    private void UpdateDashboardData()
    {
        if (carRigidbody == null) return;

        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        string speedStr = Mathf.FloorToInt(speedKmh).ToString();

        if (useAutomaticGears)
        {
            if (maxSpeedKmh <= 0f) maxSpeedKmh = 1f;
            if (totalGears < 1) totalGears = 1;

            float norm = Mathf.Clamp01(speedKmh / maxSpeedKmh);
            int gear = Mathf.Clamp(Mathf.FloorToInt(norm * totalGears) + 1, 1, totalGears);
            currentGear = gear;
        }

        float gearRange = 1f / Mathf.Max(1, totalGears);
        float gearStartRatio = (currentGear - 1) * gearRange;
        float gearEndRatio = currentGear * gearRange;

        float gearStartSpeed = gearStartRatio * maxSpeedKmh;
        float gearEndSpeed = gearEndRatio * maxSpeedKmh;

        float gearNormalized = 0f;
        if (gearEndSpeed - gearStartSpeed > 0.0001f)
            gearNormalized = Mathf.InverseLerp(gearStartSpeed, gearEndSpeed, speedKmh);

        float targetRPM = Mathf.Lerp(idleRPM, redlineRPM, Mathf.Clamp01(gearNormalized));

        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * rpmSmoothing);
        string rpmStr = Mathf.FloorToInt(currentRPM).ToString();

        string gearStr = currentGear.ToString();
        if (currentGear == 0) gearStr = "N";
        if (currentGear == -1) gearStr = "R";

        if (wheelGearText != null) wheelGearText.text = gearStr;
        if (fpvSpeedText != null) fpvSpeedText.text = speedStr;
        if (fpvRpmText != null) fpvRpmText.text = rpmStr;
        if (tCamSpeedText != null) tCamSpeedText.text = speedStr;
        if (tCamRpmText != null) tCamRpmText.text = rpmStr;

        if (thirdPersonSpeedText != null) thirdPersonSpeedText.text = speedStr + " KM/H";
        if (thirdPersonGearText != null) thirdPersonGearText.text = "GEAR: " + gearStr;
        if (thirdPersonRpmText != null) thirdPersonRpmText.text = "RPM: " + rpmStr;
    }

    private void HandleDisplayVisibility()
    {
        if (cameraController == null) return;

        if (fpvDisplayContainer != null) fpvDisplayContainer.SetActive(false);
        if (tCamDisplayContainer != null) tCamDisplayContainer.SetActive(false);
        if (thirdPersonContainer != null) thirdPersonContainer.SetActive(false);

        switch (cameraController.currentView)
        {
            case F1CameraController.CameraView.FirstPerson:
                if (fpvDisplayContainer != null) fpvDisplayContainer.SetActive(true);
                break;
            case F1CameraController.CameraView.TCam:
                if (tCamDisplayContainer != null) tCamDisplayContainer.SetActive(true);
                break;
            case F1CameraController.CameraView.ThirdPerson:
                if (thirdPersonContainer != null) thirdPersonContainer.SetActive(true);
                break;
        }
    }
}