# Pathfinder 게임 개발 Task List

> **프로젝트**: Pathfinder - 메트로바니아 스타일 2D 플랫포머  
> **시작일**: 2026-03-12  
> **Unity 버전**: 2022.3 LTS (URP 2D)  
> **코딩 표준**: SOLID 원칙 + Custom DI Container  

---

## 📋 프로젝트 개요 (메트로바니아 스타일)

**게임 목표**: 4개의 맵을 자유롭게 탐색하여 능력 구체를 획득하고, 모든 능력을 모으면 보스맵 입장 → 보스 처치로 게임 클리어

**핵심 메커니즘**:
- **즉사 시스템**: 함정/적 접촉 시 즉시 사망 (HP 없음)
- **능력 해금**: 각 맵에 배치된 능력 구체 획득 시 해금
  - DoubleJump: 높은 곳 접근 가능
  - PerspectiveShift: 숨겨진 발판/레버 표시
- **완전 자유 탐색**: 능력이 없으면 못 가는 곳 존재 (Ability Gate)
- **백트래킹**: 이전 맵으로 돌아가 새로운 구역 탐색

**맵 구성**:
- 한 씬에 5개 맵 (작은 맵 4개 + 보스맵 1개 큼)
- 포탈로 연결 (검은 화면 전환)
- 각 맵에 세이브포인트 존재

---

