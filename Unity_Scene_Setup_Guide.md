# Unity 씬 설정 가이드

## 개요
Pathfinder 게임의 Unity 씬 구성 방법을 안내합니다.

---

## 1. 기본 씬 구조

### Hierarchy 구조
```
Scene (MainScene)
├── [Managers]                    # 관리자 오브젝트들
│   ├── RootContext              # DI Container 설정
│   ├── MapManager               # 맵 관리
│   └── CameraController         # 카메라
│
├── [Maps]                        # 5개의 맵 (비활성화 상태)
│   ├── Map_01_Tutorial          # Map 1: 튜토리얼
│   ├── Map_02_DoubleJump        # Map 2: DoubleJump 구체
│   ├── Map_03_Perspective       # Map 3: PerspectiveShift 구체
│   ├── Map_04_Challenge         # Map 4: 고난이도
│   └── Map_05_Boss              # Map 5: 보스맵
│
└── [Global]                      # 전역 오브젝트
    └── Player                   # 플레이어 (씬 전환 시 유지)
```

---

## 2. 단계별 설정

### Step 1: RootContext 설정

1. **빈 오브젝트 생성**: `RootContext`
2. **컴포넌트 추가**: `RootContext.cs`
3. **GameInstaller 연결**:
   - Inspector에서 `GameInstaller` 필드에 `GameInstaller` 객체 할당

### Step 2: MapManager 설정

1. **빈 오브젝트 생성**: `MapManager`
2. **컴포넌트 추가**: `MapManager.cs`
3. **맵 등록**:
   ```
   Maps:
   - Element 0:
     MapId: "Map_01"
     MapRoot: [Map_01_Tutorial GameObject 드래그]
     DisplayName: "튜토리얼"
     SpawnPoint: [SpawnPoint transform 드래그]
   - Element 1:
     MapId: "Map_02"
     MapRoot: [Map_02_DoubleJump GameObject 드래그]
     DisplayName: "더블점프의 땅"
     SpawnPoint: [SpawnPoint transform 드래그]
   ... (총 5개)
   ```

### Step 3: 맵 생성 (각각)

각 맵은 **빈 오브젝트**로 생성하고, 모든 맵 요소는 해당 오브젝트의 자식으로 배치합니다.

#### Map 1: Tutorial
```
Map_01_Tutorial (비활성화)
├── Terrain                      # 타일맵/플랫폼
├── SpawnPoint                   # 플레이어 시작 위치
├── Checkpoint_01                # 세이브포인트
├── Portal_To_Map02              # 다음 맵 포탈
└── Decorations                  # 장식용 오브젝트
```

**특징**: 기본 이동/점프만 필요

#### Map 2: DoubleJump
```
Map_02_DoubleJump (비활성화)
├── Terrain
│   └── High_Platforms           # 높은 플랫폼들 (DoubleJump 필요)
├── SpawnPoint
├── Checkpoint_02
├── Portal_To_Map01
├── Portal_To_Map03
└── AbilityOrb_DoubleJump        # DoubleJump 구체
```

**특징**: 
- 높은 플랫폼 배치 (DoubleJump 없이는 도달 불가)
- DoubleJump 구체 배치

#### Map 3: PerspectiveShift
```
Map_03_Perspective (비활성화)
├── Terrain
│   └── Hidden_Path             # 숨겨진 길 (PerspectiveShift 필요)
├── SpawnPoint
├── Checkpoint_03
├── Portal_To_Map01
├── Portal_To_Map04
├── AbilityOrb_Perspective      # PerspectiveShift 구체
└── Hidden_Platforms            # 숨겨진 발판들
```

**특징**:
- 숨겨진 발판/길 배치
- PerspectiveShift 구체 배치

#### Map 4: Challenge
```
Map_04_Challenge (비활성화)
├── Terrain
│   ├── High_Platforms          # DoubleJump 필요
│   └── Hidden_Shortcut         # PerspectiveShift 필요
├── SpawnPoint
├── Checkpoint_04
├── Portal_To_Map01
└── Portal_To_Boss              # 보스맵 진입
```

**특징**:
- 모든 능력이 필요한 고난이도 구역

#### Map 5: Boss
```
Map_05_Boss (비활성화)
├── Terrain
├── SpawnPoint
└── Boss_Area                   # 보스전 투기장
```

**특징**:
- Checkpoint 없음
- Boss 전투 구역

