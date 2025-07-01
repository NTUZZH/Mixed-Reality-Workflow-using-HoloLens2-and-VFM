using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TextMesh GameObject will always face the Inspector
public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        transform.LookAt(Camera.main.transform.position);
        transform.Rotate(0, 180, 0);
    }
}

