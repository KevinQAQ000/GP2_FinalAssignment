using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO; // 【注意】：一定别忘了引入这个，用来读写文件

public class GameManager : MonoBehaviour
{
    [Header("暂停菜单面板")]
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

    void Update()
    {
        // 监听 ESC 键逻辑不变... (省略)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

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

    // ================== 【新增：保存游戏的核心逻辑】 ==================
    public void SaveGame()
    {
        string path = Application.persistentDataPath + "/gamesave.json";
        SaveData data = new SaveData();

        // 1. 先把本地硬盘里的老存档读出来（为了保留咱们之前设置的那些地图种子、尺寸信息）
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
        }

        // 2. 抓取玩家当前的精确坐标
        if (Player_Controller.Instance != null)
        {
            Vector3 pos = Player_Controller.Instance.playerTransform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
            data.playerZ = pos.z;
            data.hasSavedPosition = true; // 标记为：已经保存过坐标了！
        }

        // 3. 重新打包成 Json 并覆盖写入硬盘
        string newJson = JsonUtility.ToJson(data);
        File.WriteAllText(path, newJson);

        Debug.Log("✅ 游戏进度已保存！玩家位置：" + data.playerX + ", " + data.playerZ);
    }
    // ================================================================

    public void OnClickReturnMenu()
    {
        // 【最佳实践】：玩家点击返回主菜单时，自动帮他保存一下！
        SaveGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}