## 📁 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/              # DI Container, GameManager, EventBus
│   ├── Player/            # PlayerController, AbilityManager
│   ├── World/             # MapManager, Portal, Checkpoint
│   ├── Abilities/         # AbilityUnlockable, AbilityGate
│   ├── Interactables/     # Lever, Door (테스트용)
│   ├── Traps/             # Spike, MovingPlatform (인터페이스 기반)
│   ├── Boss/              # Boss, BossGate
│   ├── UI/                # DeathCounter, AbilityUI
│   ├── Data/              # ScriptableObject 설정 파일들
│   └── Interfaces/        # 게임 전체 인터페이스 정의
├── Prefabs/               # 프리팹 저장
├── Sprites/               # 2D 스프라이트 (픽셀 아트)
└── Scenes/                # MainScene (모든 맵 포함)
```

---

## ✅ 작업 목록

### Phase 1: 코어 시스템 ✅
- [x] **Task 1.1**: DI Container 구현 (DI_Library.md 기반)
- [x] **Task 1.2**: EventBus 시스템 구현 (느슨한 결합)
- [x] **Task 1.3**: GameManager 구현 (게임 상태 관리)
- [x] **Task 1.4**: Installer 설정 (DI 등록)

### Phase 2: 플레이어 코어 ✅
- [x] **Task 2.1**: PlayerController (이동, 점프, 물리)
- [x] **Task 2.2**: PlayerInput (Input System - 키보드/패드)
- [x] **Task 2.3**: AbilityManager (보유 능력 관리)
- [x] **Task 2.4**: DeathManager (즉사, 리스폰)
- [x] **Task 2.5**: Player Animation System (Penguin 에셋 연동)
  - [x] **Subtask 2.5.1**: PlayerAnimator 컴포넌트 생성 (Animator 상태 관리)
  - [x] **Subtask 2.5.2**: 통합 Animator Controller 설정 (Idle, Walk, Jump, Death)
  - [x] **Subtask 2.5.3**: PlayerController에 애니메이션 트리거 연동
  - [x] **Subtask 2.5.4**: 방향 전환 (좌우) 구현 (Sprite Flip)
  - [ ] **Subtask 2.5.5**: Penguin 스프라이트 Player 프리팹에 적용 (Unity에서 수동 설정)

### Phase 3: 메트로바니아 시스템 ✅
- [x] **Task 3.1**: MapManager (맵 전환 관리)
- [x] **Task 3.2**: Portal (맵 간 이동, 검은 화면 전환 - ScreenFade 연동 필요)
- [x] **Task 3.3**: Checkpoint (세이브포인트, 리스폰 위치)
- [x] **Task 3.4**: AbilityUnlockable (능력 구체)
- [x] **Task 3.5**: AbilityGate 삭제 - 능력은 플랫포밍 난이도로 해결 (차단이 아닌 도달 가능성)
- [x] **Task 3.6**: CameraController (맵 전환 시 카메라)

### Phase 4: 능력 구현 ✅
- [x] **Task 4.1**: DoubleJump 능력
- [x] **Task 4.2**: Dash 능력 (공중/지상, 더블탭 입력)

### Phase 5: 함정 시스템
- [ ] **Task 5.1**: ITrap 인터페이스 정의
- [ ] **Task 5.2**: SpikeTrap (가시 함정)
- [ ] **Task 5.3**: MovingPlatform (움직이는 플랫폼)

### Phase 6: 상호작용 (테스트용)
- [ ] **Task 6.1**: IInteractable 인터페이스
- [ ] **Task 6.2**: Lever (레버 작동)
- [ ] **Task 6.3**: Door (문 열림)

### Phase 7: 보스 시스템
- [ ] **Task 7.1**: BossGate (모든 능력 필요)
- [ ] **Task 7.2**: Boss (배경 보스)
- [ ] **Task 7.3**: BossAttack 패턴
- [ ] **Task 7.4**: Boss 처치 및 게임 클리어

### Phase 8: UI 시스템
- [ ] **Task 8.1**: DeathCounter (데스 횟수)
- [ ] **Task 8.2**: AbilityUI (획득한 능력 표시)
- [ ] **Task 8.3**: ScreenFade (맵 전환 화면 페이드)
- [ ] **Task 8.4**: GameOver/Clear UI

### Phase 9: 맵 구성
- [ ] **Task 9.1**: Map 1 (튜토리얼, 세이브포인트 1개)
- [ ] **Task 9.2**: Map 2 (DoubleJump 구체, 세이브포인트 1개)
- [ ] **Task 9.3**: Map 3 (PerspectiveShift 구체, 세이브포인트 1개)
- [ ] **Task 9.4**: Map 4 (고난이도, 세이브포인트 1개)
- [ ] **Task 9.5**: Boss Map (큼, BossGate)
- [ ] **Task 9.6**: 포탈 연결 설정

### Phase 10: 스프라이트 및 리소스
- [x] **Task 10.1**: 플레이어 스프라이트 (Nine Pines Penguin 에셋 사용)
- [x] **Task 10.2**: 플레이어 애니메이션 (Idle, Walk, Jump, Death)
- [ ] **Task 10.3**: 능력 구체 스프라이트
- [ ] **Task 10.4**: 포탈/체크포인트 스프라이트
- [ ] **Task 10.5**: 함정 스프라이트
- [ ] **Task 10.6**: 보스 스프라이트
- [ ] **Task 10.7**: 타일/배경 스프라이트

### Phase 11: 테스트 및 최적화
- [ ] **Task 11.1**: 기능 테스트
- [ ] **Task 11.2**: 성능 최적화
- [ ] **Task 11.3**: 버그 수정

### Phase 12: 마무리
- [ ] **Task 12.1**: 빌드 테스트
- [ ] **Task 12.2**: 문서화
- [ ] **Task 12.3**: Git 커밋 및 정리

---

## 🎮 게임 디자인 상세

### 능력 시스템

| 능력 | 위치 | 효과 |
|------|------|------|
| DoubleJump | Map 2 | 2단 점프 가능, 높은 곳 접근 |
| Dash | Map 3 | 빠른 이동, 방향키 더블탭 |

### 맵 구성

| 맵 | 크기 | 특징 | 세이브포인트 |
|----|------|------|--------------|
| Map 1 | 작음 | 튜토리얼, 기본 이동/점프 | 1개 |
| Map 2 | 작음 | DoubleJump 구체 | 1개 |
| Map 3 | 작음 | Dash 구체, 탐색 구역 | 1개 |
| Map 4 | 작음 | 고난이도, DoubleJump+Dash 필요 | 1개 |
| Boss Map | 큼 | BossGate, 보스전 | 없음 |

### 진행 흐름
```
[Map 1] → 포탈 → [Map 2] → DoubleJump 획득 → [Map 1] → 새 구역 → ...
                                                        ↓
                                          [Map 3] → Dash 획득 → ...
                                                        ↓
                                          [Map 4] → 모든 능력 테스트 → Boss Map
