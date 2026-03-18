# Agent Guidelines

## 📚 문서 참조 가이드

### 상황별 참조 문서

**코어 아키텍처 작업 시:**
- `DI_Library.md` - DI Container 구현 구조
- `SOLID_Coding_Standard.md` - 코딩 표준

**게임 시스템 작업 시:**
- `Docs_WarpPoint.md` - 워프/체크포인트 시스템
- `WarpPoint_Setup_Guide.md` - 설정 방법

**Unity 씬 작업 시:**
- `Unity_Scene_Setup_Guide.md` - 씬 구성
- `Animation_Setup_Guide.md` - 애니메이션 설정

**타일맵 작업 시:**
- **⚠️ 코드 생성 금지** - Unity Tile Palette 사용
- `Docs_TilemapGenerators.md` - 삭제된 생성기 코드 (참조용)

**진행 상황 확인:**
- `TaskList.md` - 작업 목록

---

## DI Container Implementation Reference

When implementing DI Container features, refer to `DI_Library.md` for the core structure and patterns.

### Key Reference Points from DI_Library.md

#### Core Components
- **DIContainer.cs**: Main container class with service registration and resolution
- **InjectAttribute**: Marks constructors, fields, or properties for injection
- **InjectOptionalAttribute**: Marks optional dependencies that can be null

#### Implementation Patterns

1. **Service Registration**
   - `Register<TInterface, TImplementation>()`: Maps interface to implementation
   - `RegisterInstance<T>(T instance)`: Registers pre-created singleton instances

2. **Dependency Resolution**
   - `Resolve<T>()`: Resolves service with all dependencies recursively
   - `Resolve(Type type)`: Non-generic version
   - Circular dependency detection via `_resolvingStack`

3. **Injection Types**
   - Constructor injection (preferred, with `[Inject]` attribute)
   - Field injection (`[Inject]` on fields)
   - Property injection (`[Inject]` on properties)
   - Optional injection (`[InjectOptional]` - skips if not registered)

4. **Unity Integration**
   - `InjectInto(object instance)`: Injects dependencies into existing MonoBehaviour
   - Call in `Awake()` of bootstrap/setup classes

#### Key Implementation Details

```csharp
// Constructor selection priority:
// 1. Constructor with [Inject] attribute
// 2. Constructor with most parameters (if no [Inject] marked)

// Member injection order:
// 1. All fields with [Inject] or [InjectOptional]
// 2. All properties with [Inject] or [InjectOptional]

// Error handling:
// - Throws InvalidOperationException for circular dependencies
// - Throws InvalidOperationException for unregistered services
// - Silent skip for optional dependencies not found
```

#### Container Hierarchy (Extended)

For multi-scoped containers (Global + Scene):
- Global Container: Application-lifetime singletons
- Scene Containers: Scene-specific services
- LIFO resolution: Search from current scene up to global

#### File Organization
```
Assets/Scripts/Core/DI/
├── InjectAttribute.cs      # [Inject], [InjectOptional]
├── ServiceLifetime.cs      # Singleton, Transient enum
├── DIContainer.cs          # Core container (refer to DI_Library.md structure)
├── DIContainerManager.cs   # Static list + LIFO management
├── Installer.cs            # MonoBehaviour installer base
└── RootContext.cs          # Scene installer orchestrator
```

### Unity-Specific Guidelines

1. **Installer Pattern**: Use `Installer` MonoBehaviour with `RootContext` for scene setup
2. **Timing**: Register in `Awake()`, inject in `Start()` to ensure dependencies ready
3. **Factories**: For dynamic instantiation (Enemy, Projectile), inject factory then call `InjectInto` on created instances
4. **Lifecycle**: Scene containers auto-dispose on scene unload (clear singletons)

### Testing Considerations

- Clear containers between tests: `container.Clear()`
- Not thread-safe - use one container per test
- Optional dependencies useful for test doubles

---

## ⚠️ 타일맵 작업 규칙

**코드로 자동 생성하지 않음**
- 모든 타일맵은 Unity 에디터에서 수동으로 배치
- Tile Palette 사용
- 자동 생성기는 삭제됨: `Docs_TilemapGenerators.md` 참조

---

## General Development Notes

- Follow existing code conventions in the codebase
- Maintain consistency with established patterns
- Document public APIs with XML comments
- Prefer explicit over implicit for maintainability

---

## Debug Log Cleanup - 2025-03-17

**Changes Made:**
Removed all Debug.Log, Debug.LogWarning, and Debug.LogError statements from the following files:

1. **WarpPoint.cs** - Removed player enter/exit logs, warp sequence logs
2. **MapManager.cs** - Removed invalid index warning, map activation/deactivation logs
3. **SaveManager.cs** - Removed awake/start logs, kept only [SAVE] and [LOAD] logs
4. **PlayerController.cs** - Removed ability/death manager logs, jump/dash logs, interaction detection logs
5. **DeathManager.cs** - No logs found (clean)
6. **AbilityChest.cs** - Removed reset log
7. **AbilityManager.cs** - Removed unlock and extra life logs
8. **AbilityUnlockable.cs** - Removed warning and unlock logs
9. **CameraController.cs** - Removed transition complete log
10. **Portal.cs** - Removed auto-collider log, teleport logs
11. **Checkpoint.cs** - Removed activation log
12. **RewardPopupUI.cs** - Removed show log
13. **GameInstaller.cs** - Removed ability manager error log
14. **RootContext.cs** - Removed installer warning/error logs
15. **DIContainerManager.cs** - Removed initialization and disposal logs

