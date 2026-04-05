using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    [Tooltip("Tốc độ và hướng xoay của vật thể (X, Y, Z)")]
    public Vector3 rotationSpeed = new Vector3(0f, 0f, 50f);

    void Update()
    {
        // Lệnh xoay vật thể đều đặn theo thời gian thực (Time.deltaTime)
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}