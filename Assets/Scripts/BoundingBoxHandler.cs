using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;
using UnityEngine.UI;


public class BoundingBoxHandler : MonoBehaviour, IMixedRealityPointerHandler
{
    public ImageProcessing imageProcessing;
    public Sprite circleSprite;  // set this in the inspector
    public RectTransform rawImage;
    public RectTransform boundingBox;
    public string boundingBoxStr;
    public string pointsStr;
    public string labelStr;
    public enum Mode { BoundingBox, PointPrompt } //enumeration to classify the mode    
    public Mode currentMode = Mode.BoundingBox;
    private Vector2[] boundingBoxPoints = new Vector2[2];
    private int boundingBoxPointIndex = 0;
    private int imageWidth = 1920;
    private int imageHeight = 1080;
    private float canvasWidth;
    private float canvasHeight;
    private List<LabeledPoint> pointPromptPoints = new List<LabeledPoint>();
    private int currentLabel = 1; // set the foreground points as default
    private struct LabeledPoint
    {
        public Vector2 point;
        public int label;
    }

    /// <summary>
    /// Update the BoxCollider size when Rawimage is Scaled.
    /// </summary>
    void Update()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            // 设置BoxCollider的尺寸
            boxCollider.size = new Vector3(rawImage.rect.width, rawImage.rect.height, 0f);
        }
    }
    public void FitForNightModeRawImage() // Adjust the Unity component to fit for Depth Image
    {
        rawImage.sizeDelta = new Vector2(200, 180);

        // LongThrow Mode 
        imageWidth = 320;
        imageHeight = 288;
    }

    public void ReturnBackToDaylightMode() // Adjust the Unity component back to default
    {
        rawImage.sizeDelta = new Vector2(200, 112.5f);
        imageWidth = 1920;
        imageHeight = 1080;
    }

    /// <summary>
    /// Hand Click Down
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // 将3D空间的点转换为屏幕空间的点
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(eventData.Pointer.Result.Details.Point);
        Vector2 localPoint;

        // 将屏幕空间的点转换为RectTransform的本地坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage, screenPoint, Camera.main, out localPoint))
        {
            // 转换坐标系，使得y轴反转
            localPoint.y = -localPoint.y;

            // 根据RectTransform的尺寸调整坐标
            localPoint += new Vector2(rawImage.rect.width / 2, rawImage.rect.height / 2);

            // 根据当前的模式处理点击事件
            if (currentMode == Mode.BoundingBox)
            {
                OnPointerDownBoundingBox(localPoint);
            }

            else
            {
                OnPointerDownPointPrompt(localPoint);
            }

        }
    }
    private void OnPointerDownBoundingBox(Vector2 localPoint)
    {
        // 记录点击的位置
        boundingBoxPoints[boundingBoxPointIndex] = localPoint;
        boundingBoxPointIndex = (boundingBoxPointIndex + 1) % 2;
    }
    private void OnPointerDownPointPrompt(Vector2 localPoint)
    {
        // 记录点击的位置
        pointPromptPoints.Add(new LabeledPoint { point = localPoint, label = currentLabel });
    }

    /// <summary>
    /// Hand Click Loose
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (currentMode == Mode.BoundingBox && boundingBoxPointIndex == 0)
        {
            UpdateBoundingBox();
        }
        else if (currentMode == Mode.PointPrompt)
        {
            UpdatePointPrompt();
        }
    }
    public void SwitchMode()
    {


        if(currentMode==Mode.BoundingBox)
        {
            currentMode = Mode.PointPrompt;
            imageProcessing.Information.text = "Current is Point Mode";
            
        }
        else
        {
            currentMode = Mode.BoundingBox;
            imageProcessing.Information.text = "Current is BoundingBox Mode";
            boundingBoxStr = "[0,0,0,0]";
        }
    }
    public void SwitchPointLabel()
    {
        if (currentMode == Mode.BoundingBox)
        {
            imageProcessing.Information.text = "Convert to PointMode first";
        }
        else
        {
            currentLabel = 1 - currentLabel;  // switch btw 0 and 1, default=1(foreground)

            if (currentLabel == 1)
            {
                imageProcessing.Information.text = "Current label: Foreground Points";
            }
            else if (currentLabel == 0)
            {
                imageProcessing.Information.text = "Current label: Background Points";
            }
        }
    }

    private void UpdatePointPrompt()
    {
        // Generate pointsStr and labelsStr from pointPromptPoints
        List<string> pointsList = new List<string>();
        List<string> labelsList = new List<string>();

        canvasHeight = rawImage.rect.height;
        canvasWidth = rawImage.rect.width;

        foreach (LabeledPoint lp in pointPromptPoints)
        {
            int x = Mathf.FloorToInt(lp.point.x);
            int y = Mathf.FloorToInt(lp.point.y);
            int px = (int)(x * imageWidth / canvasWidth);
            int py = (int)(y * imageHeight / canvasHeight);
            pointsList.Add($"[{px}, {py}]");
            labelsList.Add(lp.label.ToString());
        }

        pointsStr = $"[{string.Join(",", pointsList)}]";
        labelStr = $"[{string.Join(",", labelsList)}]";

        // Draw points on the screen
        foreach (LabeledPoint lp in pointPromptPoints)
        {
            // Create a new GameObject to represent the point
            GameObject pointMarker = new GameObject("PointMarker");

            // Assign it as a child to the drawPoints RectTransform
            pointMarker.transform.SetParent(rawImage, false);

            // Create and setup RectTransform component
            RectTransform rectTransform = pointMarker.AddComponent<RectTransform>();

            // Setup position (lp.point is a screen space point)
            Vector2 localPoint = lp.point;
            localPoint.y = -localPoint.y;
            rectTransform.anchoredPosition = localPoint;

            // Set the anchor and pivot to the top-left corner
            rectTransform.anchorMin = new Vector2(0, 1);  // top-left corner
            rectTransform.anchorMax = new Vector2(0, 1);  // top-left corner
            rectTransform.pivot = new Vector2(0.5f,0.5f ); //pivot

            // Set size of the point marker (adjust to fit your needs)
            rectTransform.sizeDelta = new Vector2(5, 5);  

            // Add Image component and setup
            Image image = pointMarker.AddComponent<Image>();
            image.sprite = circleSprite;  // use the circle sprite
            image.color = lp.label == 1 ? Color.green : Color.red;  // set color based on label
        }

    }
    private void UpdateBoundingBox()
    {
        // 计算bounding box的左上角坐标（取两个点的最小x和最小y）
        Vector2 topLeft = new Vector2(Mathf.Min(boundingBoxPoints[0].x, boundingBoxPoints[1].x), Mathf.Min(boundingBoxPoints[0].y, boundingBoxPoints[1].y));

        // 计算bounding box的宽度和高度
        Vector2 size = new Vector2(Mathf.Abs(boundingBoxPoints[0].x - boundingBoxPoints[1].x), Mathf.Abs(boundingBoxPoints[0].y - boundingBoxPoints[1].y));

        // 更新bounding box的位置和大小,bounding box的锚点已经设置到左上角，子对象的位置将相对于父对象的左上角进行定位。
        Vector2 boundingbox = topLeft;
        boundingbox.y = -boundingbox.y;
        boundingBox.anchoredPosition = boundingbox; // 把子对象相对于父对象的坐标位置与父对象本身的坐标位置对应起来
        boundingBox.sizeDelta = size;

        canvasHeight = rawImage.rect.height;
        canvasWidth = rawImage.rect.width;

        // 将bounding box的位置和大小转换为像素坐标
        int boxX = (int)(topLeft.x * imageWidth / canvasWidth);
        int boxY = (int)(topLeft.y * imageHeight / canvasHeight);
        int boxWidth = (int)(size.x * imageWidth / canvasWidth);
        int boxHeight = (int)(size.y * imageHeight / canvasHeight);

        boundingBoxStr = "[" + boxX + "," + boxY + "," + boxWidth + "," + boxHeight + "]";

    }
    public void ResetCanvas()
    {
        boundingBoxStr = "[0,0,0,0]";
        boundingBox.sizeDelta = new Vector2(0, 0); // remove bbox from canvas

        pointPromptPoints.Clear();
        pointsStr = "[[0,0]]";
        labelStr = "[0]";

        foreach (Transform child in rawImage.transform) // Remove points from canvas
        {
            // Check if the child is a point marker
            if (child.gameObject.name == "PointMarker")
            {
                // Destroy the point marker
                Destroy(child.gameObject);
            }
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Do nothing
    }
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // Do nothing
    }
}
