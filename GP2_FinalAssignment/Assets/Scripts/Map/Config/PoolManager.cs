using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    //Automatic Singleton Pattern
    private static PoolManager instance;//Static field to store the singleton instance
    public static PoolManager Instance//Public static property providing the access entry
    {
        get
        {
            if (instance == null)//Prevent errors
            {
                //If not in the scene, automatically create an invisible GameObject named PoolManager to act as the warehouse
                GameObject go = new GameObject("PoolManager");
                //Add PoolManager component to this object and assign it to instance
                instance = go.AddComponent<PoolManager>();
                DontDestroyOnLoad(go); //Ensure the warehouse is not destroyed when switching scenes
            }
            return instance;
        }
    }

    //Core Warehouse
    //The Key of the dictionary is the prefab name; the Value is a Queue storing idle GameObjects
    private Dictionary<string, Queue<GameObject>> poolDic = new Dictionary<string, Queue<GameObject>>();

    /// <summary>
    /// Retrieve an object from the pool
    /// </summary>
    public GameObject GetGameObject(GameObject prefab, Transform parent)
    {
        GameObject go;//Variable to store the retrieved object
        string poolKey = prefab.name; //Use the prefab name as the key to find items

        //If the warehouse contains this type of pool and it has idle items
        if (poolDic.ContainsKey(poolKey) && poolDic[poolKey].Count > 0)
        {
            go = poolDic[poolKey].Dequeue(); //Dequeue one from the queue
            go.transform.SetParent(parent);  //Place it under the target map chunk
            go.SetActive(true);              //Activate display
        }
        else
        {
            //If the pool is empty or hasn't been created yet, instantiate a new one
            go = Instantiate(prefab, parent);
            //Set the name to match the prefab exactly, removing the "(Clone)" suffix
            //This ensures we know which pool it belongs to when it is recycled
            go.name = poolKey;
        }

        return go;
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void PushGameObject(GameObject go)
    {
        string poolKey = go.name; //Determine which pool it returns to via its name

        go.SetActive(false); //Hide the object
        go.transform.SetParent(this.transform); //Move the object under the PoolManager to keep the scene clean

        //If no queue exists for this type of object in the warehouse, create one
        if (!poolDic.ContainsKey(poolKey))
        {
            poolDic.Add(poolKey, new Queue<GameObject>());//Create a new empty queue
        }

        //Insert the object into the corresponding queue for future use
        poolDic[poolKey].Enqueue(go);
    }
}