**Result:** Cleaner console output, only essential save/load logs remain for debugging.

**Files Modified:**
- Assets/Scripts/World/WarpPoint.cs
- Assets/Scripts/World/MapManager.cs
- Assets/Scripts/Core/SaveManager.cs
- Assets/Scripts/Player/PlayerController.cs
- Assets/Scripts/Player/AbilityManager.cs
- Assets/Scripts/Abilities/AbilityChest.cs
- Assets/Scripts/Abilities/AbilityUnlockable.cs
- Assets/Scripts/World/CameraController.cs
- Assets/Scripts/World/Portal.cs
- Assets/Scripts/World/Checkpoint.cs
- Assets/Scripts/UI/RewardPopupUI.cs
- Assets/Scripts/Core/GameInstaller.cs
- Assets/Scripts/Core/DI/RootContext.cs
- Assets/Scripts/Core/DI/DIContainerManager.cs

**Commit:** e83cad9 - Remove Debug.Log statements from game scripts

---

## MovingPlatform Update - 2025-03-18

**Changes Made:**
Removed player following logic from MovingPlatform. Platform now only moves on its own without carrying the player.

**Files Modified:**
- Assets/Scripts/Traps/MovingPlatform.cs

**Commit:** 9461b5f - refactor: Remove player following logic from MovingPlatform

---

## Save System Improvements - 2025-03-18

**Changes Made:**
1. **SaveManager.cs**
   - Added `OnApplicationQuit()` for auto-save on game exit
   - Added `LoadedMapId` property to track loaded map for death respawn
   - Improved `ApplySaveData()` to switch maps before restoring position
   - Added MapManager reference for proper map ID handling

2. **MapManager.cs**
   - Modified `Start()` to check for save data and load or initialize first spawn
   - Added `InitializeFirstSpawn()` to place player at SpawnPoint when no save exists
   - Added SaveManager and PlayerController references

3. **WarpPoint.cs**
   - Modified `WarpSequence()` to save on arrival (destination)
   - Added checkpoint update at destination after warp completes
   - Maintains save on departure (origin) for safety

4. **DeathManager.cs**
   - Added MapManager reference for map switching
   - Modified `OnPlayerDeath()` to switch to saved map before loading
   - Ensures player respawns in correct map when dying in different map

**Behavior Changes:**
- Game auto-saves on exit (no manual save needed)
- First play starts at SpawnPoint instead of saved position
- Warping to new map saves arrival position (not departure)
- Dying in 1-2 respawns at 1-2's WarpPoint (not 1-1)

**Files Modified:**
- Assets/Scripts/Core/SaveManager.cs
- Assets/Scripts/World/MapManager.cs
- Assets/Scripts/World/WarpPoint.cs
- Assets/Scripts/Player/DeathManager.cs

**Commit:** 13572dd - feat: Improve save system with auto-save and spawn point logic

---

## Warp Save Location Fix - 2025-03-18

**Changes Made:**
Modified WarpPoint.cs to save ONLY at destination, not at origin.

**WarpPoint.cs Changes:**
- Removed save at departure (origin) - lines 118-120 deleted
- Improved destination save timing:
  - Changed from `WaitForFixedUpdate` to `WaitForSeconds(0.1f)`
  - Ensures map transition is fully complete before saving
- Result: Warping from 1-1 to 1-2 now saves at 1-2 (not 1-1)

**Additional Changes in this commit:**
1. **SaveManager.cs** - Added debug code to delete save file on Awake (UNITY_EDITOR only, for testing)
2. **PlayerController.cs** - Fixed wall detection:
   - Reduced `_wallCheckDistance` from 0.35f to 0.2f
   - Added `_excludeWallTags` array (Platform, MovingPlatform)
   - Added `IsExcludedFromWall()` method to ignore tiles near walls

**Behavior:**
- 1-1 → 1-2 워프 시: 1-2 도착 후에만 저장됨
- 테스트 후 SaveManager.cs의 디버그 코드 제거 필요

**Files Modified:**
- Assets/Scripts/World/WarpPoint.cs
- Assets/Scripts/Core/SaveManager.cs (debug code)
- Assets/Scripts/Player/PlayerController.cs (wall detection fix)

**Commit:** 48394d0 - feat: Modify warp save to save only at destination (not origin)

---

## Death Respawn Fix - 2025-03-18

**Problem:**
사망 시 저장된 위치로 돌아가지만, 물리 리셋과 무적 상태가 적용되지 않음. 또한 처음 시작 위치로 돌아가는 문제 발생.

