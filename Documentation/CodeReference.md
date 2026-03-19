# Pathfinder Code Reference

## Overview
이 문서는 Pathfinder 프로젝트의 모든 C# 스크립트의 사용처와 재사용 방법을 정리합니다.

---

## Table of Contents
1. [Player System](#player-system)
2. [World System](#world-system)
3. [UI System](#ui-system)
4. [Ability System](#ability-system)
5. [Core System](#core-system)
6. [DI (Dependency Injection) System](#di-system)
7. [Data System](#data-system)
8. [Trap System](#trap-system)
9. [Common Interfaces](#common-interfaces)
10. [Enums](#enums)

---

## Player System

### PlayerController.cs
**경로:** `Assets/Scripts/Player/PlayerController.cs`

**용도:**
- 플레이어 기본 이동 및 점프 제어
- 더블점프, 대쉬 능력 구현
- 지면 감지 및 경사로 이동 처리
- 상호작용 시스템 (IInteractable 구현체와 연동)

**재사용 방법:**
```csharp
// 플레이어 프리팹에 부착하여 사용
// RequireComponent: Rigidbody2D

// 주요 설정 필드:
// - _moveSpeed: 이동 속도
// - _jumpForce: 점프 힘
// - _dashForce: 대쉬 힘
// - _enableDoubleJump: 더블점프 활성화
// - _enableDash: 대쉬 활성화
// - _interactionRadius: 상호작용 감지 반경
```

**의존성:**
- `IAbilityManager` (DI 주입)
- `IDeathManager` (DI 주입)
- `PlayerAnimator`

---

### DeathManager.cs
**경로:** `Assets/Scripts/Player/DeathManager.cs`

**용도:**
- 플레이어 사망 처리
- 체크포인트 기반 리스폰
- 목숨 소모 및 게임오버 처리

**재사용 방법:**
```csharp
// 씬에 하나만 존재해야 함
// GameOverUI 프리팹 연결 필요

// 주요 메서드:
// - OnPlayerDeath(): 사망 처리
// - SetCheckpoint(Vector3): 체크포인트 설정
// - Respawn(): 리스폰 실행
```

**의존성:**
- `IAbilityManager` (DI 주입)
- `ISaveManager` (DI 주입)
- `MapManager`
- `GameOverUI`

---

### AbilityManager.cs
**경로:** `Assets/Scripts/Player/AbilityManager.cs`

**용도:**
- 플레이어 능력 관리 (더블점프, 대쉬)
- 목숨(Lives) 관리
- 능력 해금 이벤트 발행

**재사용 방법:**
```csharp
// 플레이어 오브젝트에 부착
// IAbilityManager 인터페이스로 접근 권장

// 주요 메서드:
// - HasAbility(AbilityType): 능력 보유 확인
// - UnlockAbility(AbilityType): 능력 해금
// - GetLives(): 현재 목숨 반환
// - AddExtraLife(): 목숨 추가
// - ConsumeLife(): 목숨 소모

// 이벤트:
// - OnAbilityUnlocked: 능력 해금 시 발생
// - OnLivesChanged: 목숨 변경 시 발생
```

---

### PlayerAnimator.cs
**경로:** `Assets/Scripts/Player/PlayerAnimator.cs`

**용도:**
- 플레이어 애니메이션 상태 관리
- 스프라이트 좌우 반전
- 걷기, 점프, 대쉬, 사망 애니메이션 제어

**재사용 방법:**
```csharp
// RequireComponent: Animator, SpriteRenderer

// 주요 메서드:
// - SetWalking(bool): 걷기 상태 설정
// - SetJumping(bool): 점프 상태 설정
// - SetGrounded(bool): 지면 접촉 상태 설정
// - SetFacingDirection(float): 캐릭터 방향 설정
// - TriggerDeath(): 사망 애니메이션 트리거
// - TriggerDash(): 대쉬 애니메이션 트리거
```

---

### PlayerRewardPopup.cs
**경로:** `Assets/Scripts/Player/PlayerRewardPopup.cs`

**용도:**
- 플레이어 머리 위에 월드 스페이스 보상 메시지 표시
- 능력 획득, 목숨 획득 등 알림

**재사용 방법:**
```csharp
// 플레이어 오브젝트에 부착
// TextMeshPro (World Space) 필요

// 주요 메서드:
// - ShowReward(string): 일반 메시지 표시
// - ShowAbilityReward(string): 능력 획득 메시지
// - ShowExtraLifeReward(int): 목숨 획득 메시지
```

---

## World System

### CameraController.cs
**경로:** `Assets/Scripts/World/CameraController.cs`

**용도:**
- 플레이어 추적 카메라
- 맵 경계 제한
- Dead Zone 구현
- 맵 전환 시 부드러운 카메라 이동

**재사용 방법:**
```csharp
// 메인 카메라에 부착
// ICameraController 인터페이스 구현

// 주요 설정:
// - _followSpeed: 추적 속도
// - _useDeadZone: Dead Zone 사용 여부
// - _useBounds: 맵 경계 사용 여부
// - _minBounds, _maxBounds: 맵 경계

// 주요 메서드:
// - SetTarget(Transform): 추적 타겟 설정
// - SnapToTarget(): 즉시 타겟 위치로 이동
// - SetMapBounds(Vector2, Vector2): 맵 경계 설정
// - TransitionToPosition(Vector3): 부드러운 위치 이동
```

---

### MapManager.cs
**경로:** `Assets/Scripts/World/MapManager.cs`

**용도:**
- 여러 맵을 한 씬에서 SetActive로 전환
- 맵 ID 기반 전환
- 스폰 포인트 관리

**재사용 방법:**
```csharp
// 씬에 하나만 존재
// IMapManager 인터페이스 구현

// MapData 설정:
// - MapId: 맵 고유 ID
// - MapRoot: 맵 GameObject
// - DisplayName: 맵 이름
// - SpawnPoint: 플레이어 스폰 위치

// 주요 메서드:
// - SwitchToMap(int): 인덱스로 맵 전환
// - SwitchToMap(string): ID로 맵 전환
// - GetSpawnPosition(int/string): 스폰 위치 반환
// - GetCurrentMapId(): 현재 맵 ID 반환

// 이벤트:
// - OnMapChanged: 맵 전환 시 발생
```

---

### Portal.cs
**경로:** `Assets/Scripts/World/Portal.cs`

**용도:**
- 맵 간 이동 포탈
- 플레이어 터치 시 자동 전환

**재사용 방법:**
```csharp
// 트리거 콜라이더 필요 (자동 추가됨)
// IPortal 인터페이스 구현

// 주요 설정:
// - _portalId: 포탈 고유 ID
// - _targetMapId: 목표 맵 ID
// - _targetPortalId: 목표 포탈 ID (선택적)

// 주요 메서드:
// - Teleport(GameObject): 포탈 이동 실행
// - SetTarget(string, string): 목표 설정
```

---

### Checkpoint.cs
**경로:** `Assets/Scripts/World/Checkpoint.cs`

**용도:**
- 세이브포인트 구현
- 플레이어 리스폰 위치 저장

**재사용 방법:**
```csharp
// ICheckpoint 인터페이스 구현

// 주요 설정:
// - _checkpointId: 체크포인트 ID
// - _startActivated: 초기 활성화 여부
// - _activatedSprite, _deactivatedSprite: 스프라이트

// 주요 메서드:
// - Activate(): 체크포인트 활성화
// - IsActivated(): 활성화 상태 확인
// - GetPosition(): 위치 반환
```

---

### WarpPoint.cs
**경로:** `Assets/Scripts/World/WarpPoint.cs`

**용도:**
- 워프 포인트 (상호작용 기반 맵 이동)
- IInteractable 구현으로 E키 상호작용
- 체크포인트 기능 포함

**재사용 방법:**
```csharp
// IInteractable, ICheckpoint 인터페이스 구현

// 주요 설정:
// - _warpPointId: 워프포인트 ID
// - _targetMapId: 목표 맵 ID
// - _targetWarpPointId: 목표 워프포인트 ID
// - _interactionText: 상호작용 프롬프트 텍스트

// 주요 메서드:
// - OnInteract(): 상호작용 실행 (IInteractable)
// - Activate(): 워프포인트 활성화
```

---

### ClearZone.cs
**경로:** `Assets/Scripts/World/ClearZone.cs`

**용도:**
- 게임 클리어 존
- 플레이어 도달 시 클리어 처리

**재사용 방법:**
```csharp
// 트리거 콜라이더 필요
// 플레이어 태그 "Player" 감지

// 단순한 컴포넌트: OnTriggerEnter2D에서 로그 출력
```

---

### ParallaxBackground.cs
**경로:** `Assets/Scripts/World/ParallaxBackground.cs`

**용도:**
- 패럴랙스 배경 레이어 관리
- 자식 레이어 자동 수집

**재사용 방법:**
```csharp
// 배경 오브젝트의 부모에 부착
// 자식으로 ParallaxLayer 컴포넌트들 배치

// 주요 메서드:
// - AddLayer(ParallaxLayer): 레이어 추가
// - RemoveLayer(ParallaxLayer): 레이어 제거
```

---

### ParallaxLayer.cs
**경로:** `Assets/Scripts/World/ParallaxLayer.cs`

**용도:**
- 개별 패럴랙스 레이어
- 카메라 이동에 따른 배경 스크롤
- Shader Graph 또는 일반 셰이더 지원

**재사용 방법:**
```csharp
// SpriteRenderer 필요
// ExecuteInEditMode로 에디터에서도 작동

// 주요 설정:
// - _parallaxSpeed: 패럴랙스 속도 (0~1)
// - _offsetMultiplier: 오프셋 배수
// - _useShaderGraph: Shader Graph 사용 여부
// - _offsetProperty: 셰이더 프로퍼티 이름
```

---

## UI System

### LifeUI.cs
**경로:** `Assets/Scripts/UI/LifeUI.cs`

**용도:**
- 플레이어 목숨 표시
- 하트 문자로 목숨 수 표현

**재사용 방법:**
```csharp
// Canvas 내 UI Text 필요

// 주요 설정:
// - _lifeText: 목숨 텍스트
// - _heartChar: 하트 문자 (기본: \u2665)
// - _baseLives: 기본 목숨 수

// AbilityManager의 OnLivesChanged 이벤트 구독
```

---

### InteractionPromptUI.cs
**경로:** `Assets/Scripts/UI/InteractionPromptUI.cs`

**용도:**
- 상호작용 프롬프트 표시
- 월드 스페이스에서 오브젝트 머리 위에 표시

**재사용 방법:**
```csharp
// World Space Canvas에 부착
// TextMeshProUGUI 필요

// 주요 설정:
// - _promptText: 텍스트 컴포넌트
// - _defaultText: 기본 텍스트
// - _yOffset: Y축 오프셋

// 주요 메서드:
// - Show(Transform, string): 프롬프트 표시
// - Hide(): 프롬프트 숨김
// - SetText(string): 텍스트 변경
```

---

### GameOverUI.cs
**경로:** `Assets/Scripts/UI/GameOverUI.cs`

**용도:**
- 게임오버 화면 표시
- 재시작 버튼 처리

**재사용 방법:**
```csharp
// Canvas에 부착
// GameOver Panel GameObject 연결 필요

// 주요 메서드:
// - Show(): 게임오버 화면 표시
// - OnRestartClick(): 재시작 버튼 클릭 핸들러
```

---

### RewardPopupUI.cs
**경로:** `Assets/Scripts/UI/RewardPopupUI.cs`

**용도:**
- 보상 획득 팝업 UI
- 능력, 목숨 획득 시 메시지 표시

**재사용 방법:**
```csharp
// Canvas에 부착
// TextMeshProUGUI, Image, Animator 선택적

// 주요 메서드:
// - ShowReward(string, Color?): 보상 메시지 표시
// - ShowAbilityReward(string): 능력 획득 메시지
// - ShowExtraLifeReward(int): 목숨 획득 메시지
// - Hide(): 팝업 숨김
```

---

## Ability System

### AbilityChest.cs
**경로:** `Assets/Scripts/Abilities/AbilityChest.cs`

**용도:**
- 능력 또는 목숨 보상 상자
- IInteractable 구현으로 E키 상호작용

**재사용 방법:**
```csharp
// IInteractable 인터페이스 구현
// SpriteRenderer 필요

// 주요 설정:
// - _rewardType: 보상 타입 (DoubleJump, Dash, ExtraLife)
// - _chestId: 상자 고유 ID
// - _closedSprite, _openedSprite: 스프라이트

// 주요 메서드:
// - OnInteract(): 상호작용 실행 (IInteractable)
// - SetOpened(bool): 상자 상태 설정 (저장/로드용)
// - ResetChest(): 상자 리셋
```

---

### AbilityUnlockable.cs
**경로:** `Assets/Scripts/Abilities/AbilityUnlockable.cs`

**용도:**
- 능력 구체 (터치 시 능력 해금)
- 회전 및 부유 애니메이션

**재사용 방법:**
```csharp
// IAbilityUnlockable 인터페이스 구현
// 트리거 콜라이더 필요 (자동 추가)

// 주요 설정:
// - _abilityType: 해금할 능력 종류
// - _destroyOnCollect: 획득 시 파괴 여부
// - _collectEffect: 획득 이펙트

// 주요 메서드:
// - Unlock(): 능력 해금
// - GetAbilityType(): 능력 타입 반환
// - IsUnlocked(): 해금 여부 반환
```

---

## Core System

### GameInstaller.cs
**경로:** `Assets/Scripts/Core/GameInstaller.cs`

**용도:**
- 게임 전역 서비스 DI 등록
- Installer 상속

**재사용 방법:**
```csharp
// Installer 상속
// 씬에 하나만 존재

// 등록하는 서비스:
// - IAbilityManager (AbilityManager)
// - IDeathManager (DeathManager)
// - IMapManager (MapManager)
// - ISaveManager (SaveManager)
```

---

### SaveManager.cs
**경로:** `Assets/Scripts/Core/SaveManager.cs`

**용도:**
- 게임 저장/로드 (JSON 파일)
- 워프 저장 예약 시스템
- 상자 상태 저장/복원

**재사용 방법:**
```csharp
// ISaveManager 인터페이스 구현
// 씬에 하나만 존재

// 저장 파일 위치: Application.persistentDataPath/Save/savegame.json

// 주요 메서드:
// - Save(): 현재 상태 저장
// - Load(): 저장 데이터 로드
// - Load(bool): 목숨 상자 복원 여부 지정
// - HasSaveData(): 저장 데이터 존재 확인
// - ClearSave(): 저장 데이터 삭제
// - ReserveWarpSave(string, Vector3): 워프 저장 예약
// - ResetAllProgress(): 모든 진행 상황 초기화
```

---

## DI System

### DIContainer.cs
**경로:** `Assets/Scripts/Core/DI/DIContainer.cs`

**용도:**
- 의존성 주입 컨테이너
- 서비스 등록 및 해결
- 필드/프로퍼티 주입

**재사용 방법:**
```csharp
// 주요 메서드:
// - Register<TInterface, TImplementation>(): 서비스 등록
// - RegisterInstance<T>(): 인스턴스 등록
// - Resolve<T>(): 서비스 해결
// - InjectInto(object): 의존성 주입

// 수명 주기:
// - ServiceLifetime.Singleton: 싱글톤
// - ServiceLifetime.Transient: 매번 새 인스턴스
```

---

### DIContainerManager.cs
**경로:** `Assets/Scripts/Core/DI/DIContainerManager.cs`

**용도:**
- 전역/씬 DI 컨테이너 관리
- 자동 Installer 실행
- 씬 로드/언로드 시 컨테이너 관리

**재사용 방법:**
```csharp
// 정적 클래스 - 인스턴스 생성 불필요

// 주요 프로퍼티:
// - Global: 전역 컨테이너
// - CurrentSceneContainer: 현재 씬 컨테이너

// 주요 메서드:
// - Resolve<T>(): 서비스 해결
// - InjectInto(object/GameObject): 의존성 주입
// - ClearGlobal(): 전역 컨테이너 초기화
// - ClearAll(): 모든 컨테이너 초기화
```

---

### Installer.cs
**경로:** `Assets/Scripts/Core/DI/Installer.cs`

**용도:**
- Installer 베이스 클래스
- MonoBehaviour 기반

**재사용 방법:**
```csharp
// 상속하여 Install() 구현
public class MyInstaller : Installer
{
    public override void Install(DIContainer container)
    {
        container.RegisterInstance<IMyService>(myService);
    }
}
```

---

### InjectAttribute.cs
**경로:** `Assets/Scripts/Core/DI/InjectAttribute.cs`

**용도:**
- DI 주입 표시 어트리뷰트

**재사용 방법:**
```csharp
[Inject] private IAbilityManager _abilityManager;
[InjectOptional] private ILogger _logger;
```

---

### RootContext.cs
**경로:** `Assets/Scripts/Core/DI/RootContext.cs`

**용도:**
- 씬 내 DI 스코프 관리
- Installer 실행 순서 관리
- 자식 오브젝트에 DI 주입

**재사용 방법:**
```csharp
// 씬의 루트 GameObject에 부착
// Installer 리스트에 실행할 Installer들 추가

// 주요 설정:
// - _installers: 실행할 Installer 목록
// - _injectChildren: 자식에 자동 주입 여부
// - _executionOrder: 실행 순서
```

---

### ServiceLifetime.cs
**경로:** `Assets/Scripts/Core/DI/ServiceLifetime.cs`

**용도:**
- 서비스 수명 주기 열거형

**재사용 방법:**
```csharp
public enum ServiceLifetime
{
    Singleton,  // 싱글톤
    Transient   // 매번 새 인스턴스
}
```

---

## Data System

### GameSaveData.cs
**경로:** `Assets/Scripts/Data/GameSaveData.cs`

**용도:**
- 게임 저장 데이터 구조
- JSON 직렬화용

**재사용 방법:**
```csharp
[Serializable]
public class GameSaveData
{
    public Vector3Data playerPosition;    // 플레이어 위치
    public string currentMapId;            // 현재 맵 ID
    public List<int> unlockedAbilities;    // 해금된 능력
    public List<ChestStateData> abilityChestStates;   // 능력 상자 상태
    public List<ChestStateData> extraLifeChestStates; // 목숨 상자 상태
    public int lives;                      // 목숨
    public string saveTime;                // 저장 시간
}

// Vector3Data: Vector3 JSON 직렬화용 래퍼
// ChestStateData: 상자 상태 데이터
```

---

## Trap System

### MovingPlatform.cs
**경로:** `Assets/Scripts/Traps/MovingPlatform.cs`

**용도:**
- 이동 플랫폼
- 좌우/상하 이동 지원
- 양 끝에서 대기 기능

**재사용 방법:**
```csharp
// RequireComponent: Rigidbody2D
// Rigidbody2D는 Kinematic으로 설정됨

// 주요 설정:
// - _moveSpeed: 이동 속도
// - _moveDistance: 이동 거리
// - _moveDirection: 이동 방향
// - _waitTime: 양 끝 대기 시간
```

---

### SpikeTrap.cs
**경로:** `Assets/Scripts/Traps/SpikeTrap.cs`

**용도:**
- 가시 함정
- 플레이어 닿으면 즉시 사망

**재사용 방법:**
```csharp
// RequireComponent: Collider2D
// "Trap" 태그 자동 설정

// PlayerController에서 "Trap" 태그 감지하여 Die() 호출

// 주요 설정:
// - _hitEffect: 충돌 이펙트
// - _hitSound: 충돌 사운드
```

---

## Common Interfaces

### IInteractable.cs
**경로:** `Assets/Scripts/Common/Interfaces/IInteractable.cs`

**용도:**
- 상호작용 가능한 오브젝트 인터페이스

**재사용 방법:**
```csharp
public interface IInteractable
{
    string GetInteractionText();  // 프롬프트 텍스트
    bool CanInteract();           // 상호작용 가능 여부
    void OnInteract();            // 상호작용 실행
    Transform GetPromptTransform(); // 프롬프트 위치
}

// 구현체: AbilityChest, WarpPoint
```

---

### IAbilityManager.cs
**경로:** `Assets/Scripts/Player/Interfaces/IAbilityManager.cs`

**용도:**
- 능력 관리 인터페이스

**재사용 방법:**
```csharp
public interface IAbilityManager
{
    bool HasAbility(AbilityType ability);
    void UnlockAbility(AbilityType ability);
    IReadOnlyCollection<AbilityType> GetUnlockedAbilities();
    void ResetAbilities();
    event Action<AbilityType> OnAbilityUnlocked;
    
    int GetLives();
    void AddExtraLife();
    bool ConsumeLife();
    event Action<int> OnLivesChanged;
}
```

---

### IDeathManager.cs
**경로:** `Assets/Scripts/Player/Interfaces/IDeathManager.cs`

**용도:**
- 사망 관리 인터페이스

**재사용 방법:**
```csharp
public interface IDeathManager
{
    void OnPlayerDeath();
    void Respawn();
    Vector3 GetLastCheckpoint();
    void SetCheckpoint(Vector3 position);
}
```

---

### ISaveManager.cs
**경로:** `Assets/Scripts/Core/Interfaces/ISaveManager.cs`

**용도:**
- 저장 관리 인터페이스

**재사용 방법:**
```csharp
public interface ISaveManager
{
    void Save();
    bool Load();
    bool Load(bool restoreExtraLifeChests);
    bool HasSaveData();
    void ClearSave();
    GameSaveData GetCurrentSaveData();
    void SavePlayerPosition(Vector3 position);
    void UpdateSavedLives(int lives);
}
```

---

### IPortal.cs
**경로:** `Assets/Scripts/World/Interfaces/IPortal.cs`

**용도:**
- 포탈 인터페이스

**재사용 방법:**
```csharp
public interface IPortal
{
    void Teleport(GameObject player);
    string GetTargetMapId();
    string GetTargetPortalId();
}
```

---

### ICheckpoint.cs
**경로:** `Assets/Scripts/World/Interfaces/ICheckpoint.cs`

**용도:**
- 체크포인트 인터페이스

**재사용 방법:**
```csharp
public interface ICheckpoint
{
    void Activate();
    bool IsActivated();
    Vector3 GetPosition();
}
```

---

### ICameraController.cs
**경로:** `Assets/Scripts/World/Interfaces/ICameraController.cs`

**용도:**
- 카메라 컨트롤러 인터페이스

**재사용 방법:**
```csharp
public interface ICameraController
{
    void SetTarget(Transform target);
    void SnapToTarget();
    void SetMapBounds(Vector2 min, Vector2 max);
}
```

---

### IMapManager.cs
**경로:** `Assets/Scripts/World/Interfaces/IMapManager.cs`

**용도:**
- 맵 관리 인터페이스

**재사용 방법:**
```csharp
public interface IMapManager
{
    void SwitchToMap(int mapIndex);
    void SwitchToMap(string mapId);
    int GetCurrentMapIndex();
    string GetCurrentMapId();
    bool IsMapActive(int mapIndex);
    bool IsMapActive(string mapId);
}
```

---

### IAbilityUnlockable.cs
**경로:** `Assets/Scripts/Abilities/Interfaces/IAbilityUnlockable.cs`

**용도:**
- 능력 해금 가능 인터페이스

**재사용 방법:**
```csharp
public interface IAbilityUnlockable
{
    AbilityType GetAbilityType();
    void Unlock();
    bool IsUnlocked();
}
```

---

## Enums

### AbilityType.cs
**경로:** `Assets/Scripts/Player/Enums/AbilityType.cs`

**용도:**
- 플레이어 능력 타입

**재사용 방법:**
```csharp
public enum AbilityType
{
    None,
    DoubleJump,  // 더블점프
    Dash         // 대쉬
}
```

---

### RewardType.cs
**경로:** `Assets/Scripts/Abilities/Enums/RewardType.cs`

**용도:**
- 보상 타입

**재사용 방법:**
```csharp
public enum RewardType
{
    DoubleJump,  // 더블점프
    Dash,        // 대쉬
    ExtraLife    // 추가 목숨
}
```

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────┐
│                     Game Scene                               │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    RootContext                           ││
│  │  ┌─────────────────────────────────────────────────────┐││
│  │  │                 GameInstaller                        │││
│  │  │  - IAbilityManager → AbilityManager                  │││
│  │  │  - IDeathManager → DeathManager                      │││
│  │  │  - IMapManager → MapManager                          │││
│  │  │  - ISaveManager → SaveManager                        │││
│  │  └─────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────┘│
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    Player    │  │    World     │  │      UI      │      │
│  │  Controller  │  │   Systems    │  │   Systems    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│         │                 │                 │                │
│         └─────────────────┼─────────────────┘                │
│                           │                                  │
│                    DI Injection                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Dependency Flow

```
PlayerController
    ├── [Inject] IAbilityManager → AbilityManager
    ├── [Inject] IDeathManager → DeathManager
    └── PlayerAnimator

DeathManager
    ├── [Inject] IAbilityManager → AbilityManager
    ├── [Inject] ISaveManager → SaveManager
    ├── MapManager (FindFirstObjectByType)
    └── GameOverUI

SaveManager
    ├── IAbilityManager (FindFirstObjectByType)
    ├── PlayerController (FindFirstObjectByType)
    └── MapManager (FindFirstObjectByType)

AbilityChest
    ├── [Inject] IAbilityManager → AbilityManager
    └── PlayerRewardPopup (플레이어에서 찾음)

WarpPoint
    ├── SaveManager (FindFirstObjectByType)
    ├── DeathManager (FindFirstObjectByType)
    └── MapManager (FindFirstObjectByType)

Portal
    └── [Inject] IMapManager → MapManager

Checkpoint
    └── [Inject] IDeathManager → DeathManager
```

---

## File Count Summary

| Category | File Count |
|----------|------------|
| Player | 5 |
| World | 8 |
| UI | 4 |
| Abilities | 2 |
| Core | 2 |
| DI | 6 |
| Data | 1 |
| Traps | 2 |
| Common/Interfaces | 1 |
| Player/Interfaces | 3 |
| World/Interfaces | 4 |
| Abilities/Interfaces | 1 |
| Core/Interfaces | 1 |
| Enums | 2 |
| **Total** | **42** |