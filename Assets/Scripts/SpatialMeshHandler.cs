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
        // ��ȡ�ռ�����۲���
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

            // �л����ֵ
            isShowingMesh = !isShowingMesh;
        }
        else
        {
            Debug.Log("No spatial mesh observer available");
        }
    }
}
