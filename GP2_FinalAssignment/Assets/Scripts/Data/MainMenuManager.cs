using UnityEngine;
using UnityEngine.UI; //Essential for UI control
using UnityEngine.SceneManagement; //Essential for scene switching
using System.IO; //Essential for file read/write

public class MainMenuManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject settingsPanel;//Assigned in the inspector, hidden by default

    [Header("Input Field References")]
    public TMPro.TMP_InputField mapSizeInput;
    public TMPro.TMP_InputField mapSeedInput;
    public TMPro.TMP_InputField spawnSeedInput;
    public Slider marshLimitSlider;

    [Header("Button References")]
    public Button continueButton;

    private string savePath;//Absolute path to the save file, configured in Awake

    private void Awake()
    {
        //Set absolute path for save file, stored in the computer's hidden AppData folder
        savePath = Application.persistentDataPath + "/gamesave.json";

        //If no save file exists on disk, gray out the "Continue" button and make it non-interactable
        continueButton.interactable = File.Exists(savePath);
    }

    //Button bound click events
    //Executed when the "New Game" button is clicked
    public void OnClickNewGame()
    {
        settingsPanel.SetActive(true); //Pop up the hidden settings panel
    }

    //Executed when "Start Generation" is clicked in the settings panel
    public void OnClickStartNewGame()
    {
        //Store the data filled in the settings panel into a data object
        SaveData newData = new SaveData();

        //Convert the player's input (text) into integers (int.Parse) and fill the object
        //Safety check: provide default values if the input fields are empty
        newData.mapSize = string.IsNullOrEmpty(mapSizeInput.text) ? 2 : int.Parse(mapSizeInput.text);
        newData.mapSeed = string.IsNullOrEmpty(mapSeedInput.text) ? 123 : int.Parse(mapSeedInput.text);
        newData.spawnSeed = string.IsNullOrEmpty(spawnSeedInput.text) ? 456 : int.Parse(spawnSeedInput.text);
        newData.marshLimit = marshLimitSlider.value;

        //Serialize the filled object into Json text and save it to the disk
        string json = JsonUtility.ToJson(newData);
        File.WriteAllText(savePath, json);

        //Switch to the actual game scene (index 1 represents your game scene build index)
        SceneManager.LoadScene(1);
    }

    //Executed when the "Continue" button is clicked
    public void OnClickContinue()
    {
        //Directly enter the game scene; the loading task is handled by the MapManager in the game scene
        SceneManager.LoadScene(1);
    }

    //Executed when the "Exit" button is clicked
    public void OnClickExit()
    {
        Application.Quit();
        Debug.Log("Exited the game"); //Only prints this message when testing in the editor
    }
}