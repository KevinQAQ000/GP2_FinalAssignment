[System.Serializable]
public class SaveData
{
    public int mapSize = 2;//Map radius; the map expands 2 chunks outward from the center, totaling 5 chunks (-2, -1, 0, 1, 2)
    public int mapChunkSize = 10;//Size of each map chunk; default is 10, meaning each chunk covers 10m x 10m
    public float cellSize = 1f;//Size of each cell; default is 1, meaning each cell covers 1m x 1m
    public int mapSeed = 123;//Random seed for map generation
    public int spawnSeed = 456;//Random seed for enemy spawning
    public float marshLimit = 0.5f;//Threshold for marsh terrain; range 0 to 1, default 0.5, meaning if a cell's noise value exceeds 0.5, it's determined as marsh terrain

    //Position information
    public bool hasSavedPosition = false;//Used to determine if this is the first time playing this save
    public float playerX;
    public float playerY;
    public float playerZ;
}