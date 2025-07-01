using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using System.Collections;
using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Newtonsoft.Json;
using System.Linq;
using Random = UnityEngine.Random;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Main pipeline of SegSAM
/// </summary>
public class ImageProcessing : MonoBehaviour
{
    private PhotoCapture photoCaptureObject = null;
    private CameraParameters cameraParameters;
    private Vector2 vertex;
    public RawImage rawImage;
    public LayerMask spatialMappingLayerMask;
    public TextMesh Information;
    private Matrix4x4 cameraToWorldMatrix;
    private Matrix4x4 projectionMatrix;
    private Matrix4x4 cameraToWorldMatrixDepth;
    private Matrix4x4 projectionMatrixDepth;
    public byte[] imageBytes;
    public BoundingBoxHandler boundingBoxHandler;
    public RandomDistance randomDistance;
    public ReferencePlane referencePlane;
    public DepthCapture depthCapture;
    private Texture2D targetTexture;
    private String RegularShape;
    private bool isRegular;
    public bool isDepth = false;
    Stopwatch stopwatch;

    private void Start()
    {
        // Set up camera parameters
        cameraParameters = new CameraParameters
        {
            hologramOpacity = 0.0f,
            cameraResolutionWidth = 1920,
            cameraResolutionHeight = 1080,
            pixelFormat = CapturePixelFormat.BGRA32
        };
    }