```

### 함정 종류
1. **SpikeTrap**: 바닥/벽에 설치된 가시 (즉사)
2. **MovingPlatform**: 좌우 또는 상하로 움직이는 플랫폼
3. **PatrolEnemy**: 정해진 경로를 순찰하는 적 (즉사)

### 보스 시스템
- **입장 조건**: DoubleJump + Dash 능력 획득
- **BossGate**: 력이 없으면 진입 불가
- **공격 패턴**:
  - 투사체 발사
  - 지역 공격
  - 소환물
- **클리어**: 보스 처치 시 게임 클리어

---

## 🔧 기술 스택 및 아키텍처

### 의존성 주입 (DI)
- **DIContainer**: 가벼운 커스텀 DI 컨테이너
- **Service Lifetime**: Singleton, Transient 지원
- **주입 방식**: Constructor Injection, Field Injection

### SOLID 원칙 적용
- **SRP**: 각 클래스는 단일 책임만 담당 (최대 300행, 10메서드)
- **OCP**: 인터페이스 + 다형성으로 확장 (switch/if-else 금지)
- **LSP**: 자식 클래스는 부모를 대체 가능
- **ISP**: 작은 인터페이스들로 분리 (최대 5메서드)
- **DIP**: 인터페이스에 의존, DI로 주입

### 이벤트 시스템
- **EventBus**: 중앙 집중식 이벤트 관리
- **느슨한 결합**: 직접 참조 대신 이벤트 기반 통신

---

## 📝 Git 커밋 기록

| 날짜 | 커밋 메시지 | 변경 내용 |
|------|------------|-----------|
| 2026-03-17 | feat: Remove PerspectiveShift ability and Hidden system | Hidden 시스템 제거 (Tilemap 호환성 문제), 능력 2개로 단순화 (DoubleJump, Dash) |
| 2026-03-17 | feat: Implement Ability System with DoubleJump and Dash | AbilityManager, DoubleJump, Dash 능력 구현, DI 연동 |
| 2026-03-16 | feat: Implement PlayerController with DI integration | PlayerController, IAbilityManager, IDeathManager, GameInstaller 추가 |
| 2026-03-16 | feat: Implement DI Container system with RootContext architecture | DI Container 구현, Installer/RootContext 추가, AGENT.md 생성 |
| 2026-03-16 | docs: Update TaskList for Metroidvania style | 메트로바니아 스타일로 구조 변경 |
| 2026-03-12 | Initial commit: Project setup with folder structure | 폴더 구조 생성, TaskList.md 추가 |

---

## 📌 메모 및 참고사항

- **중요**: 모든 신규 코드는 SOLID_Coding_Standard.md 검토 후 작성
- **중요**: DI Container 사용 시 Inspector와의 조화 유지
- **중요**: 새로운 타입 추가 시 기존 코드 수정 금지 (OCP)
- **참고**: ScriptableObject는 Inspector에서 데이터 수정 가능
- **참고**: 복잡한 로직은 인터페이스 + DI로 분리
- **참고**: 메트로바니아 = 완전 자유 탐색, Ability Gate로 진행 조절
- **참고**: 플레이어 애니메이션은 Nine Pines Animation - Penguin 에셋 사용

---

*마지막 업데이트: 2026-03-17 (능력 시스템 구현 및 Hidden 시스템 제거)*
