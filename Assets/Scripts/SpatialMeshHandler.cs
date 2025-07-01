using UnityEngine;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;
using System.Linq;

public class SpatialMeshHandler : MonoBehaviour
{
    private IMixedRealitySpatialAwarenessMeshObserver spatialMeshObserver;
    private bool isShowingMesh = true;

    private void Start()
    {
        // 获取空间网格观察者
        var spatialAwarenessSystem = CoreServices.SpatialAwarenessSystem as IMixedRealityDataProviderAccess;
        if (spatialAwarenessSystem != null)
        {
            spatialMeshObserver = spatialAwarenessSystem.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
        }
    }

    public void ToggleSpatialMeshVisibility()
    {
        if (spatialMeshObserver != null)
        {
            if (isShowingMesh)
            {
                spatialMeshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
            }
            else
            {
                spatialMeshObserver.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
            }

            // 切换标记值
            isShowingMesh = !isShowingMesh;
        }
        else
        {
            Debug.Log("No spatial mesh observer available");
        }
    }
}
