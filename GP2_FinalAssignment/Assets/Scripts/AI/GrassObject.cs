using UnityEngine;

public class GrassObject : MonoBehaviour
{
    private void Start()
    {
        //Grass just spawned, register it
        if (AIManager.Instance != null)
        {
            //Register grass Transform to AI Manager for system queries and management
            AIManager.Instance.RegisterGrass(this.transform);
        }
    }

    private void OnDestroy()
    {
        //Unregister when grass is destroyed (eaten or game exit)
        if (AIManager.Instance != null)
        {
            //Unregister grass Transform from AI Manager to prevent querying non-existent objects
            AIManager.Instance.UnregisterGrass(this.transform);
        }
    }
}