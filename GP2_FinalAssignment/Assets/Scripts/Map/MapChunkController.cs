using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Data classes remain unchanged
[System.Serializable]
//Data classes do not need to inherit from MonoBehaviour as they only store data and don't need to be attached to GameObjects.
public class MapChunkMapObjectModel { public GameObject Prefab; public Vector3 Position; }//Single tree/object
//Records all flowers, grass, and trees on this chunk.
public class MapChunkData { public List<MapChunkMapObjectModel> MapObjectList = new List<MapChunkMapObjectModel>(); }

public class MapChunkController : MonoBehaviour
{
    public Vector2Int ChunkIndex { get; private set; }//Chunk index
    public Vector3 CentrePosition { get; private set; }//Chunk center position

    private MapChunkData mapChunkData;//Chunk data
    private List<GameObject> mapObjectList = new List<GameObject>();//List of instantiated objects on this chunk
    private bool isActive = false;//Whether the chunk is active

    //Application exit protection
    private static bool isApplicationQuitting = false;

    private void OnApplicationQuit()//Unity triggers this method when the game is closed
    {
        isApplicationQuitting = true;//Set flag to avoid errors
    }

    public void Init(Vector2Int chunkIndex, Vector3 centrePosition, List<MapChunkMapObjectModel> MapObjectList)
    {
        ChunkIndex = chunkIndex;
        CentrePosition = centrePosition;
        mapChunkData = new MapChunkData();
        mapChunkData.MapObjectList = MapObjectList;
        if (mapObjectList == null) mapObjectList = new List<GameObject>();
        mapObjectList.Clear();
    }

    private void OnDestroy()
    {
        //If the application is quitting, skip recycling logic to prevent errors
        if (isApplicationQuitting) return;

        if (isActive)
        {
            ClearObjects();
        }
    }

    private void ClearObjects()
    {
        //Safety check: ensure the object pool singleton is still active
        if (mapObjectList == null || PoolManager.Instance == null) return;

        for (int i = 0; i < mapObjectList.Count; i++)
        {
            if (mapObjectList[i] != null)
            {
                PoolManager.Instance.PushGameObject(mapObjectList[i]);//Recycle objects back to the object pool
            }
        }
        mapObjectList.Clear();//Clear the list
    }

    //If the received command differs from the current state, show or hide the chunk itself first.
    public void SetActive(bool active)//Activate chunk
    {
        if (isActive != active)//Only execute switch if the current state differs from the target state
        {
            isActive = active;
            gameObject.SetActive(isActive);//Switch the display state of the chunk

            if (mapChunkData == null) return;//Return if chunk data is null
            List<MapChunkMapObjectModel> dataList = mapChunkData.MapObjectList;//Get the object list from chunk data

            if (isActive)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    GameObject go = PoolManager.Instance.GetGameObject(dataList[i].Prefab, transform);//Get object from pool and set its parent to this chunk
                    if (go != null)
                    {
                        go.transform.position = dataList[i].Position;//Set object position
                        mapObjectList.Add(go);//Add object to the current chunk's object list
                    }
                }
            }
            else
            {
                ClearObjects();
            }
        }
    }
}