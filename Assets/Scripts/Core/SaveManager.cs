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
        
        private void Awake()
        {
            // 저장 폴더 생성
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }
        }
        
        private void Start()
        {
            // DI 주입 받기
            _abilityManager = FindObjectOfType<AbilityManager>();
            _playerController = FindObjectOfType<PlayerController>();
            _mapManager = FindObjectOfType<MapManager>();
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
                chestStates = GetAllChestStates(),
                extraLives = _abilityManager?.GetExtraLives() ?? 0,
                saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // JSON으로 직렬화
            string json = JsonUtility.ToJson(_currentSaveData, true);
            
            // 파일에 저장
            File.WriteAllText(SaveFilePath, json);
        }
        
        /// <summary>
        /// 저장된 데이터 로드 및 적용
        /// </summary>
        public bool Load()
        {
            if (!HasSaveData())
            {
                return false;
            }
            
            try
            {
                // 파일에서 읽기
                string json = File.ReadAllText(SaveFilePath);
                _currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                
                if (_currentSaveData == null)
                {
                    return false;
                }
                
                // 저장된 맵 ID 저장 (DeathManager에서 사용)
                LoadedMapId = _currentSaveData.currentMapId;
                
                // 데이터 적용
                ApplySaveData(_currentSaveData);
                
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
        
        #region Helper Methods
        
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
        /// 모든 상자 상태 가져오기
        /// </summary>
        private List<ChestStateData> GetAllChestStates()
        {
            var states = new List<ChestStateData>();
            
            // 씬의 모든 AbilityChest 찾기
            var chests = FindObjectsOfType<AbilityChest>();
            foreach (var chest in chests)
            {
                states.Add(new ChestStateData(chest.GetChestId(), chest.IsOpened()));
            }
            
            return states;
        }
        
        /// <summary>
        /// 저장 데이터 적용
        /// </summary>
        private void ApplySaveData(GameSaveData data)
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
                
                // 추가 목숨 복원
                for (int i = 0; i < data.extraLives; i++)
                {
                    _abilityManager.AddExtraLife();
                }
            }
            
            // 4. 상자 상태 복원
            RestoreChestStates(data.chestStates);
        }
        
        /// <summary>
        /// 상자 상태 복원
        /// </summary>
        private void RestoreChestStates(List<ChestStateData> chestStates)
        {
            var chests = FindObjectsOfType<AbilityChest>();
            var chestDict = new Dictionary<string, AbilityChest>();
            
            foreach (var chest in chests)
            {
                chestDict[chest.GetChestId()] = chest;
            }
            
            foreach (var state in chestStates)
            {
                if (chestDict.TryGetValue(state.chestId, out var chest))
                {
                    chest.SetOpened(state.isOpened);
                }
            }
        }
        
        #endregion
    }
}
