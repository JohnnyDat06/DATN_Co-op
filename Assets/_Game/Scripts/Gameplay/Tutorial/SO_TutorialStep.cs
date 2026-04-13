using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TutorialPointData
{
    public string PointID;
    public Vector3 Position;
    public GameObject VisualPrefab; // Mũi tên hoặc Cube Toon
    public bool RequiredByAllPlayers; // True: Cả 2 player phải đứng vào, False: Chỉ cần 1 player
}

[CreateAssetMenu(fileName = "NewTutorialStep", menuName = "Tutorial/Tutorial Step")]
public class SO_TutorialStep : ScriptableObject
{
    public string StepID;
    public List<TutorialPointData> Points;
    public string Description; // Hiển thị trên UI nếu cần
}
