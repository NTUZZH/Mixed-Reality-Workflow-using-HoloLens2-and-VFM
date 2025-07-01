using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;
using UnityEngine;

public class MeshAreaCalculator : MonoBehaviour, IMixedRealityPointerHandler
{

    public TextMesh displayText;  // 3D TextMesh to display the area
    private void Start()
    {
        displayText = GameObject.FindWithTag("Text").GetComponent<TextMesh>();
    }


    // Click Detect
    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // Convert local vertices to world space and order them correctly
        List<Vector3> quadVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            quadVertices.Add(transform.TransformPoint(vertex));
        }

        // Calculate area using your method
        float area = CalculateArea(quadVertices);

        // Display the area
        displayText.text = $"Polygon area: {area:F2} m^2";
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData) { }

    public void OnPointerDragged(MixedRealityPointerEventData eventData) { }

    public void OnPointerUp(MixedRealityPointerEventData eventData) { }

    private float CalculateArea(List<Vector3> vertices) // Shoelace Formula 
    {
        Vector3 normal = CalculatePolygonNormal(vertices);
        List<Vector2> projectedVertices = ProjectTo2DPlane(vertices, normal);

        float area = 0;
        int vertexCount = projectedVertices.Count;

        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 current = projectedVertices[i];
            Vector2 next = projectedVertices[(i + 1) % vertexCount];
            area += (current.x * next.y) - (next.x * current.y);
        }

        return Mathf.Abs(area) * 0.5f;
    }

    private Vector3 CalculatePolygonNormal(List<Vector3> vertices)
    {
        Vector3 normal = Vector3.zero;
        int vertexCount = vertices.Count;

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 current = vertices[i];
            Vector3 next = vertices[(i + 1) % vertexCount];
            normal += Vector3.Cross(current, next);
        }

        return normal.normalized;
    }

    private List<Vector2> ProjectTo2DPlane(List<Vector3> vertices, Vector3 normal)
    {
        List<Vector2> projectedVertices = new List<Vector2>();
        Vector3 u, v;

        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
        {
            float invLength = 1.0f / Mathf.Sqrt(normal.x * normal.x + normal.z * normal.z);
            u = new Vector3(-normal.z * invLength, 0, normal.x * invLength);
        }
        else
        {
            float invLength = 1.0f / Mathf.Sqrt(normal.y * normal.y + normal.z * normal.z);
            u = new Vector3(0, normal.z * invLength, -normal.y * invLength);
        }

        v = Vector3.Cross(normal, u);

        foreach (Vector3 vertex in vertices)
        {
            projectedVertices.Add(new Vector2(Vector3.Dot(vertex, u), Vector3.Dot(vertex, v)));
        }

        return projectedVertices;
    }
}
