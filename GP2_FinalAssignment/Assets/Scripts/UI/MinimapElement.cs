using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// Attached to prefabs that need to be displayed on the minimap
/// </summary>
public class MinimapElement : MonoBehaviour
{
    [Tooltip("0 for Tree, 1 for Stone, 2 for Grass")]
    public int objectType = 0; //Object type, determines which icon is shown on the minimap

    //Triggered when the object is retrieved from the object pool and SetActive(true) is called
    private void OnEnable()
    {
        StartCoroutine(DelayRegister());
    }

    private IEnumerator DelayRegister()
    {
        //Suspend execution until the end of the current frame
        yield return null; //Ensures MinimapManager has finished its Awake() registration to avoid null reference errors

        if (MinimapManager.Instance != null) //Null check to prevent crashes if MinimapManager is not set up correctly
        {
            //Register with the MinimapManager to display this object on the minimap
            MinimapManager.Instance.RegisterObject(transform, objectType);
        }
    }

    //Triggered when the player moves away and the object is returned to the pool via SetActive(false)
    private void OnDisable()
    {
        //Unregistering doesn't require a delay; remove from minimap immediately when hidden
        if (MinimapManager.Instance != null) //Null check for safety
        {
            //Unregister from the MinimapManager to remove it from the minimap
            MinimapManager.Instance.UnregisterObject(transform);
        }
    }
}