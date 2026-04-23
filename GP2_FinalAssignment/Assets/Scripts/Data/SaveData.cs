[System.Serializable]
public class SaveData
{
    public int mapSize = 2;
    public int mapChunkSize = 10;
    public float cellSize = 1f;
    public int mapSeed = 123;
    public int spawnSeed = 456;
    public float marshLimit = 0.5f;

    //位置信息
    public bool hasSavedPosition = false; // 用来判断是不是第一次玩这个存档
    public float playerX;
    public float playerY;
    public float playerZ;
}