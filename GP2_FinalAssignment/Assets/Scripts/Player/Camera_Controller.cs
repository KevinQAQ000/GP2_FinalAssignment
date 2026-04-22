using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Camera_Controller : MonoBehaviour
{

    public static Camera_Controller Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // -------------------

    private Transform mTransform;
    [SerializeField] Transform target;  // 跟随目标
    [SerializeField] Vector3 offset;    // 跟随偏移量
    [SerializeField] float moveSpeed;   // 跟随速度

    private Vector2 positionXScope; // X的范围
    private Vector2 positionZScope; // Z的范围

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        mTransform = transform;

        if (MapManager.Instance != null)
        {
            InitPositionScope(MapManager.Instance.MapSizeOnWorld);
        }
    }

    // 初始化坐标范围
    private void InitPositionScope(float mapSizeOnWorld)
    {
        positionXScope = new Vector2(5, mapSizeOnWorld - 5);
        positionZScope = new Vector2(-1, mapSizeOnWorld - 10);
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            targetPosition.x = Mathf.Clamp(targetPosition.x, positionXScope.x, positionXScope.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, positionZScope.x, positionZScope.y);
            mTransform.position = Vector3.Lerp(mTransform.position, targetPosition, Time.deltaTime * moveSpeed);
        }
    }
}