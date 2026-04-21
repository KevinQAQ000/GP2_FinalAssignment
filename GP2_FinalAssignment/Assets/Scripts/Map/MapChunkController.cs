using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapChunkController : MonoBehaviour
{
    public Vector3 CentrePosition { get; private set; }// 地图块中心位置
    public void Init(Vector3 centrePosition)// 初始化地图块
    {
        CentrePosition = centrePosition;// 设置地图块中心位置
    }
}