**Root Cause:**
DeathManager.OnPlayerDeath()에서 SaveManager.Load() 후 Respawn()을 호출하지 않았음.
- Load()만 하면 위치는 복원되지만 rb.velocity 리셋 안 됨
- 무적 상태(Invincibility) 시작 안 됨
- _isRespawning 플래그 처리 안 됨

**Solution:**
1. **RespawnFromSave() 메서드 추가**
   - Load() 후 호출하여 물리 리셋과 무적 상태 시작
   - Respawn()과 분리하여 체크포인트 위치 사용 여부 차이

2. **DeathManager.cs 수정**
   - Load() 후 RespawnFromSave() 호출하도록 변경
   - 물리 속도 리셋 (rb.linearVelocity = Vector2.zero)
   - 무적 코루틴 시작 (InvincibilityCoroutine)

**Files Modified:**
- Assets/Scripts/Player/DeathManager.cs

**Commit:** fc74b46 - fix: Call RespawnFromSave after Load to reset physics and start invincibility

---

## Save Map ID Fix - 2025-03-18

**Problem:**
1-2에서 사망해도 1-1로 리스폰됨. 저장은 1-2 위치로 되지만 사망 시 1-1로 감.

**Root Cause:**
`LoadedMapId`는 `Load()` 호출 시에만 업데이트됨. 워프 시 `Save()`만 호출되므로 `LoadedMapId`는 여전히 이전 맵("1-1")을 가리킴.

```
흐름:
1. 게임 시작: Load() → LoadedMapId = "1-1"
2. 1-1 → 1-2 워프: Save()만 호출, LoadedMapId는 "1-1" 유지
3. 1-2에서 사망: LoadedMapId("1-1")로 맵 전환 → 잘못된 맵!
```

**Solution:**
1. **SaveManager.cs**
   - `GetSavedMapId()` 메서드 추가
   - 저장 파일에서 `currentMapId`를 직접 읽어 반환
   - 파일 I/O 예외 처리 포함

2. **DeathManager.cs**
   - `saveManager.LoadedMapId` → `saveManager.GetSavedMapId()`로 변경
   - Load() 전에 최신 맵 ID를 읽어 맵 전환

**Behavior Change:**
- 사망 시 항상 저장된 최신 맵 ID로 전환
- 1-2에서 사망 → 1-2로 리스폰

**Files Modified:**
- Assets/Scripts/Core/SaveManager.cs
- Assets/Scripts/Player/DeathManager.cs

**Commit:** 3382184 - fix: Use GetSavedMapId() to read correct map ID when respawning

---

## Save System Logging & Debug Cleanup - 2025-03-18

**Changes:**
1. **Debug Code Removal**
   - SaveManager.Awake()의 파일 삭제 코드 제거
   - 저장 파일이 이제 유지됨 (Play 시 초기화되지 않음)

2. **Position Logging Added**
   - Save() 시: `[SAVE] Map: {mapId}, Position: {x, y, z}` 로그 출력
   - Load() 시: `[LOAD] Map: {mapId}, Position: {x, y, z}` 로그 출력
   - Unity Console에서 Save/Load 위치값 확인 가능

**Purpose:**
- Save 시점의 위치와 Load 시점의 위치가 같은지 확인
- 저장/로드 시스템 디버깅 용이

**Files Modified:**
- Assets/Scripts/Core/SaveManager.cs

**Commit:** af8adba - feat: Add logging for save/load position and remove debug code

---

## Debug Logging for Warp & Death - 2025-03-18

**Problem:**
- Warp는 되지만 Save 로그가 안 뜸
- P 키로 죽었을 때 Death 로그가 안 뜸
- 저장 파일 초기화 여부 확인 필요

**Changes:**
1. **WarpPoint.cs**
   - `OnInteract()` - 상호작용 호출 로그
   - `WarpSequence()` - 전체 흐름 추적 로그
   - `ResolveServices()` - Manager null 체크 로그

2. **PlayerController.cs**
   - `Die()` - _deathManager null 체크 로그

3. **DeathManager.cs**
   - `OnPlayerDeath()` - _saveManager, HasSaveData 상태 로그

4. **SaveManager.cs**
   - Play 시 저장 파일 초기화 코드 복원 (UNITY_EDITOR)

**Expected Log Flow:**
```
[WARP] OnInteract called - CanInteract: True, _isPlayerInRange: True, _isWarping: False
[WARP] WarpSequence START - _isWarping: False
[WARP] Target: 1-2, SaveManager: True
[WARP] Starting PerformWarp...
[WARP] PerformWarp completed
[WARP] Waiting 0.1s before save...
[WARP] Save check - _saveManager: True
[WARP] Calling Save()...
[SAVE] Map: 1-2, Position: (x, y, z)
[WARP] Save() completed
```

**Files Modified:**
- Assets/Scripts/World/WarpPoint.cs
- Assets/Scripts/Player/PlayerController.cs
- Assets/Scripts/Player/DeathManager.cs

**Commit:** c7faa84 - debug: Add detailed logging to WarpPoint for save system debugging
