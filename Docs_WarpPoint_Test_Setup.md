# WarpPoint 테스트 설정 가이드

> **작성일**: 2026-03-17  
> **목표**: 1-1Map에서 1-2Map으로 워프포인트 이동 테스트

---

## ✅ Phase 1: GameInstaller 수정 완료

**수정된 파일**: `Assets/Scripts/Core/GameInstaller.cs`

**추가된 코드**:
```csharp
// 맵 관리자 (씬에서 찾아서 등록)
var mapManager = UnityEngine.Object.FindObjectOfType<MapManager>();
if (mapManager != null)
{
    container.RegisterInstance<IMapManager>(mapManager);
    Debug.Log("[GameInstaller] MapManager registered");
}
else
{
    Debug.LogError("[GameInstaller] MapManager not found in scene!");
}
```

---

## 🔧 Phase 2: Unity Editor 맵 설정

### Step 1: 씬 구조 확인

**현재 Hierarchy 구조** (예상):
```
SampleScene
├── RootContext
│   └── GameInstaller (✅ 이미 설정됨)
├── MapManager (빈 GameObject + MapManager.cs)
├── Player (✅ 이미 존재)
└── (기존 맵 오브젝트들...)
```

### Step 2: 맵 오브젝트 정리

**목표 구조**:
```
SampleScene
├── RootContext
│   └── GameInstaller
├── MapManager
├── Player
└── Maps (새로 생성)
    ├── 1-1Map (기존 맵 오브젝트를 이 그룹으로 이동)
    │   ├── Tilemap (타일맵들)
    │   ├── Ground (충돌용 타일맵)
    │   ├── SpawnPoint_1-1 (새로 생성)
    │   └── WarpPoint_1-1 (새로 생성)
    └── 1-2Map (기존 맵 오브젝트를 이 그룹으로 이동)
        ├── Tilemap
        ├── Ground
        ├── SpawnPoint_1-2 (새로 생성)
        └── WarpPoint_1-2 (새로 생성)
```

### Step 3: Maps 부모 오브젝트 생성

```
Hierarchy → 우클릭 → Create Empty
이름: "Maps"
Position: (0, 0, 0)
```

### Step 4: 기존 맵을 1-1Map, 1-2Map으로 정리

**옵션 A: 기존 타일맵을 그룹화**
1. 기존 타일맵들을 선택
2. Maps/1-1Map으로 그룹화
3. 나머지 타일맵들은 Maps/1-2Map으로 그룹화

**옵션 B: 처음부터 다시 정리**
1. Maps → 1-1Map 생성 (빈 GameObject)
2. 기존 타일맵을 1-1Map의 자식으로 이동
3. Maps → 1-2Map 생성 (빈 GameObject)
4. 다른 타일맵을 1-2Map의 자식으로 이동

### Step 5: SpawnPoint 생성

**1-1Map의 SpawnPoint**:
```
1-1Map 선택 → 우클릭 → Create Empty
이름: "SpawnPoint_1-1"
Position: 플레이어가 시작할 위치 (예: (2, 1, 0))
```

**1-2Map의 SpawnPoint**:
```
1-2Map 선택 → 우클릭 → Create Empty
이름: "SpawnPoint_1-2"
Position: 플레이어가 도착할 위치 (예: (2, 1, 0))
```

### Step 6: WarpPoint 오브젝트 생성

**1-1Map의 WarpPoint**:
```
1-1Map 선택 → 우클릭 → Create Empty
이름: "WarpPoint_1-1"
Position: 워프할 위치 (예: (10, 1, 0))

Add Component:
- WarpPoint (검색해서 추가)
- Box Collider 2D (자동 추가됨, Is Trigger = true)
- Sprite Renderer (선택사항, 시각화용)
```

**1-2Map의 WarpPoint**:
```
1-2Map 선택 → 우클릭 → Create Empty
이름: "WarpPoint_1-2"
Position: 도착 지점 근처 (예: (2, 3, 0))

Add Component:
- WarpPoint
- Box Collider 2D (Is Trigger = true)
```

### Step 7: MapManager Inspector 설정

**MapManager 오브젝트 선택 → Inspector**:

| 필드 | 값 |
|------|-----|
| **Maps** | Size: 2 |
| **Element 0** | |
| Map Id | `1-1Map` |
| Map Root | `1-1Map` 게임오브젝트 드래그 |
| Display Name | `1-1 Map` |
| Spawn Point | `SpawnPoint_1-1` 드래그 |
| **Element 1** | |
| Map Id | `1-2Map` |
| Map Root | `1-2Map` 게임오브젝트 드래그 |
| Display Name | `1-2 Map` |
| Spawn Point | `SpawnPoint_1-2` 드래그 |
| **Starting Map Index** | `0` |

### Step 8: WarpPoint Inspector 설정

**WarpPoint_1-1 설정**:

| 필드 | 값 |
|------|-----|
| Warp Point Id | `Warp_1-1_to_1-2` |
| Target Map Id | `1-2Map` |
| Target Warp Point Id | `` (빈칸) |
| Start Activated | `false` |
| Interaction Radius | `2` |

