using System.Collections.Generic;
using System.IO;
using Pathfinder.Abilities;
using Pathfinder.Data;
using Pathfinder.Core;
using Pathfinder.Player;
using Pathfinder.World;
using UnityEngine;

namespace Pathfinder.Core
{
    /// <summary>
    /// 게임 저장/로드 관리자 구현체
    /// JSON 파일로 저장
    /// </summary>
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        [Header("Save Settings")]
        [Tooltip("저장 파일 이름")]
        [SerializeField] private string _saveFileName = "savegame.json";
        
        [Tooltip("저장 폴더 이름")]
        [SerializeField] private string _saveFolderName = "Save";
        
        // 현재 저장 데이터 (메모리에 캐싱)
        private GameSaveData _currentSaveData;
        
        // DI 주입
        private IAbilityManager _abilityManager;
        private PlayerController _playerController;
        private MapManager _mapManager;
        
        // 저장 파일 경로
        private string SaveFolderPath => Path.Combine(Application.persistentDataPath, _saveFolderName);
        private string SaveFilePath => Path.Combine(SaveFolderPath, _saveFileName);
        
        // 저장된 맵 ID (Load 후 사용)
        public string LoadedMapId { get; private set; }
        
        // 워프 저장 예약 데이터
        private string _reservedMapId;
        private Vector3 _reservedPosition;
        private bool _hasReservedWarpSave;
        
        private void Awake()
        {
            // 저장 폴더 생성
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }
            
