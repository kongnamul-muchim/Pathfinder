# Development Log

> Pathfinder 게임 개발 히스토리

---

## 2026-03-19: Parallax Background System (Material Offset)

### Material Offset 기반 패럴랙스 배경
- 기존 Transform 이동 방식 → Material Texture Offset 방식으로 변경
- 단일 오브젝트로 무한 스크롤 구현 (오브젝트 복제 불필요)
- Draw Call 최적화

### ParallaxLayer 변경사항
- `_material.mainTextureOffset` 조정으로 배경 이동
- `_tiling`: 텍스처 반복 설정 (Vector2)
- Player 위치 기반 Offset 계산: `playerX * (1 - speed) / textureWidth`

### Inspector 설정
- `Player`: 플레이어 Transform 할당 (필수)
- `Parallax Speed`: 0~1 (0=고정, 1=따라감)
- `Tiling`: 텍스처 반복 (기본 1,1)
- `Order In Layer`: 렌더링 순서 (낮을수록 뒤)
- `Sorting Layer Name`: "Background"

### Unity 설정
```
Backgrounds
├── Layer_0 (SpriteRenderer + ParallaxLayer)
│   ├── Parallax Speed: 0.1
│   └── Order In Layer: -100
├── Layer_1 (SpriteRenderer + ParallaxLayer)
│   ├── Parallax Speed: 0.3
│   └── Order In Layer: -50
...
```

### 주의사항
- Texture Wrap Mode: `Repeat` 설정 필수
- `renderer.material` 사용 시 자동으로 인스턴스 생성

### Commit: 088b995

---

## 2026-03-18: Dash Collider System

### 대쉬 시 Collider 축소 기능
- 대쉬 시 BoxCollider2D 높이 감소로 낮은 틈새 통과 가능
- 대쉬 종료 시 안전한 복원 처리 (충돌 시 X축 빈 공간 탐색)

### Inspector 설정
- `_dashColliderHeightReduction`: 높이 감소량 (기본 1)
- `_safePositionSearchDistance`: 빈 공간 최대 탐색 거리 (기본 3)
- `_safePositionSearchStep`: 탐색 간격 (기본 0.5)

### 메서드
- `SetDashCollider(bool)`: Collider 축소/복원
- `TryRestoreCollider()`: 안전한 복원 시도
- `FindSafePositionX()`: X축 빈 공간 탐색
- `SearchInDirection(float)`: 지정 방향 탐색

### Commit: 7254fa0

---

## 2026-03-18: GameOver System & Save-based Rollback

### 목숨 저장 버그 수정 (2차)
- **문제**: 저장 없이 죽을 때 `ResetAllProgress()` → `ResetAbilities()` → 목숨 3으로 리셋 → `ConsumeLife()` → 목숨 2
- **원인**: `ResetAbilities()`가 능력과 목숨을 함께 리셋
- **해결**: `ResetAbilitiesOnly()` 메서드 추가 (능력만 리셋, 목숨 유지)
- **SOLID 준수**: SRP (AbilityManager가 목숨 관리), OCP (기존 코드 수정 없이 새 메서드 추가)

### 파일 수정
- `IAbilityManager.cs`: `ResetAbilitiesOnly()` 추가
- `AbilityManager.cs`: `ResetAbilitiesOnly()` 구현
- `SaveManager.cs`: `ResetAllProgress()`에서 `ResetAbilitiesOnly()` 호출

### Commit: a2457b0

---

## 2026-03-18: GameOver System & Save-based Rollback

### 목숨 저장 버그 수정
- `GameSaveData.cs`: `extraLives` → `lives`로 변경 (현재 목숨 직접 저장)
- `ISaveManager.cs`: `UpdateSavedLives(int lives)` 메서드 추가
- `SaveManager.cs`:
  - `Save()`: `_abilityManager.GetLives()` 저장
  - `ApplySaveData()`: `SetLives(data.lives)` 복원
  - `UpdateSavedLives()`: 롤백 후 목숨만 업데이트
- `DeathManager.cs`: `RollbackWithLife()`에서 `UpdateSavedLives()` 호출

### 문제 원인
- 롤백 시 저장 파일에서 목숨 복원 → ConsumeLife() → 저장 파일 미갱신
- 다시 죽을 때 예전 목숨 수로 복원 → 무한 목숨