**WarpPoint_1-2 설정**:

| 필드 | 값 |
|------|-----|
| Warp Point Id | `Warp_1-2_Entrance` |
| Target Map Id | `` (빈칸 - 체크포인트만) |
| Target Warp Point Id | `` |
| Start Activated | `true` |
| Interaction Radius | `2` |

---

## 🎮 Phase 3: 테스트 단계

### Step 1: 초기화 테스트

**Play 모드 진입 → Console 로그 확인**:
```
[DIContainerManager] Global container initialized
[DIContainerManager] Scene container created for: SampleScene
[RootContext] Executed installer: GameInstaller
[GameInstaller] MapManager registered
[GameInstaller] Services registered
[RootContext] Injected X MonoBehaviours in RootContext
```

**❌ 에러가 나면**:
- `[GameInstaller] MapManager not found in scene!` → MapManager 오브젝트 확인
- 컴파일 에러 → 코드 수정 사항 확인

### Step 2: 시작 맵 확인

**Play 모드에서 확인**:
1. 1-1Map만 활성화되어 있어야 함 (Inspector에서 체크)
2. 1-2Map은 비활성화되어 있어야 함
3. 플레이어가 1-1Map의 SpawnPoint 위치에 있어야 함

### Step 3: 워프포인트 상호작용

**테스트 방법**:
1. 플레이어를 WarpPoint_1-1 위치로 이동
2. 상호작용 반경(노란색 원)에 들어가면 테두리 표시
3. E키 입력
4. 활성화되면 체크포인트 저장 (로그 확인)

**예상 로그**:
```
[DeathManager] Checkpoint set: (10.0, 1.0, 0.0)
```

### Step 4: 맵 전환 확인

**E키로 워프포인트 활성화 후**:
1. 1-1Map 비활성화 확인
2. 1-2Map 활성화 확인
3. 플레이어가 1-2Map의 SpawnPoint로 이동 확인

**예상 로그**:
```
[MapManager] Switching from map 0 to map 1
[MapManager] Activated map: 1-2Map
```

### Step 5: 체크포인트 리스폰 테스트

**테스트 방법**:
1. 1-2Map의 WarpPoint 활성화 (E키)
2. 플레이어를 다른 위치로 이동
3. 플레이어 사망 (Trap 또는 Enemy와 충돌)
4. 마지막 활성화한 워프포인트 위치로 리스폰 확인

**예상 로그**:
```
[DeathManager] Player died. Death count: 1
[DeathManager] Respawned at (2.0, 3.0, 0.0)
```

---

## 🐛 문제 해결

### 문제 1: MapManager not found

**원인**: MapManager 오브젝트가 씬에 없음

**해결**:
1. Hierarchy에서 MapManager 오브젝트 생성
2. MapManager.cs 스크립트 추가

### 문제 2: E키가 작동하지 않음

**원인**: Input System이 설정되지 않음

**해결**:
1. Project Settings → Player → Active Input Handling = "Input System Package (New)"
2. 또는 WarpPoint.cs의 Input Action 설정 확인

### 문제 3: 맵이 전환되지 않음

**원인**: 
- MapManager의 Maps 리스트가 비어있음
- Target Map Id가 잘못됨

**해결**:
1. MapManager Inspector에서 Maps 리스트 확인
2. Map Id가 정확한지 확인 ("1-1Map", "1-2Map")

### 문제 4: 플레이어가 이동하지 않음

**원인**: SpawnPoint가 설정되지 않음

**해결**:
1. MapManager의 Maps 각 요소에 SpawnPoint 드래그
2. SpawnPoint Transform 위치 확인

---

## 📝 체크리스트

**Unity Editor 설정**:
- [ ] Maps 부모 오브젝트 생성
- [ ] 1-1Map, 1-2Map 그룹화
- [ ] SpawnPoint_1-1, SpawnPoint_1-2 생성
- [ ] WarpPoint_1-1, WarpPoint_1-2 생성
- [ ] MapManager에 Maps 데이터 등록
- [ ] WarpPoint Inspector 설정

**테스트 확인**:
- [ ] DI 주입 로그 확인
- [ ] 시작 맵 자동 활성화
- [ ] E키 상호작용 작동
- [ ] 맵 전환 작동
- [ ] 체크포인트 저장

---

## 💡 추가 팁

**스프라이트 없이 테스트**:
- WarpPoint는 스프라이트 없이도 작동함
- Scene 뷰에서 Gizmos로 위치 확인 가능
- 노란색 원 = 상호작용 반경
- 녹색 원 = 활성화됨

**간단한 테스트**:
1. 처음엔 스프라이트 없이 오브젝트만으로 테스트
2. 기능이 작동하면 스프라이트 추가
3. 최종적으로 이펙트와 사운드 추가

**디버깅**:
- Console 로그를 항상 확인
- `[MapManager]`, `[DeathManager]`, `[GameInstaller]` 로그 필터
- Scene 뷰에서 Gizmos 표시 활성화