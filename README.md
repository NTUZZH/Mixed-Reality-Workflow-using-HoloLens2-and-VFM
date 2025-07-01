# AppSAM

A Mixed Reality application for HoloLens 2, built with Unity and the Mixed Reality Toolkit (MRTK). This application leverages the device's depth camera and performs complex image processing and numerical calculations. It also features spatial mapping and dynamic object interaction. 

This project has been published as a journal paper titled **"Automated non-contact geometric quality assessment of building elements integrating mixed reality, depth sensing, and vision foundation model"** in the Journal of Computing in Civil Engineering.

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

## How to Push this Project to GitHub

Follow these steps to upload your project to a new GitHub repository.

1.  **Create a `.gitignore` file:** Before uploading, it's crucial to have a `.gitignore` file to exclude large, unnecessary, and auto-generated files from source control. You can use a standard Unity `.gitignore` file. A good one can be found [here](https://github.com/github/gitignore/blob/main/Unity.gitignore).

2.  **Initialize Git:** Open a terminal or command prompt in the root of your project folder and run:
    ```bash
    git init
    ```

3.  **Add and Commit Files:** Stage all the necessary files for the initial commit.
    ```bash
    git add .
    git commit -m "Initial commit"
    ```

4.  **Create a New GitHub Repository:** Go to [GitHub](https://github.com/new) and create a new repository. Do **not** initialize it with a README or .gitignore, as you have already created them locally.

5.  **Link and Push:** Connect your local repository to the remote one on GitHub and push your files.
    ```bash
    git remote add origin https://github.com/your-username/your-repository-name.git
    git branch -M main
    git push -u origin main
    ```

    Replace `your-username` and `your-repository-name` with your actual GitHub username and repository name. 