using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomDistance : MonoBehaviour
{

    private IMixedRealitySpatialAwarenessMeshObserver spatialMeshObserver;
    public GameObject sphere;
    public GameObject line;
    public GameObject text;
    private List<GameObject> record = new List<GameObject>(); // Initialize record list
    private List<GameObject> instantiatedObjects = new List<GameObject>();
    public bool isEnabled = false;

    private void Start()
    {
        var spatialAwarenessSystem = CoreServices.SpatialAwarenessSystem;
        PointerHandler pointer = spatialAwarenessSystem.SpatialAwarenessObjectParent.AddComponent<PointerHandler>();
        pointer.OnPointerClicked.AddListener(Spawn);
    }

    public void Spawn(MixedRealityPointerEventData eventData)
    {

        if (!isEnabled) return;

        if (sphere != null)
        {
            var result = eventData.Pointer.Result;
            GameObject game = Instantiate(sphere, result.Details.Point, Quaternion.LookRotation(result.Details.Normal));
            instantiatedObjects.Add(game);

            if (record.Count >= 1)
            {
                // Render the Line btw Points
                GameObject x = Instantiate(line);
                x.SetActive(true);
                x.GetComponent<LineRenderer>().SetPositions(new Vector3[] { record[0].transform.position, game.transform.position });
                instantiatedObjects.Add(x); // Add line to the list

                // Determine the Text Position which will show the Distance
                GameObject z = Instantiate(text);
                z.SetActive(true);
                z.transform.position = (record[0].transform.position + game.transform.position) / 2;

                // Determine the Text Value of Distance and transfer its Accuracy and Unit
                z.GetComponent<TextMesh>().text = (record[0].transform.position - game.transform.position).magnitude.ToString("f3") + "m ";
                z.AddComponent<FaceCamera>();
                instantiatedObjects.Add(z); // Add text to the list

                record.Clear(); // Clear record list for next distance measurement
            }
            else
            {
                record.Add(game);
            }
        }
    }

    public void ClearAllObjects()
    {
        foreach (GameObject obj in instantiatedObjects)
        {
            Destroy(obj);
        }
        instantiatedObjects.Clear();
    }
}
