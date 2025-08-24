# AppSAM

A Mixed Reality application for HoloLens 2, built with Unity and the Mixed Reality Toolkit (MRTK). This application leverages the device's RGB and depth camera and performs complex image processing and segmentation. It features spatial mapping and dynamic building elements segmentation and quantification. 

This project has been published as a journal paper titled **"Automated non-contact geometric quality assessment of building elements integrating mixed reality, depth sensing, and vision foundation model"** in the Journal of Computing in Civil Engineering (in press, DOI: 10.1061/JCCEE5/CPENG-6727).

## Technologies & Dependencies

*   **Engine:** Unity 3D (with OpenXR plugin)
*   **MR Toolkit:** Mixed Reality Toolkit (MRTK) v2.8.3
*   **Numerical Library:** MathNet.Numerics v5.0.0
*   **Platform:** Universal Windows Platform (UWP) for HoloLens 2

## Core Functionality

The project's main logic is contained within the `Assets/Scripts` folder. Key scripts include:

*   `DepthCapture.cs`: Manages capturing and processing data from the HoloLens depth sensor.
*   `ImageProcessing.cs`: Contains extensive logic for performing advanced image processing tasks.
*   `SpatialMeshHandler.cs`: Interacts with the real-world spatial mesh provided by the HoloLens.
*   `BoundingBoxHandler.cs`: Handles the creation and manipulation of bounding boxes for runtime object interaction.
*   `ReferencePlane.cs`: Manages reference planes within the application, which may be used for aligning or placing objects.
*   `server.py`: Uses FastSAM as the Vision Foundation Model in this project to receive the prompt and raw image from the MR device to proceed the image segmentation. The server shall be deployed independently and run simultaneously with the AppSAM application. server.py can only be used together with the original FastSAM project (not provided in this repository).

## Setup and Build

To set up and build this project for a HoloLens 2 device, follow these steps:

1.  **Clone the repository:** 
    ```bash
    git clone <your-repository-url>
    ```
2.  **Open in Unity:** Open the project folder in a compatible version of the Unity Editor. Ensure you have the "Universal Windows Platform build support" module installed.
3.  **Build Unity Project:**
    *   Go to `File > Build Settings`.
    *   Select `Universal Windows Platform` from the list.
    *   Ensure the Target Device is `HoloLens` and the Architecture is `ARM64`.
    *   Click `Build`. This will generate a Visual Studio solution in the folder you select (typically a `Build` or `App` folder).
4.  **Build in Visual Studio:**
    *   Open the generated `.sln` file in Visual Studio.
    *   Change the build configuration to `Release` and `ARM64`.
    *   Build the solution. This will create the application package that can be deployed to a HoloLens 
