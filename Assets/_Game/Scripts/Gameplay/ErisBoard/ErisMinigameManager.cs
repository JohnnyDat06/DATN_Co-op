using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine;

public class ErisMinigameManager : NetworkBehaviour
{
    [Header("References")]
    public GameObject TilePrefab;
    public GameObject ChessPiecePrefab;
    public ParticleSystem BlackFogVFX; 
    public Transform NextAreaSpawn; 
    public Transform ControllerStandPos; 
    public Transform ObserverStandPos;   

    [Header("Camera Customization")]
    [Tooltip("Vị trí camera so với Manager (Center)")]
    public Vector3 CameraOffset = new Vector3(4.935221f, 14f, 5f);
    public float CameraFOV = 60f;

    private List<ErisTile> _spawnedTiles = new List<ErisTile>();
    private Vector2Int[] _syncedPath; 
    
    private NetworkVariable<bool> _isGameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> _isMemorizing = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> _hasCompleted = new NetworkVariable<bool>(false); 
    private NetworkVariable<ulong> _controllerId = new NetworkVariable<ulong>();
    private NetworkVariable<ulong> _observerId = new NetworkVariable<ulong>();
    private NetworkVariable<int> _currentStepIndex = new NetworkVariable<int>(0);
    private NetworkVariable<Vector2Int> _pieceGridPos = new NetworkVariable<Vector2Int>();

    private GameObject _spawnedPieceInstance;
    private Dictionary<ulong, Vector3> _lockedPositions = new Dictionary<ulong, Vector3>();
    private Dictionary<ulong, Quaternion> _lockedRotations = new Dictionary<ulong, Quaternion>();
    
    private Coroutine _moveCoroutine;
    private Coroutine _pathLoopCoroutine;
    private bool _isReseting = false; 
    private bool _canInput = true;

    private readonly KeyCode[] _upKeys = { KeyCode.W, KeyCode.UpArrow };
    private readonly KeyCode[] _downKeys = { KeyCode.S, KeyCode.DownArrow };
    private readonly KeyCode[] _leftKeys = { KeyCode.A, KeyCode.LeftArrow };
    private readonly KeyCode[] _rightKeys = { KeyCode.D, KeyCode.RightArrow };

