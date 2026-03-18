using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinder.Data
{
    /// <summary>
    /// 게임 저장 데이터 구조
    /// JSON 직렬화를 위해 Serializable 속성 사용
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        /// <summary>
        /// 플레이어 위치
        /// </summary>
        public Vector3Data playerPosition;
        
        /// <summary>
        /// 현재 맵 ID
        /// </summary>
        public string currentMapId;
        
        /// <summary>
        /// 해금된 능력 목록
        /// </summary>
        public List<int> unlockedAbilities;
        
        /// <summary>
        /// 능력 상자 열림 상태 (롤백 시 복원됨)
        /// </summary>
        public List<ChestStateData> abilityChestStates;
        
        /// <summary>
        /// 목숨 상자 열림 상태 (롤백 시 복원되지 않음 - 한 번 열면 영구)
        /// </summary>
        public List<ChestStateData> extraLifeChestStates;
        
        /// <summary>
        /// 현재 목숨 수 (기본 3 포함)
        /// </summary>
        public int lives;
        
        /// <summary>
        /// 저장 시간
        /// </summary>
        public string saveTime;
        
        /// <summary>
        /// 저장 데이터 버전 (향후 호환성을 위해)
        /// </summary>
        public int version = 1;
        
        public GameSaveData()
        {
            playerPosition = new Vector3Data();
            unlockedAbilities = new List<int>();
            abilityChestStates = new List<ChestStateData>();
            extraLifeChestStates = new List<ChestStateData>();
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    /// <summary>
    /// Vector3를 JSON 직렬화하기 위한 래퍼
    /// </summary>
    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;
        
        public Vector3Data() { }
        
        public Vector3Data(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
    
    /// <summary>
    /// 상자 상태 데이터
    /// </summary>
    [Serializable]
    public class ChestStateData
    {
        public string chestId;
        public bool isOpened;
        
        public ChestStateData() { }
        
        public ChestStateData(string id, bool opened)
        {
            chestId = id;
            isOpened = opened;
        }
    }
}