            // Play 시 저장 파일 초기화 (테스트용)
            #if UNITY_EDITOR
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
            #endif
        }
        
        private void Start()
        {
            // DI 주입 받기
            _abilityManager = FindObjectOfType<AbilityManager>();
            _playerController = FindObjectOfType<PlayerController>();
            _mapManager = FindObjectOfType<MapManager>();
            
            // MapManager 이벤트 구독
            if (_mapManager != null)
                _mapManager.OnMapChanged += OnMapChanged;
        }
        
        private void OnDestroy()
        {
            // MapManager 이벤트 구독 해제
            if (_mapManager != null)
                _mapManager.OnMapChanged -= OnMapChanged;
        }
        
        /// <summary>
        /// 맵 전환 이벤트 핸들러
        /// </summary>
        private void OnMapChanged(int mapIndex, string mapId)
        {
            Debug.Log($"[SAVE] Map changed to: {mapId}, hasReservedWarpSave: {_hasReservedWarpSave}");
            
            // 예약된 워프 저장이 있으면 수행
            if (_hasReservedWarpSave)
            {
                Debug.Log($"[SAVE] Executing reserved warp save - Map: {_reservedMapId}, Position: {_reservedPosition}");
                
                // 플레이어 위치를 예약된 위치로 이동
                if (_playerController != null)
                {
                    _playerController.transform.position = _reservedPosition;
                }
                
                // 저장 수행
                Save();
                
                // 예약 데이터 초기화
                _hasReservedWarpSave = false;
                _reservedMapId = null;
                _reservedPosition = Vector3.zero;
            }
        }
        
        /// <summary>
        /// 워프 후 저장 예약 (WarpPoint에서 호출)
        /// </summary>
        public void ReserveWarpSave(string targetMapId, Vector3 targetPosition)
        {
            _reservedMapId = targetMapId;
            _reservedPosition = targetPosition;
            _hasReservedWarpSave = true;
            Debug.Log($"[SAVE] Reserved warp save - Map: {targetMapId}, Position: {targetPosition}");
        }
        
        /// <summary>
        /// 게임 종료 시 자동 저장
        /// </summary>
        private void OnApplicationQuit()
        {
            if (_playerController != null)
            {
                Save();
            }
        }
        
        /// <summary>
        /// 첫 시작 시 초기화 (저장 데이터 없을 때 SpawnPoint에서 시작)
        /// </summary>
        public void InitializeFirstSpawn()
        {
            if (_mapManager == null) return;
            
            // 첫 시작 맵의 SpawnPoint로 이동
            Vector3 spawnPosition = _mapManager.GetSpawnPosition(0);
            if (_playerController != null && spawnPosition != Vector3.zero)
            {
                _playerController.transform.position = spawnPosition;
            }
        }
        
        /// <summary>
        /// 현재 게임 상태 저장
        /// </summary>
        public void Save()
        {
            if (_playerController == null)
            {
                Debug.LogError("[SAVE ERROR] PlayerController is null! Cannot save.");
                return;
            }
            
            _currentSaveData = new GameSaveData
            {
                playerPosition = new Vector3Data(_playerController.transform.position),
                currentMapId = GetCurrentMapId(),
                unlockedAbilities = GetUnlockedAbilityIndices(),
                abilityChestStates = GetAbilityChestStates(),
                extraLifeChestStates = GetExtraLifeChestStates(),
                lives = _abilityManager?.GetLives() ?? 3,
                saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            string json = JsonUtility.ToJson(_currentSaveData, true);
            File.WriteAllText(SaveFilePath, json);
        }
        
        /// <summary>
        /// 저장된 데이터 로드 및 적용 (기본: 모든 상자 복원)
        /// </summary>
        public bool Load()
        {
            return Load(true);
        }
        
        /// <summary>
        /// 저장된 데이터 로드 및 적용
        /// </summary>
        /// <param name="restoreExtraLifeChests">목숨 상자도 복원할지 여부 (롤백 시 false)</param>
        public bool Load(bool restoreExtraLifeChests)
        {
            if (!HasSaveData())
            {
                return false;
            }
            
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                _currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                
                if (_currentSaveData == null)
                {
                    return false;
                }
                
                LoadedMapId = _currentSaveData.currentMapId;
                
                ApplySaveData(_currentSaveData, restoreExtraLifeChests);
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LOAD ERROR] Load failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 저장 데이터 존재 여부 확인
        /// </summary>
        public bool HasSaveData()
        {
            return File.Exists(SaveFilePath);
        }
        
        /// <summary>
        /// 저장 데이터 삭제
        /// </summary>
        public void ClearSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                _currentSaveData = null;
                LoadedMapId = null;
            }
        }
        
        /// <summary>
        /// 모든 진행 상황 초기화 (저장 없이 죽었을 때)
        /// </summary>
        public void ResetAllProgress()
        {
            Debug.Log("[SAVE] ResetAllProgress - Resetting all progress");
            
            if (_abilityManager != null)
            {
                _abilityManager.ResetAbilities();
                Debug.Log("[SAVE] Abilities reset");
            }
            
            var chests = FindObjectsOfType<AbilityChest>();
            foreach (var chest in chests)
            {
                chest.ResetChest();
            }
            Debug.Log($"[SAVE] {chests.Length} chests reset");
            
            ClearSave();
            
            if (_mapManager != null && _playerController != null)
            {
                Vector3 spawnPosition = _mapManager.GetSpawnPosition(0);
                _playerController.transform.position = spawnPosition;
                Debug.Log($"[SAVE] Player moved to SpawnPoint: {spawnPosition}");
            }
        }
        
        /// <summary>
        /// 현재 저장 데이터 가져오기
        /// </summary>
        public GameSaveData GetCurrentSaveData()
        {
            return _currentSaveData;
        }
        
        /// <summary>
        /// 플레이어 위치만 저장 (맵 전환 시 사용)
        /// </summary>
        public void SavePlayerPosition(Vector3 position)
        {
            if (_currentSaveData == null)
            {
                // 기존 저장 데이터 로드 또는 새로 생성
                if (HasSaveData())
                {
                    Load();
                }
                else
                {
                    _currentSaveData = new GameSaveData();
                }
            }
            
            _currentSaveData.playerPosition = new Vector3Data(position);
            _currentSaveData.currentMapId = GetCurrentMapId();
            Save();
        }
        
        /// <summary>
        /// 저장된 맵 ID 가져오기 (파일에서 직접 읽기)
        /// </summary>
        public string GetSavedMapId()
        {
            if (!HasSaveData()) return string.Empty;
            
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                return data?.currentMapId ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 현재 맵 ID 가져오기
        /// </summary>
        private string GetCurrentMapId()
        {
            if (_mapManager != null)
            {
                return _mapManager.GetCurrentMapId();
            }
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        
        /// <summary>
        /// 해금된 능력 인덱스 목록 가져오기
        /// </summary>
        private List<int> GetUnlockedAbilityIndices()
        {
            var indices = new List<int>();
            if (_abilityManager == null) return indices;
            
            var unlockedAbilities = _abilityManager.GetUnlockedAbilities();
            foreach (var ability in unlockedAbilities)
            {
                indices.Add((int)ability);
            }
            
            return indices;
        }
        
        /// <summary>
        /// 능력 상자 상태만 가져오기
        /// </summary>
        private List<ChestStateData> GetAbilityChestStates()
        {
            var states = new List<ChestStateData>();
            var chests = FindObjectsOfType<AbilityChest>();
            
            foreach (var chest in chests)
            {
                if (chest.GetRewardType() != RewardType.ExtraLife)
                {
                    states.Add(new ChestStateData(chest.GetChestId(), chest.IsOpened()));
                }
            }
            
            Debug.Log($"[SAVE] Found {states.Count} ability chests");
            return states;
        }
        
        /// <summary>
        /// 목숨 상자 상태만 가져오기
        /// </summary>
        private List<ChestStateData> GetExtraLifeChestStates()
        {
            var states = new List<ChestStateData>();
            var chests = FindObjectsOfType<AbilityChest>();
            
            foreach (var chest in chests)
            {
                if (chest.GetRewardType() == RewardType.ExtraLife)
                {
                    states.Add(new ChestStateData(chest.GetChestId(), chest.IsOpened()));
                }
            }
            
            Debug.Log($"[SAVE] Found {states.Count} extra life chests");
            return states;
        }
        
        /// <summary>
        /// 저장 데이터 적용
        /// </summary>
        private void ApplySaveData(GameSaveData data, bool restoreExtraLifeChests)
        {
            // 1. 맵 전환 (맵 ID가 있을 경우)
            if (!string.IsNullOrEmpty(data.currentMapId) && _mapManager != null)
            {
                _mapManager.SwitchToMap(data.currentMapId);
            }
            
            // 2. 플레이어 위치 복원
            if (_playerController != null)
            {
                _playerController.transform.position = data.playerPosition.ToVector3();
            }
            
            // 3. 능력 복원
            if (_abilityManager != null)
            {
                _abilityManager.ResetAbilities();
                foreach (int abilityIndex in data.unlockedAbilities)
                {
                    _abilityManager.UnlockAbility((AbilityType)abilityIndex);
                }
                
                // 목숨 복원
                _abilityManager.SetLives(data.lives);
                Debug.Log($"[LOAD] Lives restored: {data.lives}");
            }
            
            // 4. 능력 상자 상태 복원 (항상 복원)
            RestoreAbilityChestStates(data.abilityChestStates);
            
            // 5. 목숨 상자 상태 복원 (롤백 시 제외)
            if (restoreExtraLifeChests && data.extraLifeChestStates != null)
            {
                RestoreExtraLifeChestStates(data.extraLifeChestStates);
            }
        }
        
        /// <summary>
        /// 능력 상자 상태 복원
        /// </summary>
        private void RestoreAbilityChestStates(List<ChestStateData> chestStates)
        {
            if (chestStates == null) return;
            
            var chests = FindObjectsOfType<AbilityChest>();
            var chestDict = new Dictionary<string, AbilityChest>();
            
            foreach (var chest in chests)
            {
                if (chest.GetRewardType() != RewardType.ExtraLife)
                {
                    chestDict[chest.GetChestId()] = chest;
                }
            }
            
            foreach (var state in chestStates)
            {
                if (chestDict.TryGetValue(state.chestId, out var chest))
                {
                    chest.SetOpened(state.isOpened);
                    Debug.Log($"[LOAD] Ability Chest {state.chestId}: SetOpened = {state.isOpened}");
                }
            }
        }
        
        /// <summary>
        /// 저장된 목숨만 업데이트 (롤백 후 사용)
        /// </summary>
        public void UpdateSavedLives(int lives)
        {
            if (!HasSaveData()) return;
            
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                _currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                _currentSaveData.lives = lives;
                json = JsonUtility.ToJson(_currentSaveData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SAVE] Updated saved lives to: {lives}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SAVE ERROR] UpdateSavedLives failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 목숨 상자 상태 복원
        /// </summary>
        private void RestoreExtraLifeChestStates(List<ChestStateData> chestStates)
        {
            if (chestStates == null) return;
            
            var chests = FindObjectsOfType<AbilityChest>();
            var chestDict = new Dictionary<string, AbilityChest>();
            
            foreach (var chest in chests)
            {
                if (chest.GetRewardType() == RewardType.ExtraLife)
                {
                    chestDict[chest.GetChestId()] = chest;
                }
            }
            
            foreach (var state in chestStates)
            {
                if (chestDict.TryGetValue(state.chestId, out var chest))
                {
                    chest.SetOpened(state.isOpened);
                    Debug.Log($"[LOAD] Extra Life Chest {state.chestId}: SetOpened = {state.isOpened}");
                }
            }
        }
    }
}
