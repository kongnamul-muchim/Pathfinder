# Pathfinder 게임 개발 Task List

> **프로젝트**: Pathfinder - 함정 피하기 2D 플랫포머  
> **시작일**: 2026-03-12  
> **Unity 버전**: 2022.3 LTS (URP 2D)  
> **코딩 표준**: SOLID 원칙 + Custom DI Container

---

## 📋 프로젝트 개요

**게임 목표**: 맵에 존재하는 모든 레버를 상호작용하면 문이 열리고, 문으로 들어가면 스테이지 클리어  
**보스 스테이지**: 배경에 보스가 나타나 기믹 공격을 피해 레버를 당기면 보스 체력 감소

**능력 해금 시스템**:
- 스테이지 1: 기본 조작 (이동, 점프)
- 스테이지 2 클리어: 2단 점프 해금
- 스테이지 3 클리어: 시점 변환 해금 (숨겨진 발판 표시/숨김)

---

## 📁 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/           # DI Container, GameManager, EventBus
│   ├── Player/         # PlayerMovement, PlayerAbilities, PlayerHealth
│   ├── Interactables/  # Lever, Door, InteractionSystem
│   ├── Traps/          # Spike, MovingPlatform, TimedTrap (인터페이스 기반)
│   ├── Enemies/        # PatrolEnemy, Boss
│   ├── UI/             # Timer, StageIndicator, LeverCounter
│   ├── Data/           # ScriptableObject 설정 파일들
│   └── Interfaces/     # 게임 전체 인터페이스 정의
├── Prefabs/            # 프리팹 저장
├── Sprites/            # 2D 스프라이트 (픽셀 아트)
└── Scenes/             # 5개 스테이지 씬
```

---

## ✅ 작업 목록

### Phase 1: 코어 시스템 구축
- [x] **Task 1.1**: DI Container 구현 (DI_Library.md 기반)
- [x] **Task 1.2**: EventBus 시스템 구현 (느슨한 결합)
- [ ] **Task 1.3**: GameManager 구현 (게임 상태 관리)
- [x] **Task 1.4**: Installer 설정 (DI 등록)

### Phase 2: 플레이어 시스템
- [ ] **Task 2.1**: PlayerMovement 구현 (기본 이동, 점프)
- [ ] **Task 2.2**: PlayerAbilities 구현 (2단 점프, 시점 변환)
- [ ] **Task 2.3**: PlayerHealth 구현 (데스, 리스폰)
- [ ] **Task 2.4**: PlayerInput 구현 (Input System)

### Phase 3: 상호작용 시스템
- [ ] **Task 3.1**: IInteractable 인터페이스 정의
- [ ] **Task 3.2**: Lever 구현 (레버 작동)
- [ ] **Task 3.3**: Door 구현 (문 열림/닫힘)
- [ ] **Task 3.4**: Lever-Door 연동 시스템

### Phase 4: 함정 시스템
- [ ] **Task 4.1**: ITrap 인터페이스 정의
- [ ] **Task 4.2**: SpikeTrap 구현 (가시 함정)
- [ ] **Task 4.3**: MovingPlatform 구현 (움직이는 플랫폼)
- [ ] **Task 4.4**: TimedTrap 구현 (시간 기반 함정)

### Phase 5: 적 및 보스 시스템
- [ ] **Task 5.1**: IEnemy 인터페이스 정의
- [ ] **Task 5.2**: PatrolEnemy 구현 (순찰 적)
- [ ] **Task 5.3**: Boss 구현 (배경 보스)
- [ ] **Task 5.4**: BossAttack 패턴 구현

### Phase 6: UI 시스템
- [ ] **Task 6.1**: Timer 구현
- [ ] **Task 6.2**: StageIndicator 구현
- [ ] **Task 6.3**: LeverCounter 구현
- [ ] **Task 6.4**: DeathCounter 구현

### Phase 7: 스테이지 구성
- [ ] **Task 7.1**: Stage 1 구성 (튜토리얼, 레버 1개)
- [ ] **Task 7.2**: Stage 2 구성 (2단 점프 해금, 레버 2개)
- [ ] **Task 7.3**: Stage 3 구성 (시점 변환 해금, 레버 3개)
- [ ] **Task 7.4**: Stage 4 구성 (고난이도, 레버 4개)
- [ ] **Task 7.5**: Stage 5 구성 (보스전, 레버 5개)

### Phase 8: 스프라이트 및 리소스
- [ ] **Task 8.1**: 플레이어 스프라이트 제작
- [ ] **Task 8.2**: 레버/문 스프라이트 제작
- [ ] **Task 8.3**: 함정 스프라이트 제작
- [ ] **Task 8.4**: 보스 스프라이트 제작
- [ ] **Task 8.5**: 배경 및 타일 스프라이트 제작

### Phase 9: 테스트 및 최적화
- [ ] **Task 9.1**: 기능 테스트
- [ ] **Task 9.2**: 성능 최적화
- [ ] **Task 9.3**: 버그 수정

### Phase 10: 마무리
- [ ] **Task 10.1**: 빌드 테스트
- [ ] **Task 10.2**: 문서화
- [ ] **Task 10.3**: Git 커밋 및 정리

---

## 🎮 게임 디자인 상세

### 스테이지별 구성

| 스테이지 | 유형 | 레버 수 | 능력 | 특징 |
|---------|------|---------|------|------|
| 1 | 튜토리얼 | 1개 | 기본 | 이동/점프 학습 |
| 2 | 퍼즐 | 2개 | 2단 점프 해금 | 높은 곳의 레버 |
| 3 | 퍼즐 | 3개 | 시점 변환 해금 | 숨겨진 레버 발견 |
| 4 | 고난이도 | 4개 | 모두 사용 | 복잡한 함정 배치 |
| 5 | 보스전 | 5개 | 모두 사용 | 보스 기믹 공격 회피 |

### 함정 종류
1. **SpikeTrap**: 바닥/벽에 설치된 가시
2. **MovingPlatform**: 좌우 또는 상하로 움직이는 플랫폼
3. **TimedTrap**: 특정 시간 간격으로 활성화되는 함정
4. **PatrolEnemy**: 정해진 경로를 순찰하는 적

### 보스 스테이지 상세
- **보스 체력**: 5칸 (레버 1개당 1칸 감소)
- **공격 패턴**:
  - 투사체 발사 (예측 가능한 패턴)
  - 지역 공격 (바닥에 경고 표시 후 공격)
  - 소환물 (작은 적이나 장애물 소환)
- **클리어 조건**: 모든 레버 작동 → 보스 체력 0 → 자동 클리어

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
| 2026-03-16 | feat: Implement DI Container system with RootContext architecture | DI Container 구현, Installer/RootContext 추가, AGENT.md 생성 |
| 2026-03-12 | Initial commit: Project setup with folder structure | 폴더 구조 생성, TaskList.md 추가 |

---

## 📌 메모 및 참고사항

- **중요**: 모든 신규 코드는 SOLID_Coding_Standard.md 검토 후 작성
- **중요**: DI Container 사용 시 Inspector와의 조화 유지
- **중요**: 새로운 타입 추가 시 기존 코드 수정 금지 (OCP)
- **참고**: ScriptableObject는 Inspector에서 데이터 수정 가능
- **참고**: 복잡한 로직은 인터페이스 + DI로 분리

---

*마지막 업데이트: 2026-03-16*