### 해결
- `lives` 필드로 현재 목숨 직접 저장
- 롤백 후 `UpdateSavedLives()`로 저장 파일 갱신

### Commit: fdfd886

---

## 2026-03-18: GameOver System & Save-based Rollback

### GameOver System
- `GameOverUI.cs` 생성: GameOver 화면, 재시작 기능
- DeathManager에 GameOver 분기 로직 추가
- 4가지 사망 시나리오 처리

### Death Scenarios
| 저장 | 목숨 | 동작 |
|:----:|:----:|------|
| O | O | Load 롤백 + 목숨 -1 |
| O | X | GameOver |
| X | O | 진행상황 리셋 + SpawnPoint |
| X | X | GameOver |

### SaveManager Changes
- `ResetAllProgress()` 추가: 능력/상자/저장파일 전체 리셋
- 상자 상태 저장/로드 로그 추가

### AbilityManager Changes
- 초기 능력 비활성화 (테스트용)
- 능력 해금/목숨 관련 로그 추가

### Commit: 019ffd3

---

## 2026-03-18: Save System 개선

### MovingPlatform Update
- 플레이어 따라가기 로직 제거
- `Assets/Scripts/Traps/MovingPlatform.cs`
- Commit: 9461b5f

### Save System Improvements
- `SaveManager.cs`: OnApplicationQuit() 자동 저장, LoadedMapId 추가
- `MapManager.cs`: 저장 데이터 확인 후 로드 또는 초기 스폰
- `WarpPoint.cs`: 도착지 저장
- `DeathManager.cs`: 저장된 맵으로 전환 후 리스폰
- Commit: 13572dd

### Warp Save Location Fix
- 출발지 저장 제거, 도착지에서만 저장
- SaveManager: UNITY_EDITOR에서 파일 삭제 (테스트용)
- PlayerController: 벽 감지 수정
- Commit: 48394d0

### Death Respawn Fix
- RespawnFromSave() 메서드 추가
- Load() 후 물리 리셋, 무적 상태 시작
- Commit: fc74b46

### Save Map ID Fix
- GetSavedMapId() 메서드 추가
- LoadedMapId → GetSavedMapId() 변경
- Commit: 3382184

### Save System Logging
- Save/Load 위치 로그 추가
- Commit: af8adba

### Debug Logging for Warp & Death
- WarpPoint, PlayerController, DeathManager 로그 추가
- Commit: c7faa84

---

## 2026-03-18: Refactoring

### Debug Log Cleanup
- MovingPlatform.cs, RootContext.cs 디버그 로그 제거
- Commit: 8304c11

---

## 2026-03-17: Debug Log Cleanup

다음 파일에서 Debug.Log 제거:
- WarpPoint.cs, MapManager.cs, SaveManager.cs
- PlayerController.cs, AbilityManager.cs, AbilityChest.cs
- AbilityUnlockable.cs, CameraController.cs, Portal.cs
- Checkpoint.cs, RewardPopupUI.cs, GameInstaller.cs
- RootContext.cs, DIContainerManager.cs

Commit: e83cad9

---

## 2026-03-17: Interaction System

### 상호작용 프롬프트 시스템
- IInteractable 인터페이스 추가
- InteractionPromptUI 컴포넌트 생성
- WarpTarget 필드 추가 (정확한 도착 위치)
- Commit: 4eac5cf

---

## 2026-03-17: Ability Chest System

### 능력 상자 시스템 구현
- AbilityChest.cs, 목숨 시스템
- Q키 상호작용, RewardType enum
- Commit: (기록됨)

### PerspectiveShift 제거
- Hidden 시스템 제거 (Tilemap 호환성 문제)
- 능력 2개로 단순화 (DoubleJump, Dash)
- Commit: (기록됨)

### Ability System 구현
- AbilityManager, DoubleJump, Dash
- DI 연동
- Commit: (기록됨)

---

## 2026-03-16: Player & DI

### PlayerController with DI
- PlayerController, IAbilityManager, IDeathManager
- GameInstaller 추가
- Commit: (기록됨)

### DI Container System
- DI Container 구현
- Installer/RootContext 추가
- AGENT.md 생성
- Commit: (기록됨)

---

## 2026-03-12: Project Start

### Initial Setup
- 폴더 구조 생성
- TaskList.md 추가
- Commit: Initial commit