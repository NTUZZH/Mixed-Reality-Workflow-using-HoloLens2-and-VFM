using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Method to select the seed points of fitting plane using hand tap of HoloLens2, and record the 3D positions of these seed points for sending to server for fitting algorithm.
/// </summary>
public class ReferencePlane : MonoBehaviour
{
    private List<GameObject> selectedPoints = new List<GameObject>();
    public GameObject spherePrefab;
    public Plane customPlane;

    private void Start()
    {
        var spatialAwarenessSystem = CoreServices.SpatialAwarenessSystem;
        PointerHandler pointer = spatialAwarenessSystem.SpatialAwarenessObjectParent.AddComponent<PointerHandler>();
        pointer.OnPointerClicked.AddListener(SelectPoint);
    }

    public void SelectPoint(MixedRealityPointerEventData eventData)
    {
        var result = eventData.Pointer.Result;
        GameObject point = Instantiate(spherePrefab, result.Details.Point, Quaternion.LookRotation(result.Details.Normal));

        selectedPoints.Add(point);

    }

    public List<Vector3> GetSelectedPoints()
    {
        List<Vector3> points = new List<Vector3>();
        foreach (GameObject point in selectedPoints)
        {
            points.Add(point.transform.position);
        }
        return points;
    }

    public float CreatePlaneFromReceivedData(Vector3 normal, Vector3 centroid)
    {
        customPlane = new Plane(normal, centroid);

        // Calculate the MAE for seed points
        float mae = CalculateMAE(selectedPoints);

        foreach (GameObject point in selectedPoints)
        {
            Destroy(point);
        }

        selectedPoints.Clear();

        return mae;
    }

    private float CalculateMAE(List<GameObject> points)
    {
        float sumAbsoluteDistances = 0;
        foreach (GameObject pointObject in points)
        {
            Vector3 pointPosition = pointObject.transform.position;
            float distanceInMeters = customPlane.GetDistanceToPoint(pointPosition);
            float distanceInMilimeters = distanceInMeters * 1000;
            sumAbsoluteDistances += Mathf.Abs(distanceInMilimeters);
        }

        float mae = sumAbsoluteDistances / points.Count;
        return mae;
    }

    public void ResetFunction()
    {
        customPlane = new Plane();
    }

}

