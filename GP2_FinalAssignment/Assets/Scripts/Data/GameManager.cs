using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO; //Required for file read/write

public class GameManager : MonoBehaviour
{
    [Header("Pause Menu Panel")]
    public GameObject pauseMenuPanel;//Remember to drag this reference in the Inspector!
    private bool isPaused = false;//Used to track whether the game is currently paused

    void Update()
    {
        //Listen for the ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();//Resume game if already paused
            else PauseGame();//Pause game if not yet paused
        }
    }

    //Pause and Resume game
    public void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    //Save Game
    public void SaveGame()
    {
        string path = Application.persistentDataPath + "/gamesave.json";
        SaveData data = new SaveData();

        //Read existing save from local disk first (to preserve previously set map seeds and size info)
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
        }

        //Capture player's current precise coordinates
        if (Player_Controller.Instance != null)
        {
            Vector3 pos = Player_Controller.Instance.playerTransform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
            data.playerZ = pos.z;
            data.hasSavedPosition = true; //Coordinates have been saved!
        }

        //Repackage as Json and overwrite back to disk
        string newJson = JsonUtility.ToJson(data);
        File.WriteAllText(path, newJson);

        Debug.Log("✅ Game progress saved! Player location：" + data.playerX + ", " + data.playerZ);
    }
    // ================================================================

    public void OnClickReturnMenu()//This function should be bound to the "Return to Main Menu" button in the pause menu
    {
        //Automatically save when the player clicks to return to the main menu!
        SaveGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OnClickQuit()//This function should be bound to the "Quit Game" button in the pause menu
    {
        Application.Quit();
    }
}