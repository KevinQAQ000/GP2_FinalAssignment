using UnityEngine;
using UnityEngine.UI; // 控制UI必备
using UnityEngine.SceneManagement; // 切换场景必备
using System.IO; // 读写文件必备

public class MainMenuManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject settingsPanel;

    [Header("输入框引用")]
    // 注意：如果你用的是 TextMeshPro 的输入框，这里可能需要改成 TMPro.TMP_InputField
    public TMPro.TMP_InputField mapSizeInput;
    public TMPro.TMP_InputField mapSeedInput;
    public TMPro.TMP_InputField spawnSeedInput;
    public Slider marshLimitSlider;

    [Header("按钮引用")]
    public Button continueButton;

    private string savePath;

    private void Awake()
    {
        // 设定存档文件的绝对路径（存在电脑的隐藏AppData文件夹里）
        savePath = Application.persistentDataPath + "/gamesave.json";

        // 智能判断：如果硬盘里没有存档文件，就把“继续游戏”按钮变灰点不动
        continueButton.interactable = File.Exists(savePath);
    }

    // --- 下面是给按钮绑定的点击事件 ---

    // 1. 点击“新建游戏”按钮时执行
    public void OnClickNewGame()
    {
        settingsPanel.SetActive(true); // 让隐藏的设置面板弹出来
    }

    // 2. 在设置面板里点击“开始生成”时执行
    public void OnClickStartNewGame()
    {
        // 第一步：把我们刚才写的“填空表”拿出来
        SaveData newData = new SaveData();

        // 把玩家在输入框里敲的字（text）转换成整数（int.Parse），填进表里
        // 加个安全判断，万一玩家没填，就给个默认值
        newData.mapSize = string.IsNullOrEmpty(mapSizeInput.text) ? 2 : int.Parse(mapSizeInput.text);
        newData.mapSeed = string.IsNullOrEmpty(mapSeedInput.text) ? 123 : int.Parse(mapSeedInput.text);
        newData.spawnSeed = string.IsNullOrEmpty(spawnSeedInput.text) ? 456 : int.Parse(spawnSeedInput.text);
        newData.marshLimit = marshLimitSlider.value;

        // 第二步：把填好的表翻译成 Json 文本，存入电脑硬盘
        string json = JsonUtility.ToJson(newData);
        File.WriteAllText(savePath, json);

        // 第三步：切换到真正的游戏场景（参数 1 代表你游戏场景的编号）
        SceneManager.LoadScene(1);
    }

    // 3. 点击“继续游戏”按钮时执行
    public void OnClickContinue()
    {
        // 直接进游戏场景，读取工作交给游戏场景里的 MapManager 去做
        SceneManager.LoadScene(1);
    }

    // 4. 点击“退出”按钮时执行
    public void OnClickExit()
    {
        Application.Quit();
        Debug.Log("退出了游戏"); // 在编辑器里测试时只会输出这句话
    }
}