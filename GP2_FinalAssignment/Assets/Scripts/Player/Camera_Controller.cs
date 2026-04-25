using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Camera_Controller : MonoBehaviour
{

    public static Camera_Controller Instance { get; private set; }

    private void Awake()
    {
        //Singleton pattern
        //Ensure there is only one instance of the camera controller in the scene.
        if (Instance == null) Instance = this;
        //If an instance already exists, destroy this object immediately.
        else Destroy(gameObject);
    }

    private Transform mTransform; //Cache the camera's own transform information
    [SerializeField] Transform target;  //Follow target
    [SerializeField] Vector3 offset;    //Offset distance from the target
    [SerializeField] float moveSpeed;   //Following speed

    //Restrict the camera's movement within the map boundaries while following the target to prevent it from moving out of bounds.
    private Vector2 positionXScope; //Scope for X axis
    private Vector2 positionZScope; //Scope for Z axis

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        mTransform = transform; //Cache the camera's own transform information

        if (MapManager.Instance != null)
        {
            //Get the map dimensions and define the movement boundary lines based on the size.
            InitPositionScope(MapManager.Instance.MapSizeOnWorld); //Initialize coordinate scope
        }
    }

    //Initialize coordinate scope
    private void InitPositionScope(float mapSizeOnWorld)
    {
        //The camera's X position cannot be less than 5 or greater than (map width - 5).
        positionXScope = new Vector2(5, mapSizeOnWorld - 5);
        //The camera's Z position cannot be less than -1 or greater than (map width - 10).
        positionZScope = new Vector2(-1, mapSizeOnWorld - 10);
    }

    private void LateUpdate()
    {
        if (target != null) //If there is a target, follow it
        {
            //Calculate the position the camera should move to, based on the target position plus offset, and clamped within the specified scope.
            Vector3 targetPosition = target.position + offset;

            //Constrain the camera movement within the map range to avoid moving out of map boundaries.
            targetPosition.x = Mathf.Clamp(targetPosition.x, positionXScope.x, positionXScope.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, positionZScope.x, positionZScope.y);

            //Use Linear Interpolation (Lerp) to smoothly move the camera toward the target position.
            mTransform.position = Vector3.Lerp(mTransform.position, targetPosition, Time.deltaTime * moveSpeed);
        }
    }
}