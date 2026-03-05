using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DynamicRacingLine : MonoBehaviour
{
    [Header("Core References")]
    public RoadSpline targetSpline;
    public Rigidbody playerCar;

    [Header("Line Settings")]
    public float lineWidth = 1.5f;
    public float heightOffset = 0.2f;
    public int drawDistance = 40;

    [Header("Color Settings")]
    public Color tooSlowColor = Color.blue;
    public Color optimalColor = Color.yellow;
    public Color tooFastColor = Color.red;
    public float colorSensitivity = 10f;

    [Header("Arrow Visuals")]
    [Tooltip("How many arrows appear per meter. Adjust this so they don't look stretched.")]
    public float arrowsPerMeter = 0.5f;
    [Tooltip("How fast the arrows scroll along the track.")]
    public float scrollSpeed = -2f;

    [Header("Track Analysis Tuning")]
    public float maxSpeed = 90f;
    [Tooltip("The speed the car should take the absolute sharpest turns.")]
    public float minCornerSpeed = 15f;
    [Tooltip("The angle (in degrees) between waypoints that counts as a severe hairpin. This depends on how many waypoints your spline generates!")]
    public float maxCornerAngle = 25f;

    private LineRenderer lineRenderer;
    private List<Vector3> allTrackPoints = new List<Vector3>();
    private List<float> safeSpeeds = new List<float>();
    private Material lineMat;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;

        // This is the magic setting that allows the arrow texture to repeat instead of stretch!
        lineRenderer.textureMode = LineTextureMode.Tile;

        // Cache the material so we can animate it without causing memory leaks
        lineMat = lineRenderer.material;

        AnalyzeTrack();
    }

    void AnalyzeTrack()
    {
        if (targetSpline == null) return;

        for (int s = 0; s < targetSpline.SegmentCount; s++)
        {
            for (int i = 0; i <= targetSpline.samplesPerSegment; i++)
            {
                if (s > 0 && i == 0) continue;
                float t = i / (float)targetSpline.samplesPerSegment;
                Vector3 pt = targetSpline.GetPointOnSegment(s, t) + Vector3.up * heightOffset;
                allTrackPoints.Add(pt);
            }
        }

        // Calculate "Safe Speed" based on the actual degree of the corners
        for (int i = 0; i < allTrackPoints.Count; i++)
        {
            Vector3 prev = allTrackPoints[Mathf.Max(0, i - 1)];
            Vector3 curr = allTrackPoints[i];
            Vector3 next = allTrackPoints[Mathf.Min(allTrackPoints.Count - 1, i + 1)];

            // Get the direction we are driving into the point, and out of the point
            Vector3 dirIn = (curr - prev).normalized;
            Vector3 dirOut = (next - curr).normalized;

            // Find the exact angle in degrees between those two directions
            float cornerAngle = Vector3.Angle(dirIn, dirOut);

            // Calculate how "severe" the turn is on a scale from 0.0 (straight) to 1.0 (max hairpin)
            float severity = Mathf.Clamp01(cornerAngle / maxCornerAngle);

            // We multiply severity by itself (squaring it). 
            // This is a math trick so gentle 5-degree bends barely slow you down, 
            // but the moment it gets sharp, the required speed drops dramatically.
            severity = severity * severity;

            // Blend smoothly between your top speed and your slowest hairpin speed based on the severity
            float safeSpeed = Mathf.Lerp(maxSpeed, minCornerSpeed, severity);

            safeSpeeds.Add(safeSpeed);
        }

        SmoothBrakingZones();
    }

    void SmoothBrakingZones()
    {
        float[] smoothed = new float[safeSpeeds.Count];
        int blurRadius = 8;

        for (int i = 0; i < safeSpeeds.Count; i++)
        {
            float sum = 0;
            int count = 0;
            for (int j = -blurRadius; j <= blurRadius; j++)
            {
                int index = i + j;
                if (index >= 0 && index < safeSpeeds.Count)
                {
                    sum += safeSpeeds[index];
                    count++;
                }
            }
            smoothed[i] = sum / count;
        }
        safeSpeeds = new List<float>(smoothed);
    }

    void Update()
    {
        if (playerCar == null || allTrackPoints.Count == 0 || lineRenderer == null) return;

        int nearestIndex = 0;
        float minDist = float.MaxValue;
        Vector3 carPos = playerCar.position;

        for (int i = 0; i < allTrackPoints.Count; i++)
        {
            float dist = Vector3.SqrMagnitude(carPos - allTrackPoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }

        List<Vector3> drawPoints = new List<Vector3>();
        List<float> drawSpeeds = new List<float>();

        for (int i = 0; i < drawDistance; i++)
        {
            int index = nearestIndex + i;
            if (index >= allTrackPoints.Count)
            {
                if (targetSpline.closed) index = index % allTrackPoints.Count;
                else break;
            }
            drawPoints.Add(allTrackPoints[index]);
            drawSpeeds.Add(safeSpeeds[index]);
        }

        lineRenderer.positionCount = drawPoints.Count;
        lineRenderer.SetPositions(drawPoints.ToArray());

        UpdateLineColors(drawSpeeds);
        AnimateArrows(drawPoints);
    }

    void UpdateLineColors(List<float> upcomingSafeSpeeds)
    {
        float currentSpeed = playerCar.linearVelocity.magnitude;
        Gradient gradient = new Gradient();

        int keyCount = Mathf.Min(8, upcomingSafeSpeeds.Count);
        GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        int step = upcomingSafeSpeeds.Count / Mathf.Max(1, (keyCount - 1));

        for (int i = 0; i < keyCount; i++)
        {
            int speedIndex = Mathf.Min(i * step, upcomingSafeSpeeds.Count - 1);
            float targetSafeSpeed = upcomingSafeSpeeds[speedIndex];
            float speedDiff = currentSpeed - targetSafeSpeed;

            Color pointColor;

            // --- The Smooth Blending Math ---
            if (speedDiff > 0)
            {
                // Going too fast: Blend smoothly from Yellow to Red
                float t = Mathf.Clamp01(speedDiff / colorSensitivity);
                pointColor = Color.Lerp(optimalColor, tooFastColor, t);
            }
            else
            {
                // Going optimal or slow: Blend smoothly from Yellow to Blue
                float t = Mathf.Clamp01(-speedDiff / colorSensitivity);
                pointColor = Color.Lerp(optimalColor, tooSlowColor, t);
            }

            float time = i / (float)(keyCount - 1);
            colorKeys[i] = new GradientColorKey(pointColor, time);
        }

        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = gradient;
    }

    void AnimateArrows(List<Vector3> drawnPoints)
    {
        if (lineMat == null || drawnPoints.Count < 2) return;

        // 1. Calculate the physical length of the currently drawn line
        float totalLength = 0f;
        for (int i = 0; i < drawnPoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(drawnPoints[i], drawnPoints[i + 1]);
        }

        // 2. Set the tiling so the arrows stay perfectly square regardless of line length
        lineMat.mainTextureScale = new Vector2(totalLength * arrowsPerMeter, 1f);

        // 3. Scroll the texture to make the arrows flow forward
        float offset = Time.time * scrollSpeed;
        lineMat.mainTextureOffset = new Vector2(offset, 0f);
    }
}