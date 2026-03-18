using System.Collections.Generic;
using Pathfinder.Core;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.World
{
    /// <summary>
    /// 맵 데이터
    /// </summary>
    [System.Serializable]
    public class MapData
    {
        [Tooltip("맵 고유 ID")]
        public string MapId;
        
        [Tooltip("맵 GameObject (비활성화된 상태로 설정)")]
        public GameObject MapRoot;
        
        [Tooltip("맵 이름 (UI 표시용)")]
        public string DisplayName;
        
        [Tooltip("플레이어 스폰 위치")]
        public Transform SpawnPoint;
    }
    
    /// <summary>
    /// 맵 관리자 - 한 씬에 여러 맵을 SetActive로 전환
    /// </summary>
    public class MapManager : MonoBehaviour, IMapManager
    {
        [Header("Map Settings")]
        [SerializeField] private List<MapData> _maps = new();
        [SerializeField] private int _startingMapIndex = 0;
        
        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 0.5f;
        
        // 현재 맵 인덱스
        private int _currentMapIndex = -1;
        
        // 맵 ID → 인덱스 매핑
        private Dictionary<string, int> _mapIdToIndex = new();
        
        // 참조
        private SaveManager _saveManager;
        private PlayerController _playerController;
        
        private void Awake()
        {
            BuildMapIndex();
            InitializeMaps();
            
            // 참조 찾기
            _saveManager = FindObjectOfType<SaveManager>();
            _playerController = FindObjectOfType<PlayerController>();
        }
        
        private void Start()
        {
            // 저장 데이터 확인
            if (_saveManager != null && _saveManager.HasSaveData())
            {
                // 저장 데이터 있음 → 저장된 맵으로 로드
                _saveManager.Load();
            }
            else
            {
                // 저장 데이터 없음 → 시작 맵의 SpawnPoint에서 시작
                InitializeFirstSpawn();
            }
        }
        
        /// <summary>
        /// 첫 시작 시 초기화 (저장 데이터 없을 때)
        /// </summary>
        private void InitializeFirstSpawn()
        {
            // 시작 맵 활성화
            if (_maps.Count > 0 && _startingMapIndex >= 0 && _startingMapIndex < _maps.Count)
            {
                SwitchToMap(_startingMapIndex);
                
                // SpawnPoint로 플레이어 이동
                if (_playerController != null)
                {
                    Vector3 spawnPosition = GetSpawnPosition(_startingMapIndex);
                    if (spawnPosition != Vector3.zero)
                    {
                        _playerController.transform.position = spawnPosition;
                    }
                }
            }
        }
        
        /// <summary>
        /// 맵 ID 인덱스 빌드
        /// </summary>
        private void BuildMapIndex()
        {
            _mapIdToIndex.Clear();
            for (int i = 0; i < _maps.Count; i++)
            {
                if (!string.IsNullOrEmpty(_maps[i].MapId) && _maps[i].MapRoot != null)
                {
                    _mapIdToIndex[_maps[i].MapId] = i;
                }
            }
        }
        
        /// <summary>
        /// 모든 맵 초기화 (비활성화)
        /// </summary>
        private void InitializeMaps()
        {
            foreach (var map in _maps)
            {
                if (map.MapRoot != null)
                {
                    map.MapRoot.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 인덱스로 맵 전환
        /// </summary>
        public void SwitchToMap(int mapIndex)
        {
            if (mapIndex < 0 || mapIndex >= _maps.Count) return;
            if (_currentMapIndex == mapIndex) return;
            
            // 현재 맵 비활성화
            if (_currentMapIndex >= 0 && _currentMapIndex < _maps.Count)
            {
                var currentMap = _maps[_currentMapIndex];
                if (currentMap.MapRoot != null)
                {
                    currentMap.MapRoot.SetActive(false);
                }
            }
            
            // 새 맵 활성화
            var newMap = _maps[mapIndex];
            if (newMap.MapRoot != null)
            {
                newMap.MapRoot.SetActive(true);
            }
            
            _currentMapIndex = mapIndex;
            
            // 이벤트 발행 (선택적)
            OnMapChanged?.Invoke(mapIndex, newMap.MapId);
        }
        
        /// <summary>
        /// ID로 맵 전환
        /// </summary>
        public void SwitchToMap(string mapId)
        {
            if (_mapIdToIndex.TryGetValue(mapId, out int index))
            {
                SwitchToMap(index);
            }
        }
        
        /// <summary>
        /// 현재 맵 인덱스 반환
        /// </summary>
        public int GetCurrentMapIndex()
        {
            return _currentMapIndex;
        }
        
        /// <summary>
        /// 현재 맵 ID 반환
        /// </summary>
        public string GetCurrentMapId()
        {
            if (_currentMapIndex >= 0 && _currentMapIndex < _maps.Count)
            {
                return _maps[_currentMapIndex].MapId;
            }
            return string.Empty;
        }
        
        /// <summary>
        /// 특정 맵이 활성화되어 있는지 확인
        /// </summary>
        public bool IsMapActive(int mapIndex)
        {
            return _currentMapIndex == mapIndex;
        }
        
        /// <summary>
        /// 특정 맵이 활성화되어 있는지 확인 (ID로)
        /// </summary>
        public bool IsMapActive(string mapId)
        {
            return GetCurrentMapId() == mapId;
        }
        
        /// <summary>
        /// 플레이어 스폰 위치 반환
        /// </summary>
        public Vector3 GetSpawnPosition(int mapIndex)
        {
            if (mapIndex >= 0 && mapIndex < _maps.Count)
            {
                var spawnPoint = _maps[mapIndex].SpawnPoint;
                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// 플레이어 스폰 위치 반환 (ID로)
        /// </summary>
        public Vector3 GetSpawnPosition(string mapId)
        {
            if (_mapIdToIndex.TryGetValue(mapId, out int index))
            {
                return GetSpawnPosition(index);
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// 맵 전환 이벤트
        /// </summary>
        public delegate void MapChangedEvent(int mapIndex, string mapId);
        public event MapChangedEvent OnMapChanged;
        
        /// <summary>
        /// 맵 데이터 리스트 반환 (읽기 전용)
        /// </summary>
        public IReadOnlyList<MapData> Maps => _maps;
        
        /// <summary>
        /// 전환 시간 반환
        /// </summary>
        public float TransitionDuration => _transitionDuration;
    }
}
