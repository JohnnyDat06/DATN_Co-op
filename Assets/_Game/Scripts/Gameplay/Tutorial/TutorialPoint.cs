using UnityEngine;
using MoreMountains.Feedbacks;
using System;
using Unity.Netcode;

public class TutorialPoint : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float _detectionRadius = 4f;
    [SerializeField] private bool _mustBeBelow = true;
    [SerializeField] private LayerMask _playerLayer;

    [Header("Quick Outline Settings")]
    [SerializeField] private bool _useOutline = true;
    [SerializeField] private Color _outlineColor = Color.yellow;
    [SerializeField] private float _outlineWidth = 5f;
    [SerializeField] private Outline.Mode _outlineMode = Outline.Mode.OutlineAll;

    [Header("Floating Animation (Simple)")]
    [SerializeField] private bool _autoFloat = true;
    [SerializeField] private float _floatAmplitude = 0.5f;
    [SerializeField] private float _floatSpeed = 2f;

    [Header("Feel Feedbacks (v5.8)")]
    [SerializeField] private MMF_Player _appearFeedback;
    [SerializeField] private MMF_Player _disappearFeedback;
    [SerializeField] private MMF_Player _idleFeedback;

    public int PointIndex { get; set; }
    public Action<int, ulong> OnPlayerReached;

    private bool _isReached = false;
    private Vector3 _startPos;
    private Outline _outline;

    private void Start()
    {
        _startPos = transform.position;
        
        // Cấu hình Quick Outline
        if (_useOutline)
        {
            _outline = GetComponent<Outline>();
            if (_outline == null) _outline = gameObject.AddComponent<Outline>();
            
            _outline.OutlineColor = _outlineColor;
            _outline.OutlineWidth = _outlineWidth;
            _outline.OutlineMode = _outlineMode;
            _outline.enabled = true;
        }

        if (_appearFeedback != null) _appearFeedback.PlayFeedbacks();
        if (_idleFeedback != null) _idleFeedback.PlayFeedbacks();

        if (_playerLayer == 0) _playerLayer = LayerMask.GetMask("Player");
    }

    private void Update()
    {
        if (_isReached) return;

        if (_autoFloat)
        {
            float newY = _startPos.y + Mathf.Sin(Time.time * _floatSpeed) * _floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        CheckProximity();
    }

    private void CheckProximity()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;
        
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        Vector3 playerPos = localPlayer.transform.position;
        float distance = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), 
                                          new Vector3(transform.position.x, 0, transform.position.z));

        if (distance <= _detectionRadius)
        {
            bool isHeightValid = !_mustBeBelow || (playerPos.y < transform.position.y + 1f);

            if (isHeightValid)
            {
                _isReached = true;
                OnPlayerReached?.Invoke(PointIndex, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    public void Disappear()
    {
        _isReached = true;
        
        // Tắt outline khi biến mất cho đẹp
        if (_outline != null) _outline.enabled = false;

        if (_disappearFeedback != null)
        {
            _disappearFeedback.PlayFeedbacks();
            Destroy(gameObject, 0.6f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isReached) return;
        var player = other.GetComponent<PlayerController>();
        if (player != null && player.IsOwner)
        {
            _isReached = true;
            OnPlayerReached?.Invoke(PointIndex, player.OwnerClientId);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        if (_mustBeBelow)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 5f);
        }
    }
}
