using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Time state data
/// </summary>
[Serializable]
public class TimeStateData
{
    //Duration of this state
    public float durationTime;
    //Intensity of the sun
    public float sunIntensity;
    //Color of the sun
    public Color sunColor;
    //Rotation of the sun in Euler angles
    public Vector3 sunRotation;

    [HideInInspector]
    public Quaternion sunQuaternion; //Quaternion representation of sun rotation

    public void InitRotation()
    {
        //Convert Euler angles to Quaternion for smooth interpolation later
        sunQuaternion = Quaternion.Euler(sunRotation);
    }

    /// <summary>
    /// Check and calculate time-based transitions
    /// </summary>
    /// <returns>Returns true if still within the current state</returns>
    public bool CheckAndCalTime(float currTime, TimeStateData nextState, out Quaternion rotation, out Color color, out float sunIntensityParam)
    {
        //Value between 0 and 1
        float ratio = 1f - (currTime / durationTime); //Calculate interpolation ratio between current and next state

        //Use Spherical Linear Interpolation (Slerp) for smooth rotation, and Lerp for color/intensity
        rotation = Quaternion.Slerp(this.sunQuaternion, nextState.sunQuaternion, ratio);
        //Linear interpolation for sun color
        color = Color.Lerp(this.sunColor, nextState.sunColor, ratio);
        //Linear interpolation for sun intensity
        sunIntensityParam = Mathf.Lerp(this.sunIntensity, nextState.sunIntensity, ratio);

        //If remaining time is greater than 0, we are still in this state
        return currTime > 0;
    }
}


/// <summary>
/// Time Manager
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [SerializeField] private Light mainLight;                    //Directional light (The Sun)
    [SerializeField] private TimeStateData[] timeStateDatas;    //Time configuration array
    private int currentStateIndex = 0; //Index of the current time state
    private float currTime = 0; //Remaining time in the current state
    private int dayNum; //Current day count

    [SerializeField, Range(0, 30)] private float timeScale = 1; //Time passage speed (1 is default)

    [Header("Night Ecosystem Glow Settings")]
    [SerializeField] private Material[] grassMaterials; //Array of materials to support multiple grass types

    [ColorUsage(true, true)]
    [SerializeField] private Color nightGlowColor = new Color(0, 1f, 0.5f, 1f); //Night glow color (HDR)

    [SerializeField, Range(0f, 5f)] private float maxGlowIntensity = 2.5f; //Maximum emission intensity at night
    [SerializeField] private float glowThreshold = 0.4f; //Intensity threshold when night glow begins

    private void Awake()
    {
        Instance = this;
    }

    private void OnValidate() //Executes immediately when parameters are modified in the Unity Inspector
    {
        if (timeStateDatas != null) //Ensure rotation quaternions are updated when data changes in the editor
        {
            foreach (var data in timeStateDatas) //Iterate through each state to initialize rotations
            {
                data.InitRotation();
            }
        }
    }

    private void Start()
    {
        //Ensure all angles are correctly initialized when the game starts
        if (timeStateDatas != null)
        {
            foreach (var data in timeStateDatas)
            {
                data.InitRotation();
            }
        }

        StartCoroutine(UpdateTime()); //Start the time update coroutine
    }

    private IEnumerator UpdateTime() //Initializes the clock and sets it to index 0 (usually morning)
    {
        currentStateIndex = 0;   //Start with the first state
        int nextIndex = currentStateIndex + 1; //Index of the transition target
        currTime = timeStateDatas[currentStateIndex].durationTime; //Set current state's remaining time
        dayNum = 0; //Start at Day 0

        while (true) //Infinite loop to update time states every frame
        {
            yield return null;
            currTime -= Time.deltaTime * timeScale; //Time progression adjusted by time scale

            //Calculate sun settings and handle state transitions
            if (!timeStateDatas[currentStateIndex].CheckAndCalTime(currTime, timeStateDatas[nextIndex], out Quaternion quaternion, out Color color, out float sunIntensity))
            {
                //Switch to the next state
                currentStateIndex = nextIndex;
                //Boundary check: loop back to 0 if the end of the array is reached
                nextIndex = currentStateIndex + 1 >= timeStateDatas.Length ? 0 : currentStateIndex + 1;

                //If we return to state 0 (Morning), increment the day counter
                if (currentStateIndex == 0) dayNum++;

                //Reset the timer for the new state
                currTime = timeStateDatas[currentStateIndex].durationTime;
            }

            //Update the Sun's rotation, color, and intensity
            mainLight.transform.rotation = quaternion;
            mainLight.color = color;
            SetLight(sunIntensity);
        }
    }

    private void SetLight(float intensity) //Updates sun intensity and triggers grass glow logic
    {
        mainLight.intensity = intensity; //Apply sun intensity
        //Adjust ambient light brightness to match the sun
        RenderSettings.ambientIntensity = intensity;

        //Update the emission of ecosystem materials
        UpdateGrassGlow(intensity);
    }

    /// <summary>
    /// Dynamically adjusts grass emission based on current sun intensity
    /// </summary>
    private void UpdateGrassGlow(float currentSunIntensity)
    {
        if (grassMaterials == null || grassMaterials.Length == 0) return; //Exit if no materials assigned

        //Calculate glow ratio
        float glowRatio = 0f;
        if (currentSunIntensity < glowThreshold) //If sun intensity drops below threshold, start glowing
        {
            glowRatio = 1f - (currentSunIntensity / glowThreshold); //Linear calculation: weaker sun leads to stronger glow
        }

        //Calculate final HDR emission color
        Color finalEmissionColor = nightGlowColor * maxGlowIntensity * glowRatio;

        //Iterate through all materials to apply the emission
        foreach (Material mat in grassMaterials)
        {
            if (mat != null)
            {
                mat.EnableKeyword("_EMISSION"); //Ensure emission keyword is active
                mat.SetColor("_EmissionColor", finalEmissionColor);
            }
        }
    }

    private void OnDisable() //Reset grass emission when the script is disabled to avoid persistent glow in the editor
    {
        if (grassMaterials != null)
        {
            foreach (Material mat in grassMaterials)
            {
                if (mat != null) mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}