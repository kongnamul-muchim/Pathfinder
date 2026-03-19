# WarpPoint 시스템 가이드

> **작성일**: 2026-03-17  
> **Git Commit**: `8c46201`

---

## 업데이트 이력

### 2026-03-17 - 상호작용 프롬프트 시스템 구현
- **Git Commit**: `4eac5cf`
- **변경사항**: 
  - IInteractable 인터페이스 추가 (`Assets/Scripts/Interfaces/IInteractable.cs`)
  - InteractionPromptUI 컴포넌트 생성 (`Assets/Scripts/UI/InteractionPromptUI.cs`)
  - WarpPoint에서 아웃라인 시스템 제거 → UI 프롬프트로 대체
  - PlayerController에 상호작용 감지 및 E키 입력 처리 추가
  - WarpTarget 필드 추가로 타일맵 워프 포인트의 정확한 도착 위치 지정 가능
- **목적**: 타일맵 기반 워프 포인트에 정확한 위치 지정 + 직관적인 시각적 상호작용 표시

---

## 1. 개요

WarpPoint는 **Checkpoint**와 **Portal** 기능을 통합한 시스템입니다.

**플레이어가 워프 포인트 범위 내에서 E키를 누르면:**
1. 플레이어 머리 위에 "Press E" 프롬프트 UI 표시
2. E키 입력 시 체크포인트 저장 + 목표 맵으로 순간이동

---

## 2. 구현 상태

### 완료된 작업

| 항목 | 상태 |
|------|------|
| WarpPoint.cs | ✅ 구현 완료 |
| IInteractable 인터페이스 | ✅ 구현 완료 |
| InteractionPromptUI.cs | ✅ 구현 완료 |
| PlayerController 상호작용 | ✅ 구현 완료 |
| Portal.cs | ✅ 구현 완료 |
| MapManager.cs | ✅ 구현 완료 |
| IMapManager 인터페이스 | ✅ 정의 완료 |
| DI Container 등록 | ✅ GameInstaller에 등록 |

### DI Container 설정

- ✅ RootContext 오브젝트 존재
- ✅ GameInstaller가 RootContext의 Installers에 등록됨
- ✅ MapManager 오브젝트가 씬에 존재
- ✅ IAbilityManager, IDeathManager, IMapManager 등록됨

---

## 3. Inspector 설정

### WarpPoint 컴포넌트

| 필드 | 설명 | 예시 |
|------|------|------|
| `Warp Point Id` | 고유 ID | "Village_Center" |
| `Target Map Id` | 목표 맵 ID | "Meadow" |
| `Target Warp Point Id` | 목표 워프 포인트 ID | "Meadow_Entrance" |
| **Warp Target** | **도착 위치 Transform** | WarpTarget (빈 오브젝트) |
| `Start Activated` | 시작 시 활성화 여부 | true (시작 지점용) |
| `Activated Sprite` | 활성화 시 스프라이트 | 초록색 타일 |
| `Deactivated Sprite` | 비활성화 시 스프라이트 | 회색 타일 |
| `Interaction Text` | 상호작용 프롬프트 텍스트 | "Press E" |
| `Interaction Radius` | 상호작용 범위 | 2.0 |

### MapManager 설정

| 필드 | 값 |
|------|-----|
| **Maps** | Size: 2 |
| **Element 0** | MapId="1-1Map", MapRoot=1-1Map, SpawnPoint=SpawnPoint_1-1 |
| **Element 1** | MapId="1-2Map", MapRoot=1-2Map, SpawnPoint=SpawnPoint_1-2 |
| **Starting Map Index** | 0 |

### PlayerController 설정

| 필드 | 값 |
|------|-----|
| **Interaction Prompt Prefab** | InteractionPrompt 프리팹 드래그 |
| **Interaction Radius** | 2.0 (WarpPoint와 동일 권장) |

---

## 4. 설정 가이드

### Step 1: 맵 오브젝트 구조

```
SampleScene/
├── RootContext
│   └── GameInstaller
├── MapManager
├── Player (PlayerController에 InteractionPrompt Prefab 연결)
└── Maps/
    ├── 1-1Map (활성화)
    │   ├── Tilemap
    │   ├── SpawnPoint_1-1
    │   └── WarpPoint_1-1
    │       └── WarpTarget (빈 오브젝트 - 정확한 도착 위치)
    └── 1-2Map (비활성화)
        ├── Tilemap
        ├── SpawnPoint_1-2
        └── WarpPoint_1-2
```

### Step 2: InteractionPrompt 프리팹 생성

```
1. Hierarchy → Create → UI → Text - TextMeshPro
2. 이름: "InteractionPrompt"
3. Canvas 설정:
   - Render Mode: World Space
   - Sorting Layer: UI
4. TextMeshPro 설정:
   - Text: "Press E"
   - Color: White
   - Outline Color: Black
   - Outline Thickness: 0.2
   - Font Size: 36
5. 프리팹으로 저장 (Assets/Prefabs/UI/)
6. 컴포넌트 추가: InteractionPromptUI 스크립트
```

### Step 3: WarpPoint 오브젝트 생성 (타일맵 + 빈 오브젝트 구조)

```
1. 타일맵 워프 포인트 배치
   - 타일맵에서 워프 포인트 위치에 타일 배치
   - 빈 오브젝트 생성: "WarpPoint_1-1"
   - Add Component → WarpPoint
   - Sprite Renderer에 타일맵 스프라이트 연결

2. 정확한 도착 위치 설정
   - WarpPoint_1-1 우클릭 → Create Empty
   - 이름: "WarpTarget"
   - Position: 원하는 정확한 도착 위치로 이동
   - WarpPoint Inspector의 Warp Target 필드에 연결

3. Collider 설정 (자동)
   - Circle Collider 2D 자동 추가 (Is Trigger = true)
   - Interaction Radius 조정 (기본값: 2)
```

