using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý các điểm tuần tra cho quái. 
/// Component này chỉ cung cấp dữ liệu, logic AI điều khiển sẽ nằm ở các Action của Behavior Graph.
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Danh sách các điểm tuần tra")]
    public List<Transform> waypoints = new List<Transform>();

    [Tooltip("Chế độ đi tuần: True là Random, False là đi theo thứ tự")]
    public bool isRandom = false;

    private int _currentIndex = -1;

    /// <summary>
    /// Lấy điểm đến tiếp theo dựa trên chế độ tuần tra.
    /// Nên được gọi từ AI Node trên Server.
    /// </summary>
    /// <returns>Tọa độ Vector3 của điểm đến</returns>
    public Vector3 GetNextWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: Waypoint list is empty! Returning current position.");
            return transform.position;
        }

        if (isRandom)
        {
            // Chế độ Random
            if (waypoints.Count > 1)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, waypoints.Count);
                } while (newIndex == _currentIndex);
                _currentIndex = newIndex;
            }
            else
            {
                _currentIndex = 0;
            }
        }
        else
        {
            // Chế độ tuần tự
            _currentIndex = (_currentIndex + 1) % waypoints.Count;
        }

        return waypoints[_currentIndex].position;
    }

    /// <summary>
    /// Tìm điểm tuần tra gần nhất (hữu ích khi quái vừa kết thúc truy đuổi).
    /// </summary>
    public Vector3 GetClosestWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return transform.position;

        float minDistance = Mathf.Infinity;
        Transform closest = waypoints[0];

        foreach (Transform wp in waypoints)
        {
            float dist = Vector3.Distance(transform.position, wp.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = wp;
            }
        }

        _currentIndex = waypoints.IndexOf(closest);
        return closest.position;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Transform wp in waypoints)
        {
            if (wp != null) Gizmos.DrawSphere(wp.position, 0.3f);
        }

        if (!isRandom)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null && waypoints[(i + 1) % waypoints.Count] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Count].position);
                }
            }
        }
    }
}