    public void PerformPhotoCapture()
    {
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                if (result.success)
                {
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                }
            });
        });
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Get the two key matrices retrieving from the photo capturing state
        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
        photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out projectionMatrix);

        // Copy the raw image data into the target texture
        targetTexture = new Texture2D(cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight, TextureFormat.BGRA32, false);
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        rawImage.texture = targetTexture;

        // Convert the Texture2D to a byte array
        imageBytes = targetTexture.EncodeToJPG();

        Information.text = "Image Captured, Create Reference Plane! ";

        // Stop the PhotoCapture object
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    /// <summary>
    /// Button Control to send the Ori_image and Prompt to server side, default mode
    /// </summary>
    public void SendToServerDefault()
    {
        // 每次发送给服务器之前，如果是point prompt，则清除上次的分割和投射结果，以显示一个结果。
        // 对于bbox prompt，我们想保留之前的分割和投射结果，以显示多个结果。
        if (boundingBoxHandler.currentMode==BoundingBoxHandler.Mode.PointPrompt)
        {
            // Find all contour outline holder objects
            GameObject[] contourOutlineHolders = GameObject.FindGameObjectsWithTag("Respawn");

            // If the contour outline holder exists
            foreach (GameObject contourOutlineHolder in contourOutlineHolders)
            {
                // Destroy all child objects (line renderers and text meshes)
                foreach (Transform child in contourOutlineHolder.transform)
                {
                    Destroy(child.gameObject);
                }

                // Destroy the contour outline holder object
                Destroy(contourOutlineHolder);
            }
        }

        isRegular = false;

        // Specify the server URL
        string serverUrl = "http://10.96.7.116:50000";

        if (imageBytes != null)
        {
            Information.text = "Connecting with Server......";

            List<Vector3> seedPointsList = referencePlane.GetSelectedPoints();

            string seedPoints = PointsToString(seedPointsList);
            string bbox = boundingBoxHandler.boundingBoxStr;
            string plist = boundingBoxHandler.pointsStr;
            string plabel = boundingBoxHandler.labelStr;
            RegularShape = "Default";

            //开始计时
            stopwatch = Stopwatch.StartNew();

            StartCoroutine(SendImageToServer(serverUrl,imageBytes,bbox,plist,plabel,RegularShape,seedPoints));
        }
        else
        {
            Information.text = "Capture Photo First!";
        }
        
    }


    /// <summary>
    /// Button Control to send the Ori_image and Prompt to server side, RegularShape mode
    /// </summary>
    public void SendToServerRegularShape()
    {
        // If point mode, clear previous presentation

        if (boundingBoxHandler.currentMode == BoundingBoxHandler.Mode.PointPrompt)
        {
            // Find all contour outline holder objects
            GameObject[] contourOutlineHolders = GameObject.FindGameObjectsWithTag("Respawn");

            // If the contour outline holder exists
            foreach (GameObject contourOutlineHolder in contourOutlineHolders)
            {
                // Destroy all child objects (line renderers and text meshes)
                foreach (Transform child in contourOutlineHolder.transform)
                {
                    Destroy(child.gameObject);
                }

                // Destroy the contour outline holder object
                Destroy(contourOutlineHolder);
            }
        }

        isRegular = true;

        // Specify the server URL
        string serverUrl = "http://10.96.7.116:50000";

        if (imageBytes != null)
        {
            List<Vector3> seedPointsList = referencePlane.GetSelectedPoints();

            string seedPoints = PointsToString(seedPointsList);
            string bbox = boundingBoxHandler.boundingBoxStr;
            string plist = boundingBoxHandler.pointsStr;
            string plabel = boundingBoxHandler.labelStr;
            RegularShape = "Regular";

            //开始计时
            stopwatch = Stopwatch.StartNew();

            StartCoroutine(SendImageToServer(serverUrl, imageBytes, bbox, plist, plabel, RegularShape,seedPoints));
        }
        else
        {
            Information.text = "Capture Photo First!";
        }

    }
    public string PointsToString(List<Vector3> points) // e.g.: 1,2,3;4,5,6
    {
        StringBuilder sb = new StringBuilder();
        foreach (var point in points)
        {
            sb.Append(point.x + "," + point.y + "," + point.z + ";");
        }
        return sb.ToString().TrimEnd(';');
    }

    // Seed points for creating reference plane must be selected before pipeline.
      IEnumerator SendImageToServer(string url, byte[] imageData, string boundingBox, string pList, string pLabel, string RegularShape, string SeedPoints)
        {
            // WWWform is created
            WWWForm form = new WWWForm(); // use WWWForm method

            // image data
            form.AddBinaryData("image", imageData, "image.jpg", "image/jpeg");

            // Bbox data
            form.AddField("bbox", boundingBox);

            // Points data
            form.AddField("pList", pList);

            // Points Label data
            form.AddField("pLabel",pLabel);

            //String to transfer mode
            form.AddField("regularShape", RegularShape);

            // SeedPoints for plane fitting

            form.AddField("seedPoints", SeedPoints);

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    UnityEngine.Debug.Log(request.error);
                }
                else
                {
                    byte[] responseBytes = request.downloadHandler.data;
                    string responseStr = System.Text.Encoding.Default.GetString(responseBytes);

                    // The response is a JSON string now, so we parse it
                    var responseJson = JsonConvert.DeserializeObject<ServerResponse>(responseStr);

                    // Convert the image data from base64 to a byte array
                    byte[] imageBytes = System.Convert.FromBase64String(responseJson.image);

                    Texture2D responseTexture = new Texture2D(2, 2);
                    responseTexture.LoadImage(imageBytes);
                    rawImage.texture = responseTexture;

                // Transfer the contour list retrieved from server into Unity-readble format.

                    List<List<Vector2>> contourSVector = new List<List<Vector2>>();
                    foreach (var contour in responseJson.contours)
                    {
                        List<Vector2> contourVector = new List<Vector2>();
                        foreach (var point in contour)
                        {
                            contourVector.Add(new Vector2(point[0], point[1]));
                        }
                        contourSVector.Add(contourVector);
                    }

                Vector3 receivedNormal = responseJson.NormalVector;
                Vector3 receivedCentroid = responseJson.CentroidVector;

                UnityEngine.Debug.Log($"Received Normal: {receivedNormal} | Received Centroid: {receivedCentroid}");

                float mae = referencePlane.CreatePlaneFromReceivedData(receivedNormal, receivedCentroid);

              
                DrawContourOutlines(contourSVector);

                    // 结束计时（从发送信息开始，到投射计算显示为止的时间。）
                    stopwatch.Stop();
                    UnityEngine.Debug.Log("Elapsed time: " + stopwatch.Elapsed.TotalSeconds + " s");
                    Information.text = "Elapsed time: " + stopwatch.Elapsed.TotalSeconds + " s"+ ", MAE= "+ mae;
                }
            }
        }


    [Serializable]
    public class ServerResponse
    {
        public string image;
        public List<List<List<int>>> contours; // Reason: inner list: (x,y); middle list: (x1,y1),(x2,y2) outside list:{[(x1,y1),(x2,y2)...],[(),(),....]}
        public List<float> normal;
        public List<float> centroid;

        // Use list to receive data to bypass some limitations.
        public Vector3 NormalVector => new Vector3(normal[0], normal[1], normal[2]);
        public Vector3 CentroidVector => new Vector3(centroid[0], centroid[1], centroid[2]);
    }


    /// <summary>
    /// IrregularShape:Perimeter and Area; RegularShape: SideLength, Angle, and Area
    /// </summary>
    /// <param name="contours">Received 2D contour coordinates from Server</param>
    private void DrawContourOutlines(List<List<Vector2>> contours)
    {
        // Create a new GameObject to hold all contour outlines
        GameObject contourOutlineHolder = new GameObject("ContourOutlineHolder")
        {
            tag = "Respawn"
        };

        foreach (List<Vector2> cont in contours)
        {

            // Project the vertices of this contour to 3D space
            List<Vector3> vertices3D = ProjectVerticesTo3DSpace(cont,isDepth);

            // Create a new GameObject for each contours
            GameObject contourOutlineObject = new GameObject("ContourOutline");
            contourOutlineObject.transform.parent = contourOutlineHolder.transform;

            //IrregularShape and RegularShape has different quantifications.

            if(isRegular == true)
            {
                CalculateLengthsAndAngles(vertices3D, contourOutlineObject);
            }
            else if(isRegular == false)
            {
                CalculatePerimeter(vertices3D, contourOutlineObject);
            }

            // Create a new LineRenderer and assign the vertices to it
            LineRenderer lineRenderer = contourOutlineObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = vertices3D.Count;
            lineRenderer.SetPositions(vertices3D.ToArray());

            // Make sure the line is continuous
            lineRenderer.loop = true;

            // Create Material
            Material lineMaterial = new Material(Shader.Find("Sprites/Default"));

            // Set Color
            lineMaterial.color = new Color(0,255,0);

            // Assign Material
            lineRenderer.material = lineMaterial;

            //Line's Color will be the Material's Color
            lineRenderer.startColor = lineMaterial.color;
            lineRenderer.endColor = lineMaterial.color;

            // Set Width
            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.015f;

            // Calculate Area and Display Setting
            float Area = CalculateArea(vertices3D);
            GameObject textObject = new GameObject("AreaText");
            textObject.transform.parent = contourOutlineObject.transform;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = $"{Area:F3} m2";
            textMesh.color = Color.red;
            textMesh.fontStyle = FontStyle.Bold; // Set the font style to bold
            textMesh.fontSize = 40; // Make the font size larger, you can adjust this to your needs
            textMesh.characterSize = 0.015f; // Adjust the character size as needed

            // Set Position
            textObject.transform.position = lineRenderer.bounds.center;
            textObject.AddComponent<FaceCamera>();
        }
    }


    /// <summary>
    /// Projection From 2D to 3D
    /// </summary>
    /// <param name="vertices2D"></param>
    /// <returns></returns>
    private List<Vector3> ProjectVerticesTo3DSpace(List<Vector2> vertices2D, bool isDepth)
    {
        if (isDepth==false)
        {
            float photoHeight = 1080;
            List<Vector3> vertices3D = new List<Vector3>();

            foreach (Vector2 vertex2D in vertices2D) // point by point
            {
                vertex.x = vertex2D.x;
                vertex.y = photoHeight - vertex2D.y;

                Vector3 raydir = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, vertex, isDepth);
                Ray ray = new Ray(cameraToWorldMatrix.GetColumn(3), raydir); // cameraToWorldMatrix.GetColumn(3) RGB相机在世界空间的位置用于射线的起点，raydir向量则表示世界空间中射线的方向

                if (referencePlane.customPlane.distance != 0) // Check if the plane is initialized
                {
                    float enter;
                    if (referencePlane.customPlane.Raycast(ray, out enter))
                    {
                        Vector3 vertexPosition = ray.GetPoint(enter);
                        vertices3D.Add(vertexPosition);
                    }
                }
                else
                {
                    Information.text = "create the plane!";

                }
            }
            return vertices3D;
        }
        else
        {
            float photoHeight = 288;
            List<Vector3> vertices3D = new List<Vector3>();
            cameraToWorldMatrixDepth = depthCapture.depthCameraToWorldMatrix;
            projectionMatrixDepth = depthCapture.depthCameraProjectionMatrix;

            foreach (Vector2 vertex2D in vertices2D)
            {
                vertex.x = vertex2D.x;
                vertex.y = photoHeight - vertex2D.y;

                Vector3 raydir = PixelCoordToWorldCoord(cameraToWorldMatrixDepth, projectionMatrixDepth, vertex, isDepth);
                Ray ray = new Ray(cameraToWorldMatrixDepth.GetColumn(3), raydir); // cameraToWorldMatrixDepth.GetColumn(3) 表示Depth 相机在世界空间中的的位置，用于射线的起点。raydir向量则表示世界空间中射线的方向

                if (referencePlane.customPlane.distance != 0) // Check if the plane is initialized
                {
                    float enter;
                    if (referencePlane.customPlane.Raycast(ray, out enter))
                    {
                        Vector3 vertexPosition = ray.GetPoint(enter);
                        vertices3D.Add(vertexPosition);
                    }
                }
            }
            return vertices3D;

        }

    }

    /// <summary>
    /// This function aims to convert pixel coordinates from the 2D image captured by the HoloLens 2's RGB/Depth camera into a 3D direction vector in the world space.
    /// From NDC to Camera to World space.
    /// </summary>
    /// <param name="cameraToWorldMatrix">extrinsic parameters(positions and orientations of the camera)</param>
    /// <param name="projectionMatrix">intrinsic parameters(focal length and optical centers of the camera)</param>
    /// <param name="pixelCoordinates"></param>
    /// <returns></returns>
    /// RGB和RGBD，都适用于FPERT
    public static Vector3 PixelCoordToWorldCoord(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix, Vector2 pixelCoordinates, bool isDepth)
    {
        pixelCoordinates = ConvertPixelCoordsToScaledCoords(pixelCoordinates, isDepth); // a range of -1 to 1, with the center of the image as the origin, normalized coordinates in NDC

        float focalLengthX = projectionMatrix.GetColumn(0).x; // focal length
        float focalLengthY = projectionMatrix.GetColumn(1).y;
        float centerX = projectionMatrix.GetColumn(2).x; // optical center
        float centerY = projectionMatrix.GetColumn(2).y;

        // the optical centers need to be normalized Based On Microsoft Webpage 
        float normFactor = projectionMatrix.GetColumn(2).z;
        centerX /= normFactor;
        centerY /= normFactor;

        Vector3 dirRay = new Vector3((pixelCoordinates.x - centerX) / focalLengthX, (pixelCoordinates.y - centerY) / focalLengthY, 1.0f / normFactor); //This is in camera space. From NDC(image space) to Camera Space

        //transformed from camera space to world space. using the cameraToWorldMatrix, which represents the camera's position and orientation in the world. From Camera Space to World Space
        Vector3 direction = new Vector3(Vector3.Dot(cameraToWorldMatrix.GetRow(0), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(1), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(2), dirRay));

        return direction;
    }
    static Vector2 ConvertPixelCoordsToScaledCoords(Vector2 pixelCoords, bool isDepth)
    {
        float halfWidth, halfHeight;

        if (isDepth==true)
        {
            // Dimensions for depth mode
            halfWidth = 320 / 2f;
            halfHeight = 288 / 2f;
        }
        else
        {
            // Original dimensions
            halfWidth = 1920 / 2f;
            halfHeight = 1080 / 2f;
        }

        // Translate registration to image center
        pixelCoords.x -= halfWidth;
        pixelCoords.y -= halfHeight;

        // Scale pixel coords to percentage coords (-1 to 1)
        pixelCoords = new Vector2(pixelCoords.x / halfWidth, pixelCoords.y / halfHeight * -1f);

        isDepth = false; // Set back to RGB mode

        return pixelCoords;
    }


    /// <summary>
    /// Calculate Perimeter for Irregular Shape.
    /// </summary>
    /// <param name="vertices3D">Retrieved 3D world coordinates of target shape</param>
    /// <param name="contourOutlineObject"></param>
    private void CalculatePerimeter(List<Vector3> vertices3D, GameObject contourOutlineObject)
    {
        float perimeter = 0.0f;
        for (int i = 0; i < vertices3D.Count; i++)
        {
            // 获取当前顶点和下一个顶点，注意处理最后一个顶点的情况
            Vector3 currentVertex = vertices3D[i];
            Vector3 nextVertex = vertices3D[(i + 1) % vertices3D.Count];

            // 计算两个顶点之间的距离，即边的长度
            float edgeLength = Vector3.Distance(currentVertex, nextVertex);

            // 累加到周长
            perimeter += edgeLength;
        }

        // 计算多边形的中心点
        Vector3 center = CalculateCentroid(vertices3D);

        // 创建一个新的TextMesh对象来显示周长信息
        GameObject perimeterTextObject = new GameObject("PerimeterText");
        perimeterTextObject.transform.parent = contourOutlineObject.transform;
        TextMesh perimeterTextMesh = perimeterTextObject.AddComponent<TextMesh>();
        perimeterTextMesh.text = $"perimeter: {perimeter:F3} m";
        perimeterTextMesh.color = Color.white;
        perimeterTextMesh.fontStyle = FontStyle.Bold;
        perimeterTextMesh.fontSize = 30;
        perimeterTextMesh.characterSize = 0.015f;

        perimeterTextObject.transform.position = center;
        perimeterTextObject.AddComponent<FaceCamera>();
    }

    // 计算一组点的质心
    private Vector3 CalculateCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        return centroid / points.Count;
    }

    /// <summary>
    /// Calculate side length and angles for Regular shape
    /// </summary>
    /// <param name="vertices3D">Retrieved 3D world coordinates of target shape</param>
    /// <param name="contourOutlineObject"></param>
    private void CalculateLengthsAndAngles(List<Vector3> vertices3D, GameObject contourOutlineObject)
    {
        for (int i = 0; i < vertices3D.Count; i++)
        {
            // 获取当前顶点和下一个顶点，注意处理最后一个顶点的情况
            Vector3 currentVertex = vertices3D[i];
            Vector3 nextVertex = vertices3D[(i + 1) % vertices3D.Count]; // 避免获取最后一个顶点的下一个顶点时可能出现的索引超出数组边界的问题。

            // 计算两个顶点之间的距离，即边的长度
            float edgeLength = Vector3.Distance(currentVertex, nextVertex);

            // 创建一个新的TextMesh对象来显示长度信息
            GameObject lengthTextObject = new GameObject("LengthText");
            lengthTextObject.transform.parent = contourOutlineObject.transform;
            TextMesh lengthTextMesh = lengthTextObject.AddComponent<TextMesh>();
            lengthTextMesh.text = $"{edgeLength:F2} m";
            lengthTextMesh.color = Color.white;
            lengthTextMesh.fontStyle = FontStyle.Bold;
            lengthTextMesh.fontSize = 25;
            lengthTextMesh.characterSize = 0.015f;

            // 设置长度信息的位置在两个顶点的中点
            lengthTextObject.transform.position = (currentVertex + nextVertex) / 2;
            lengthTextObject.AddComponent<FaceCamera>();

            // 计算角的夹角，需要前一个顶点、当前顶点和下一个顶点
            if (i > 0) // if currentVertex is not the first vertex
            {
                Vector3 previousVertex = vertices3D[(i - 1 + vertices3D.Count) % vertices3D.Count];

                // 计算两个向量
                Vector3 vector1 = previousVertex - currentVertex;
                Vector3 vector2 = nextVertex - currentVertex;

                // 计算夹角（以度为单位）
                float angle = Vector3.Angle(vector1, vector2);

                // 创建一个新的TextMesh对象来显示角度信息
                GameObject angleTextObject = new GameObject("AngleText");
                angleTextObject.transform.parent = contourOutlineObject.transform;
                TextMesh angleTextMesh = angleTextObject.AddComponent<TextMesh>();
                angleTextMesh.text = $"{angle:F1}°";
                angleTextMesh.color = Color.white;
                angleTextMesh.fontStyle = FontStyle.Bold;
                angleTextMesh.fontSize = 25;
                angleTextMesh.characterSize = 0.015f;

                // 设置角度信息的位置在当前顶点
                angleTextObject.transform.position = currentVertex;
                angleTextObject.AddComponent<FaceCamera>();
            }
        }

        // 最后处理第一个角的夹角
        Vector3 firstVector1 = vertices3D[vertices3D.Count - 1] - vertices3D[0];
        Vector3 firstVector2 = vertices3D[1] - vertices3D[0];
        float firstAngle = Vector3.Angle(firstVector1, firstVector2);

        // 创建一个新的TextMesh对象来显示第一个角的夹角信息
        GameObject firstAngleTextObject = new GameObject("FirstAngleText");
        firstAngleTextObject.transform.parent = contourOutlineObject.transform;
        TextMesh firstAngleTextMesh = firstAngleTextObject.AddComponent<TextMesh>();
        firstAngleTextMesh.text = $"{firstAngle:F1}°";
        firstAngleTextMesh.color = Color.white;
        firstAngleTextMesh.fontStyle = FontStyle.Bold;
        firstAngleTextMesh.fontSize = 25;
        firstAngleTextMesh.characterSize = 0.015f;

        // 设置第一个角的夹角信息的位置在第一个顶点
        firstAngleTextObject.transform.position = vertices3D[0];
        firstAngleTextObject.AddComponent<FaceCamera>();
    }


    /// <summary>
    /// Polygon Calculation, Vertices shall be on the same 3D plane.
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    private float CalculateArea(List<Vector3> vertices)
    {
        Vector3 normal = CalculatePolygonNormal(vertices);
        List<Vector2> projectedVertices = ProjectTo2DPlane(vertices, normal);

        float area = 0;
        int vertexCount = projectedVertices.Count;

        // Shoelace Formula
        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 current = projectedVertices[i];
            Vector2 next = projectedVertices[(i + 1) % vertexCount];
            area += (current.x * next.y) - (next.x * current.y);
        }

        return Mathf.Abs(area) * 0.5f;
    }

    // 计算所有相邻顶点构成向量的交叉积（即它们的法向量）并相加取单位向量
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
    // 取一个和多边形正交的平面，将3D点投射到此平面
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


    /// <summary>
    /// Button Control to Clear View
    /// </summary>
    public void ClearContourOutlines()
    {
        randomDistance.ClearAllObjects(); // reset distance mode
        referencePlane.ResetFunction();// reset customPlane.
        boundingBoxHandler.ReturnBackToDaylightMode();// reset the unity components
        isDepth = false;

        if (imageBytes != null)
        {
            boundingBoxHandler.ResetCanvas();// Set bbox to default [0,0,0,0] and remove from canvas
            rawImage.texture = targetTexture; // Back to original image
            Information.text = "Cleared!";

            // Find all contour outline holder objects
            GameObject[] contourOutlineHolders = GameObject.FindGameObjectsWithTag("Respawn");

            // If the contour outline holder exists
            foreach (GameObject contourOutlineHolder in contourOutlineHolders)
            {
                // Destroy all child objects (line renderers and text meshes)
                foreach (Transform child in contourOutlineHolder.transform)
                {
                    Destroy(child.gameObject);
                }

                // Destroy the contour outline holder object
                Destroy(contourOutlineHolder);
            }
        }
        else
        {
            Information.text = "Cleared!";
        }
    }

    public void ControlDistance()
    {
        if(randomDistance.isEnabled==false)
        {
            Information.text = "Distance On";
        }
        else if (randomDistance.isEnabled==true)
        {
            Information.text = "Distance Off";
        }
        randomDistance.isEnabled = !randomDistance.isEnabled;   
    }

}