### Step 4: Player 설정

1. **2D Object → Sprite** 생성
2. **이름 변경**: `Player`
3. **컴포넌트 추가**:
   - `Rigidbody2D`:
     - Gravity Scale: 3
     - Collision Detection: Continuous
   - `BoxCollider2D` (또는 CircleCollider2D)
   - `PlayerController.cs`
   - `PlayerAnimator.cs`
   - `Animator`:
     - Controller: `PlayerAnimatorController` 할당
   - `Sprite Renderer`:
     - Sprite: Penguin 스프라이트 선택

4. **Player 태그 설정**: `Tag: Player`

### Step 5: Checkpoint 설정

1. **2D Object → Sprite** 생성
2. **이름 변경**: `Checkpoint_01`
3. **스프라이트**: 깃발 또는 체크포인트 이미지
4. **컴포넌트 추가**: `Checkpoint.cs`
5. **Inspector 설정**:
   - Checkpoint ID: "Checkpoint_01"
   - Start Activated: 첫 체크포인트만 true

### Step 6: Portal 설정

1. **2D Object → Sprite** 생성
2. **이름 변경**: `Portal_To_Map02`
3. **스프라이트**: 포탈 이미지
4. **Collider 설정**: `BoxCollider2D` → Is Trigger ✓
5. **컴포넌트 추가**: `Portal.cs`
6. **Inspector 설정**:
   - Portal ID: "Portal_Map01_To_Map02"
   - Target Map ID: "Map_02"
   - Target Portal ID: "Portal_Map02_From_Map01"

### Step 7: AbilityOrb 설정

1. **2D Object → Sprite** 생성
2. **이름 변경**: `AbilityOrb_DoubleJump`
3. **컴포넌트 추가**: `AbilityUnlockable.cs`
4. **Inspector 설정**:
   - Ability Type: DoubleJump
   - Destroy On Collect: true

### Step 8: Camera 설정

1. **Main Camera** 선택
2. **컴포넌트 추가**: `CameraController.cs`
3. **Inspector 설정**:
   - Target: Player 드래그
   - Follow Speed: 5
   - Offset: (0, 2, -10)
   - Use Bounds: true
   - Min Bounds: (-20, -10)
   - Max Bounds: (20, 15)

---

## 3. 능력 기반 맵 디자인

### DoubleJump가 필요한 곳
- **높은 플랫폼**: 일반 점프로는 도달 불가
- **예시 배치**:
  ```
  Ground        High Platform
  [=====]      [=========]
       ↑
       ← DoubleJump 필요
  ```

### PerspectiveShift가 필요한 곳
- **숨겨진 발판**: 투명한 발판들
- **숨겨진 길**: 벽 뒤에 있는 통로
- **예시 배치**:
  ```
  [Wall]    (Hidden Platform)
     ↑
     ← PerspectiveShift 필요
  ```

---

## 4. 테스트 체크리스트

### 기본 이동
- [ ] 좌우 이동 (A/D)
- [ ] 점프 (Space)
- [ ] 애니메이션 재생 (Walk, Jump, Idle)

### 맵 전환
- [ ] Portal 접촉 시 맵 전환
- [ ] 플레이어 위치 변경
- [ ] 카메라 위치 변경

### Checkpoint
- [ ] Checkpoint 활성화 (색상 변경)
- [ ] 사망 시 Checkpoint에서 리스폰
- [ ] DeathManager에 위치 저장

### 능력 획득
- [ ] AbilityOrb 획득
- [ ] AbilityManager에 해금 확인
- [ ] 능력 획득 후 숨겨진 구역 접근 가능

---

## 5. 주의사항

1. **맵은 항상 비활성화 상태로 시작**
   - MapManager가 시작 맵만 활성화

2. **플레이어는 Global로 유지**
   - 씬 전환 시 파괴되지 않음

3. **Checkpoint는 처음에 비활성화**
   - 첫 Checkpoint만 Start Activated = true

4. **Portal은 쌍으로 생성**
   - Map01→Map02와 Map02→Map01 모두 필요

5. **AbilityOrb는 획득 시 사라짐**
   - Destroy On Collect = true 권장

---

## 6. 참고: 능력별 색상

- **DoubleJump**: 노란색/금색
- **PerspectiveShift**: 하늘색/파란색

---

*마지막 업데이트: 2026-03-16*