    public override void OnNetworkSpawn()
    {
        _pieceGridPos.OnValueChanged += (oldVal, newVal) => UpdatePieceTarget(newVal);
        _isMemorizing.OnValueChanged += (oldVal, newVal) => { if (!newVal) StopPathLoop(); };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || _isGameActive.Value || _hasCompleted.Value) return;
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out var netObj))
        {
            StartMinigameServer(netObj.OwnerClientId);
        }
    }

    private void StartMinigameServer(ulong triggerPlayerId)
    {
        _isGameActive.Value = true;
        _isMemorizing.Value = true;
        _controllerId.Value = triggerPlayerId;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            if (client.ClientId != triggerPlayerId) { _observerId.Value = client.ClientId; break; }

        _syncedPath = GeneratePathArray();
        _pieceGridPos.Value = _syncedPath[0];
        _currentStepIndex.Value = 0;

        SpawnChessPieceServer();
        SetupBoardClientRpc(_controllerId.Value, _observerId.Value, _syncedPath);
    }

    private Vector2Int[] GeneratePathArray()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        bool success = false;
        while (!success) {
            path.Clear();
            Vector2Int current = new Vector2Int(Random.Range(0, 10), 0);
            path.Add(current);
            while (current.y < 9) {
                List<Vector2Int> moves = new List<Vector2Int>();
                Vector2Int[] neighbors = { current + Vector2Int.up, current + Vector2Int.left, current + Vector2Int.right, current + Vector2Int.down };
                foreach (var m in neighbors) {
                    if (m.x >= 0 && m.x < 10 && m.y >= 0 && m.y < 10 && !path.Contains(m)) {
                        int count = 0;
                        if (path.Contains(m + Vector2Int.up)) count++;
                        if (path.Contains(m + Vector2Int.down)) count++;
                        if (path.Contains(m + Vector2Int.left)) count++;
                        if (path.Contains(m + Vector2Int.right)) count++;
                        if (count <= 1) moves.Add(m);
                    }
                }
                if (moves.Count == 0) break;
                List<Vector2Int> sideMoves = moves.FindAll(v => v.y == current.y);
                float r = Random.value;
                if (sideMoves.Count > 0 && r < 0.6f) current = sideMoves[Random.Range(0, sideMoves.Count)];
                else {
                    List<Vector2Int> upMoves = moves.FindAll(v => v.y > current.y);
                    if (upMoves.Count > 0) current = upMoves[Random.Range(0, upMoves.Count)];
                    else current = moves[Random.Range(0, moves.Count)];
                }
                path.Add(current);
                if (current.y == 9 && path.Count >= 15 && path.Count <= 25) success = true;
            }
        }
        return path.ToArray();
    }

    [ClientRpc]
    private void SetupBoardClientRpc(ulong controllerId, ulong observerId, Vector2Int[] path)
    {
        _syncedPath = path;
        CleanupBoard();
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++) {
                Vector3 worldPos = transform.TransformPoint(new Vector3(x * 1.1f, 0, y * 1.1f)); 
                GameObject tileObj = Instantiate(TilePrefab, worldPos, transform.rotation, transform);
                ErisTile tile = tileObj.GetComponent<ErisTile>();
                tile.Init(new Vector2Int(x, y));
                _spawnedTiles.Add(tile);
            }

        var lp = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (lp != null) {
            if(lp.TryGetComponent<Rigidbody>(out var rb)) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
            if(lp.TryGetComponent<PlayerStateMachine>(out var fsm)) fsm.TransitionTo(PlayerStateType.Idle);
            
            Transform standPos = (NetworkManager.Singleton.LocalClientId == controllerId) ? ControllerStandPos : ObserverStandPos;
            if (standPos != null) { lp.transform.position = standPos.position; lp.transform.rotation = standPos.rotation; }
            _lockedPositions[NetworkManager.Singleton.LocalClientId] = lp.transform.position;
            _lockedRotations[NetworkManager.Singleton.LocalClientId] = lp.transform.rotation;
        }

        // CHỈNH CAMERA TOP-DOWN CỐ ĐỊNH TẠI MANAGER
        bool isController = NetworkManager.Singleton.LocalClientId == controllerId;
        CameraPreset preset = isController ? CameraPreset.TopDownController : CameraPreset.TopDownObserver;
        
        CameraManager.Instance.SwitchCamera(preset);
        
        // Ép vị trí/góc xoay cho Virtual Camera để NHÌN CỐ ĐỊNH VÀO MANAGER
        SyncCameraToManager();

        if (isController && BlackFogVFX != null) BlackFogVFX.Play();
        else if (!isController) StartPathLoop();

        EventBus.RaiseGamePaused(); UpdatePieceTarget(path[0]); 
    }

    private void SyncCameraToManager() {
        var vcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var vcam in vcams) {
            if (vcam.name.Contains("TopDown")) {
                // Tắt Follow/LookAt để dùng vị trí tĩnh
                vcam.Target.TrackingTarget = null;
                vcam.Target.LookAtTarget = null;
                
                // Đặt vị trí camera tương đối so với Manager
                vcam.transform.position = transform.TransformPoint(CameraOffset);
                vcam.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
                
                var lens = vcam.Lens;
                lens.FieldOfView = CameraFOV;
                vcam.Lens = lens;
            }
        }
    }

    private void StartPathLoop() { if (_pathLoopCoroutine != null) StopCoroutine(_pathLoopCoroutine); _pathLoopCoroutine = StartCoroutine(PathRevealRoutine()); }
    private void StopPathLoop() {
        if (_pathLoopCoroutine != null) StopCoroutine(_pathLoopCoroutine);
        foreach (var t in _spawnedTiles) t.RestoreColor();
        
        if (NetworkManager.Singleton.LocalClientId == _controllerId.Value) 
        { 
            if (BlackFogVFX != null) { BlackFogVFX.Stop(); BlackFogVFX.Clear(); } 
            // Tự động hiển thị các ô có thể đi tiếp theo ngay khi bắt đầu chơi
            HighlightPossibleMoves(_pieceGridPos.Value);
        }
        else 
        { 
            ErisTile st = GetTileAt(_syncedPath[0]); 
            if (st != null) st.SetColor(Color.green, true); 
        }
    }

    private IEnumerator PathRevealRoutine() {
        while (true) {
            foreach (var t in _spawnedTiles) t.ResetTile();
            foreach (var step in _syncedPath) {
                ErisTile tile = GetTileAt(step);
                if (tile != null) tile.SetColor(Color.green);
                yield return new WaitForSeconds(0.15f);
            }
            yield return new WaitForSeconds(2f);
        }
    }

    private void SyncCameraRotation() {
        var vcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var vcam in vcams) vcam.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }

    private void SpawnChessPieceServer() {
        Vector3 worldStart = transform.TransformPoint(new Vector3(_pieceGridPos.Value.x * 1.1f, 0.5f, _pieceGridPos.Value.y * 1.1f));
        _spawnedPieceInstance = Instantiate(ChessPiecePrefab, worldStart, transform.rotation);
        _spawnedPieceInstance.GetComponent<NetworkObject>().Spawn();
    }

    private void Update() {
        if (!IsSpawned || !_isGameActive.Value) return;
        var lp = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (lp != null) {
            if(_lockedPositions.TryGetValue(NetworkManager.Singleton.LocalClientId, out Vector3 lockPos)) lp.transform.position = lockPos;
            if(_lockedRotations.TryGetValue(NetworkManager.Singleton.LocalClientId, out Quaternion lockRot)) lp.transform.rotation = lockRot;
        }
        if (NetworkManager.Singleton.LocalClientId == _observerId.Value) 
        { 
            if (_isMemorizing.Value && Input.GetKeyDown(KeyCode.E)) ReadyToPlayServerRpc(); 
        }
        
        if (NetworkManager.Singleton.LocalClientId == _controllerId.Value) 
        { 
            if (!_isMemorizing.Value && !_isReseting && _canInput) HandleKeyboardInput(); 
        }
    }

    private void HandleKeyboardInput() {
        Vector2Int moveDir = Vector2Int.zero;
        if (AnyKeyPressed(_upKeys)) moveDir = Vector2Int.up;
        else if (AnyKeyPressed(_downKeys)) moveDir = Vector2Int.down;
        else if (AnyKeyPressed(_leftKeys)) moveDir = Vector2Int.left;
        else if (AnyKeyPressed(_rightKeys)) moveDir = Vector2Int.right;
        if (moveDir != Vector2Int.zero) {
            Vector2Int targetPos = _pieceGridPos.Value + moveDir;
            if (targetPos.x >= 0 && targetPos.x < 10 && targetPos.y >= 0 && targetPos.y < 10) {
                _canInput = false; SubmitMoveServerRpc(targetPos);
            }
        }
    }

    private bool AnyKeyPressed(KeyCode[] keys) { foreach (var k in keys) if (Input.GetKeyDown(k)) return true; return false; }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyToPlayServerRpc() => _isMemorizing.Value = false;

    [ServerRpc(RequireOwnership = false)]
    private void SubmitMoveServerRpc(Vector2Int gridPos) {
        Vector2Int currentPos = _pieceGridPos.Value;
        if (Mathf.Abs(gridPos.x - currentPos.x) + Mathf.Abs(gridPos.y - currentPos.y) == 1 
            && _currentStepIndex.Value + 1 < _syncedPath.Length 
            && gridPos == _syncedPath[_currentStepIndex.Value + 1]) 
        {
            _currentStepIndex.Value++; 
            _pieceGridPos.Value = gridPos;
        } 
        else 
        {
            WrongMoveEffectClientRpc(gridPos);
            StartCoroutine(ResetServerDelayed());
        }
    }

    private IEnumerator ResetServerDelayed() { 
        yield return new WaitForSeconds(2.0f); 
        _currentStepIndex.Value = 0; 
        // Force update: gán giá trị rác trước khi gán lại giá trị cũ để ép OnValueChanged trên Client
        _pieceGridPos.Value = new Vector2Int(-1, -1);
        _pieceGridPos.Value = _syncedPath[0]; 
    }

    [ClientRpc]
    private void WrongMoveEffectClientRpc(Vector2Int wrongPos) { StartCoroutine(WrongMoveRippleEffect(wrongPos)); }

    private IEnumerator WrongMoveRippleEffect(Vector2Int wrongPos) {
        _isReseting = true; _canInput = false;
        ErisTile wrongTile = GetTileAt(wrongPos);
        if (wrongTile != null) wrongTile.ApplyTemporaryRed();
        yield return new WaitForSeconds(0.2f);
        for (int dist = 1; dist < 20; dist++) {
            bool found = false;
            foreach (var tile in _spawnedTiles) {
                int d = Mathf.Abs(tile.GridPos.x - wrongPos.x) + Mathf.Abs(tile.GridPos.y - wrongPos.y);
                if (d == dist) { tile.ApplyTemporaryRed(); found = true; }
            }
            if (!found && dist > 10) break;
            yield return new WaitForSeconds(0.04f);
        }
        yield return new WaitForSeconds(0.4f);
        foreach (var t in _spawnedTiles) t.RestoreColor();
        _isReseting = false;
    }

    private void UpdatePieceTarget(Vector2Int gridPos) {
        if (_spawnedPieceInstance == null) _spawnedPieceInstance = GameObject.FindWithTag("ChessPiece");
        Vector3 worldTarget = transform.TransformPoint(new Vector3(gridPos.x * 1.1f, 0.5f, gridPos.y * 1.1f));
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(SmoothMovePiece(worldTarget, gridPos));
        ErisTile t = GetTileAt(gridPos);
        if (t != null) t.SetColor(Color.green, true);
        if (IsServer && _currentStepIndex.Value == _syncedPath.Length - 1) StartCoroutine(EndGameDelayed());
    }

    private IEnumerator SmoothMovePiece(Vector3 target, Vector2Int gridPos) {
        if (_spawnedPieceInstance == null) yield break;
        float speed = (_isReseting || _currentStepIndex.Value == 0) ? 22f : 12f; 
        while (Vector3.Distance(_spawnedPieceInstance.transform.position, target) > 0.001f) {
            _spawnedPieceInstance.transform.position = Vector3.MoveTowards(_spawnedPieceInstance.transform.position, target, speed * Time.deltaTime);
            _spawnedPieceInstance.transform.rotation = Quaternion.Lerp(_spawnedPieceInstance.transform.rotation, transform.rotation, Time.deltaTime * 10f);
            yield return null;
        }
        _spawnedPieceInstance.transform.position = target;
        HighlightPossibleMoves(gridPos);
        yield return new WaitForSeconds(0.05f);
        if (!_isReseting) _canInput = true;
    }

    private void HighlightPossibleMoves(Vector2Int center) {
        foreach (var t in _spawnedTiles) t.SetOutline(false);
        if (NetworkManager.Singleton.LocalClientId != _controllerId.Value || _isMemorizing.Value || _isReseting) return;
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in neighbors) { ErisTile t = GetTileAt(center + dir); if (t != null) t.SetOutline(true); }
    }

    private IEnumerator EndGameDelayed() { 
        yield return new WaitForSeconds(1f); 
        SuccessEffectClientRpc(_pieceGridPos.Value); 
    }

    [ClientRpc]
    private void SuccessEffectClientRpc(Vector2Int finalPos) {
        StartCoroutine(SuccessRippleEffect(finalPos));
    }

    private IEnumerator SuccessRippleEffect(Vector2Int finalPos) {
        _canInput = false;
        // Ripple màu xanh Cyan lan tỏa từ vị trí đích
        for (int dist = 0; dist < 20; dist++) {
            bool found = false;
            foreach (var tile in _spawnedTiles) {
                int d = Mathf.Abs(tile.GridPos.x - finalPos.x) + Mathf.Abs(tile.GridPos.y - finalPos.y);
                if (d == dist) { 
                    tile.SetColor(Color.green, true); 
                    found = true; 
                }
            }
            if (!found && dist > 10) break;
            yield return new WaitForSeconds(0.04f);
        }
        yield return new WaitForSeconds(0.8f);
        
        // Sau khi hiệu ứng chạy xong mới gọi logic kết thúc và dọn dẹp
        FinalizeMinigame();
    }

    private void FinalizeMinigame() {
        if (IsServer) {
            _isGameActive.Value = false; 
            _hasCompleted.Value = true;
            if (_spawnedPieceInstance != null) _spawnedPieceInstance.GetComponent<NetworkObject>().Despawn();
        }
        
        CleanupBoard();
        var lp = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (lp != null && lp.TryGetComponent<Rigidbody>(out var rb)) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; }
        _lockedPositions.Clear(); _lockedRotations.Clear();
        
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchCamera(CameraPreset.ThirdPerson); 
        
        EventBus.RaiseGameResumed();
        if (NextAreaSpawn != null && lp != null) lp.transform.position = NextAreaSpawn.position;
    }

    private ErisTile GetTileAt(Vector2Int pos) => _spawnedTiles.Find(t => t.GridPos == pos);
    private void CleanupBoard() { foreach (var t in _spawnedTiles) if (t != null) Destroy(t.gameObject); _spawnedTiles.Clear(); }
}