### Step 4: WarpPoint 설정 예시

**WarpPoint_1-1 (마을 → 초원):**
| 필드 | 값 |
|------|-----|
| Warp Point Id | `Warp_1-1_to_1-2` |
| Target Map Id | `1-2Map` |
| Target Warp Point Id | `Warp_1-2_Entrance` |
| Warp Target | `WarpTarget` (빈 오브젝트) |
| Start Activated | `false` |
| Interaction Text | `Press E` |
| Interaction Radius | `2` |

**WarpPoint_1-2 (체크포인트만):**
| 필드 | 값 |
|------|-----|
| Warp Point Id | `Warp_1-2_Entrance` |
| Target Map Id | (빈칸 - 체크포인트만) |
| Warp Target | (빈칸 - 현재 위치 사용) |
| Start Activated | `true` |

---

## 5. 테스트 방법

### 테스트 체크리스트

**설정 단계:**
- [ ] Maps 부모 오브젝트 생성
- [ ] 1-1Map, 1-2Map 그룹화
- [ ] SpawnPoint 오브젝트 생성
- [ ] WarpPoint 오브젝트 생성 (타일맵 스프라이트 포함)
- [ ] WarpTarget 빈 오브젝트 생성 및 위치 설정
- [ ] WarpPoint에 Warp Target 연결
- [ ] MapManager에 Maps 데이터 등록
- [ ] PlayerController에 InteractionPrompt Prefab 연결
- [ ] WarpPoint Inspector 설정

**실행 단계:**
- [ ] Play 모드 진입
- [ ] Console 로그 확인: `[GameInstaller] MapManager registered`
- [ ] 시작 맵 자동 활성화 확인
- [ ] 플레이어를 WarpPoint 범위 내로 이동
- [ ] "Press E" 프롬프트 UI 표시 확인
- [ ] E키 입력으로 워프 활성화
- [ ] 맵 전환 및 정확한 위치로 이동 확인

### 예상 로그

```
[GameInstaller] MapManager registered
[PlayerController] Showing interaction prompt
[WarpPoint] Activated: Warp_1-1_to_1-2
[MapManager] Switching from map 0 to map 1
[DeathManager] Checkpoint set: (10.0, 1.0, 0.0)
```

---

## 6. 문제 해결

| 문제 | 원인 | 해결 |
|------|------|------|
| "Press E" UI가 표시되지 않음 | InteractionPrompt Prefab 미연결 | PlayerController에 프리팹 드래그 |
| UI 위치가 이상함 | World Space Canvas 설정 오류 | Canvas Render Mode를 World Space로 설정 |
| 워프 후 위치가 부정확함 | Warp Target 미설정 | WarpPoint에 Warp Target 빈 오브젝트 연결 |
| E키가 작동하지 않음 | Input System 미설정 | Project Settings → Active Input Handling = "Input System Package" |
| 맵이 전환되지 않음 | Maps 리스트 비어있음 또는 Map Id 불일치 | MapManager Inspector 확인 |
| 플레이어가 이동하지 않음 | SpawnPoint 미설정 | MapManager에 SpawnPoint 드래그 |

---

## 7. 상호작용 시스템 아키텍처

### 클래스 다이어그램

```
IInteractable (Interface)
    ├── GetInteractionText(): string
    ├── CanInteract(): bool
    ├── OnInteract(): void
    └── GetPromptTransform(): Transform

WarpPoint : MonoBehaviour, IInteractable, ICheckpoint
    └── IInteractable 구현

PlayerController : MonoBehaviour
    ├── _interactionPromptPrefab: InteractionPromptUI
    ├── _interactionRadius: float
    ├── CheckForInteractables(): void
    └── ShowInteractionPrompt(IInteractable): void

InteractionPromptUI : MonoBehaviour
    ├── _promptText: TextMeshProUGUI
    ├── Show(Transform, string): void
    └── Hide(): void
```

### 동작 흐름

```
1. PlayerController.Update()
   └── CheckForInteractables()
       └── OverlapCircleAll() → IInteractable 검색
           └── CanInteract() 확인
               └── 가장 가까운 오브젝트 선택
                   └── ShowInteractionPrompt()
                       └── InteractionPromptUI.Show()

2. Player가 E키 입력
   └── PlayerController.Update()
       └── _currentInteractable.OnInteract()
           └── WarpPoint.WarpSequence() 실행
```

---

## 8. 관련 파일

### 스크립트
- `Assets/Scripts/World/WarpPoint.cs`
- `Assets/Scripts/World/Portal.cs`
- `Assets/Scripts/World/MapManager.cs`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/UI/InteractionPromptUI.cs`
- `Assets/Scripts/Core/GameInstaller.cs`

### 인터페이스
- `Assets/Scripts/Interfaces/IInteractable.cs` (신규)
- `Assets/Scripts/Player/IDeathManager.cs`
- `Assets/Scripts/World/IMapManager.cs`

### 프리팹
- `Assets/Prefabs/UI/InteractionPrompt.prefab` (생성 필요)

---

## 9. 향후 개선사항

- [x] ✅ UI 프롬프트 시스템 연동 (완료)
- [x] ✅ 타일맵 워프 포인트 정확한 위치 지정 (완료)
- [ ] ScreenFade와 연동
- [ ] 워프 중 이동 불가 처리
- [ ] 사운드 이펙트 추가
- [ ] 여러 상호작용 오브젝트 우선순위 처리 (현재는 가장 가까운 것만)
