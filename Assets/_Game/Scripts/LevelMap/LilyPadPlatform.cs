using UnityEngine;

public class LilyPadPlatform : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public Transform targetPointB;
    public float travelTime = 4f;

    [Header("Cài đặt nhấp nhô")]
    public float bobSpeed = 3f;
    public float bobHeight = 0.02f;

    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 previousPosition;

    private CharacterController playerController;

    void Start()
    {
        pointA = transform.position;
        if (targetPointB != null)
            pointB = new Vector3(targetPointB.position.x, pointA.y, targetPointB.position.z);
        else
            pointB = pointA;

        previousPosition = transform.position;
    }

    void Update()
    {
        // 1. Tính toán vị trí mới của lá sen
        float lerpValue = Mathf.PingPong(Time.time / travelTime, 1f);
        Vector3 currentPos = Vector3.Lerp(pointA, pointB, lerpValue);
        float verticalBob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        transform.position = new Vector3(currentPos.x, pointA.y + verticalBob, currentPos.z);

        // 2. Kéo Player đi theo khoảng cách mà lá sen vừa di chuyển
        Vector3 deltaMovement = transform.position - previousPosition;
        if (playerController != null)
        {
            playerController.Move(deltaMovement);
        }

        previousPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = other.GetComponent<CharacterController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = null;
        }
    }
}