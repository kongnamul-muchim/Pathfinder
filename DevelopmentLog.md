# Development Log

> Pathfinder 게임 개발 히스토리

---

## 2026-03-19: Player Movement Improvements

### 변경사항

#### 1. 무적 시스템 삭제 (DeathManager.cs)
- `_respawnInvincibilityTime` 필드 삭제
- `_isInvincible` 변수 삭제
- `InvincibilityCoroutine()` 메서드 삭제
- Kinematic 설정 로직 삭제
- 리스폰 시 물리 속도만 리셋

#### 2. 벽 충돌 로직 삭제 (PlayerController.cs)
- `_wallCheckDistance`, `_wallSlideSpeed`, `_wallLayer`, `_excludeWallTags` 필드 삭제
- `_isTouchingWall`, `_wallDirection` 변수 삭제
- `CheckWallCollision()` 메서드 삭제
- `IsExcludedFromWall()` 메서드 삭제

#### 3. 벽 달라붙음 현상 수정
```csharp
// FixedUpdate 말미에 추가
if (!_isDashing && Mathf.Abs(_horizontalInput) > 0.1f && Mathf.Abs(_rb.linearVelocity.x) < 0.1f)
{
    velocity = _rb.linearVelocity;
    velocity.x = _horizontalInput * 0.5f;  // 작은 속도로 미끄러짐
    _rb.linearVelocity = velocity;
}
```

### 효과
- 무적 관련 버그 제거
- 벽에 달라붙지 않고 자연스럽게 미끄러짐
- 코드 간소화

### Commit: 9e08520

---

## 2026-03-19: Parallax Shader Graph Support

### 문제 해결
- **Shader Graph 미작동**: `SetTextureOffset`은 `_MainTex_ST`만 조절
- **해결**: Shader Graph의 `_Offset` Vector2 프로퍼티를 `SetVector`로 직접 조절

### ParallaxLayer.cs 변경사항
```csharp
[Header("Shader")]
[SerializeField] private string _offsetProperty = "_Offset";
[SerializeField] private bool _useShaderGraph = true;

// LateUpdate
if (_useShaderGraph)
{
    _material.SetVector(_offsetPropertyId, new Vector2(offsetX, 0));
}
else
{
    _material.SetTextureOffset(_offsetPropertyId, new Vector2(offsetX, 0));
}
```

### Inspector 설정
| 필드 | 기본값 | 설명 |
|------|--------|------|
| Offset Property | `_Offset` | Shader Graph용 프로퍼티 이름 |
| Use Shader Graph | `true` | Shader Graph 사용 여부 |

### 셰이더별 설정
| 셰이더 | Use Shader Graph | Offset Property |
|--------|------------------|-----------------|
| Parallax.shadergraph | `true` | `_Offset` |
| Sprites/Default | `false` | `_MainTex` |
| URP/Unlit | `false` | `_BaseMap` |

### Commit: c9adbad

---

## 2026-03-19: Parallax System Simplified

### 문제 해결
1. **버벅거림**: Transform 즉시 이동 → `Lerp`로 부드러운 이동
2. **TextureWidth 불필요**: 단순화된 Offset 계산

### ParallaxLayer.cs 변경사항
```csharp
// Transform 부드러운 이동
Vector3 targetPosition = new Vector3(cameraX, _initialPosition.y, _initialPosition.z);
transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);

// 단순화된 Offset 계산
float offsetX = cameraX * _parallaxSpeed * _offsetMultiplier;
_material.SetTextureOffset(_texturePropertyId, new Vector2(offsetX, 0));
```

### Inspector 설정
| 필드 | 기본값 | 설명 |
|------|--------|------|
| Parallax Speed | 0.5 | 0~1: 낮을수록 풍경 변화 느림 |
| Offset Multiplier | 1.0 | Offset 배수 |
| Smooth Speed | 5.0 | 부드러운 이동 속도 |
| Texture Property | `_MainTex` | 텍스처 프로퍼티 (셰이더에 따라 다름) |

### Speed 설정 예시
| 배경 | Speed | 효과 |
|------|-------|------|
| Layer_0 (가장 먼) | 0.1 | 가장 느린 풍경 변화 |
| Layer_2 (중간) | 0.5 | 중간 속도 |
| Layer_4 (가장 가까운) | 0.9 | 가장 빠른 풍경 변화 |

### 주의사항
- Texture Wrap Mode: `Repeat` 설정 필수
- **Texture Property**: 셰이더에 따라 다름
  - Sprites/Default: `_MainTex` (기본값)
  - URP/Lit, URP/Unlit: `_BaseMap` (필요시 Inspector에서 변경)

### Commit: 415d361

---

## 2026-03-19: Parallax Background System (Material Offset)

---

## 2026-03-19: Bug Fixes

### ParallaxLayer Material 누출 수정
- `[ExecuteInEditMode]` 상태에서 `renderer.material` 호출 시 Material 인스턴스 누출
- `Application.isPlaying` 체크로 Play 모드에서만 Material 접근

### 사망 시 바닥 밑으로 떨어지는 현상 수정
- **문제**: 무적 상태에서 Collider 비활성화 → 중력으로 떨어짐 → Collider 활성화 시 지형 뚫고 낙하
- **해결**: Collider 비활성화 대신 Rigidbody를 Kinematic으로 변경
  - 무적 시작: `rb.bodyType = Kinematic`
  - 무적 종료: `rb.bodyType = Dynamic`

### Commit: 9b447d2

---

## 2026-03-19: Parallax Background System 개선

### 최종 구현 방식
- **Transform**: 카메라를 따라감 (화면에 항상 보임)
- **Texture Offset**: Speed와 OffsetMultiplier 조합으로 풍경 스크롤

### Inspector 설정
- `Parallax Speed`: 0~1 (낮을수록 배경 풍경이 천천히 변화)
- `Offset Multiplier`: 배수 조절 (기본 1.0)

### Offset 계산
```
offsetX = cameraX × (1 - speed) × offsetMultiplier / textureWidth
```

### 설정 예시
| 배경 | Speed | OffsetMultiplier | 효과 |
|------|-------|------------------|------|
| Layer_0 (가장 먼) | 0.1 | 1.0 | 가장 느림 |
| Layer_2 (중간) | 0.5 | 1.0 | 중간 |
| Layer_4 (가장 가까운) | 0.9 | 1.0 | 가장 빠름 |

### Commit: 0991449

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