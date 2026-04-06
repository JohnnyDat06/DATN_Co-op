using UnityEngine;

public class FloatingLog : MonoBehaviour
{
    public float bobSpeed = 2f;     // Tốc độ nhấp nhô
    public float bobHeight = 0.05f; // Độ cao nhấp nhô
    public float wobbleSpeed = 1f;  // Tốc độ lắc lư
    public float wobbleAngle = 2f;  // Góc lắc lư

    private Vector3 startPos;
    private Vector3 startRot;
    private float randomOffset;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.eulerAngles;
        // Random để các khúc gỗ nhấp nhô lệch nhịp nhau, trông tự nhiên hơn
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Xử lý nhấp nhô lên xuống (Trục Y)
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed + randomOffset) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Xử lý lắc lư ngả nghiêng (Trục X và Z)
        float rotX = startRot.x + Mathf.Sin(Time.time * wobbleSpeed + randomOffset) * wobbleAngle;
        float rotZ = startRot.z + Mathf.Cos(Time.time * wobbleSpeed + randomOffset) * wobbleAngle;
        transform.rotation = Quaternion.Euler(rotX, startRot.y, rotZ);
    }
}