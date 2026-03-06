using System.Collections.Generic;
using UnityEngine;

public class ProRacingLine : MonoBehaviour
{
    // --- We created a struct to hold the permanent data for each arrow ---
    public struct ArrowPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float safeSpeed;
        public float distanceAlongTrack;
    }

    [Header("Core References")]
    public RoadSpline targetSpline;
    public Rigidbody playerCar;
    public GameObject arrowPrefab;

    [Header("Visual Tuning")]
    public int lookAheadPoints = 60;
    public float minSpacing = 1.0f;
    public float maxSpacing = 4.0f;
    public float minScale = 0.5f;
    public float maxScale = 1.2f;
    public float heightOffset = 0.1f;

    [Header("Color Settings")]
    public Color tooSlowColor = Color.blue;
    public Color optimalColor = Color.green;
    public Color tooFastColor = Color.red;
    public float colorSensitivity = 15f;

    [Header("Car Physics Limits")]
    [Range(0.5f, 3.0f)]
    public float tireGripLimit = 1.2f;
    public float brakingDeceleration = 8.0f;

    private List<Vector3> allTrackPoints = new List<Vector3>();
    private List<float> safeSpeeds = new List<float>();

    // The new lists for our pre-baked data
    private List<ArrowPoint> bakedArrows = new List<ArrowPoint>();
    private List<GameObject> arrowPool = new List<GameObject>();
    private float totalTrackLength = 0f;

    void Start()
    {
        AnalyzeTrack();
        BakeArrows(); // Calculate fixed positions once!
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < lookAheadPoints; i++)
        {
            GameObject arrow = Instantiate(arrowPrefab, transform);
            arrow.SetActive(false);
            arrowPool.Add(arrow);
        }
    }

    void AnalyzeTrack()
    {
        if (targetSpline == null) return;
        allTrackPoints.Clear();

        for (int s = 0; s < targetSpline.SegmentCount; s++)
        {
            for (int i = 0; i <= targetSpline.samplesPerSegment; i++)
            {
                if (s > 0 && i == 0) continue;
                allTrackPoints.Add(targetSpline.GetPointOnSegment(s, i / (float)targetSpline.samplesPerSegment));
            }
        }

        safeSpeeds = new List<float>(new float[allTrackPoints.Count]);
        for (int i = 1; i < allTrackPoints.Count - 1; i++)
        {
            float radius = CalculateRadius(allTrackPoints[i - 1], allTrackPoints[i], allTrackPoints[i + 1]);
            float maxSafeVel = Mathf.Sqrt(tireGripLimit * 9.81f * radius);
            safeSpeeds[i] = float.IsNaN(maxSafeVel) ? 200f : maxSafeVel;
        }
    }

    // --- NEW METHOD: Pre-calculates permanent arrow coordinates ---
    void BakeArrows()
    {
        bakedArrows.Clear();
        float distanceSinceLastArrow = maxSpacing; // Force first arrow to spawn instantly
        totalTrackLength = 0f;

        for (int i = 0; i < allTrackPoints.Count - 1; i++)
        {
            float distToNext = Vector3.Distance(allTrackPoints[i], allTrackPoints[i + 1]);

            float speed = safeSpeeds[i];
            float sharpness = Mathf.InverseLerp(60f, 10f, speed);
            float requiredSpacing = Mathf.Lerp(maxSpacing, minSpacing, sharpness);

            // Is it time to place a permanent arrow?
            if (distanceSinceLastArrow >= requiredSpacing)
            {
                ArrowPoint pt = new ArrowPoint();
                pt.position = allTrackPoints[i] + Vector3.up * heightOffset;

                // Direction and Raycast to hug the road perfectly
                Vector3 dir = (allTrackPoints[i + 1] - allTrackPoints[i]).normalized;
                Vector3 roadNormal = Vector3.up;
                if (Physics.Raycast(pt.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
                {
                    roadNormal = hit.normal;
                }

                if (dir != Vector3.zero) pt.rotation = Quaternion.LookRotation(dir, roadNormal);
                else pt.rotation = Quaternion.identity;

                float scaleVal = Mathf.Lerp(maxScale, minScale, sharpness);
                pt.scale = new Vector3(scaleVal, scaleVal, scaleVal);
                pt.safeSpeed = speed;
                pt.distanceAlongTrack = totalTrackLength;

                bakedArrows.Add(pt);
                distanceSinceLastArrow = 0f; // Reset counter
            }

            distanceSinceLastArrow += distToNext;
            totalTrackLength += distToNext;
        }
    }

    float CalculateRadius(Vector3 a, Vector3 b, Vector3 c)
    {
        float sideA = Vector3.Distance(b, c);
        float sideB = Vector3.Distance(a, c);
        float sideC = Vector3.Distance(a, b);
        float s = (sideA + sideB + sideC) / 2;
        float area = Mathf.Sqrt(s * (s - sideA) * (s - sideB) * (s - sideC));
        if (area < 0.001f) return 1000f;
        return (sideA * sideB * sideC) / (4 * area);
    }

    void Update()
    {
        if (playerCar == null || bakedArrows.Count == 0) return;
        UpdateRacingLine();
    }

    void UpdateRacingLine()
    {
        int nearestIndex = GetNearestBakedPoint();
        float currentSpeed = playerCar.linearVelocity.magnitude;

        int activeCount = Mathf.Min(lookAheadPoints, bakedArrows.Count);

        for (int i = 0; i < activeCount; i++)
        {
            // Loop around the track if we reach the end
            int index = (nearestIndex + i) % bakedArrows.Count;
            ArrowPoint pt = bakedArrows[index];

            GameObject arrow = arrowPool[i];
            arrow.SetActive(true);

            // Snap the pool object to the permanent baked coordinates
            arrow.transform.position = pt.position;
            arrow.transform.rotation = pt.rotation;
            arrow.transform.localScale = pt.scale;

            // --- Braking Math ---
            float distToArrow = pt.distanceAlongTrack - bakedArrows[nearestIndex].distanceAlongTrack;
            if (distToArrow < 0) distToArrow += totalTrackLength; // Fix math if car is passing the start/finish line

            float maxEntrySpeed = Mathf.Sqrt(Mathf.Pow(pt.safeSpeed, 2) + (2 * brakingDeceleration * distToArrow));
            float speedDiff = currentSpeed - maxEntrySpeed;

            Color arrowColor = optimalColor;
            if (speedDiff > 0)
                arrowColor = Color.Lerp(optimalColor, tooFastColor, speedDiff / colorSensitivity);
            else
                arrowColor = Color.Lerp(optimalColor, tooSlowColor, -speedDiff / colorSensitivity);

            arrow.GetComponentInChildren<Renderer>().material.color = arrowColor;
        }

        // Hide unused arrows
        for (int i = activeCount; i < arrowPool.Count; i++) arrowPool[i].SetActive(false);
    }

    // Now checks against the baked array instead of every single spline point
    int GetNearestBakedPoint()
    {
        float minDst = float.MaxValue;
        int index = 0;
        Vector3 carPos = playerCar.position;

        for (int i = 0; i < bakedArrows.Count; i++)
        {
            float dst = (carPos - bakedArrows[i].position).sqrMagnitude;
            if (dst < minDst) { minDst = dst; index = i; }
        }
        return index;
    }
}