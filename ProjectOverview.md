# Pathfinder 프로젝트 개요

> **프로젝트**: Pathfinder - 메트로바니아 스타일 2D 플랫포머  
> **시작일**: 2026-03-12  
> **Unity 버전**: 2022.3 LTS (URP 2D)  
> **코딩 표준**: SOLID 원칙 + Custom DI Container

---

## 게임 개요

**목표**: 4개의 맵을 자유롭게 탐색하여 능력 구체를 획득하고, 모든 능력을 모으면 게임 클리어

**핵심 메커니즘**:
- **즉사 시스템**: 함정/적 접촉 시 즉시 사망 (HP 없음)
- **능력 해금**: 각 맵에 배치된 능력 구체 획득 시 해금
- **자유 탐색**: 능력이 없으면 못 가는 곳 존재
- **백트래킹**: 이전 맵으로 돌아가 새로운 구역 탐색

---

## 맵 구성

| 맵 | 크기 | 특징 | 세이브포인트 |
|----|------|------|--------------|
| Map 1 | 작음 | 튜토리얼, 기본 이동/점프 | 1개 |
| Map 2 | 작음 | DoubleJump 구체 | 1개 |
| Map 3 | 작음 | Dash 구체 | 1개 |
| Map 4 | 작음 | 고난이도 | 1개 |

---

## 능력 시스템

| 능력 | 위치 | 효과 |
|------|------|------|
| DoubleJump | Map 2 | 2단 점프, 높은 곳 접근 |
| Dash | Map 3 | 빠른 이동, 방향키 더블탭 |

---

## 진행 흐름

```
[Map 1] → 포탈 → [Map 2] → DoubleJump 획득 → [Map 1] → 새 구역
                                                      ↓
                                        [Map 3] → Dash 획득
                                                      ↓
                                        [Map 4] → 게임 클리어
```

---

## 함정 종류

1. **SpikeTrap**: 바닥/벽 가시 (즉사)
   - "Trap" 태그로 PlayerController와 연동

2. **MovingPlatform**: 움직이는 플랫폼
   - X축만 동기화, Y축은 자유
   - 왕복 이동

---

## 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/              # DI Container, GameManager
│   ├── Player/            # PlayerController, AbilityManager
│   ├── World/             # MapManager, Portal, Checkpoint
│   ├── Abilities/         # AbilityUnlockable
│   ├── Traps/             # Spike, MovingPlatform
│   ├── UI/                # DeathCounter, AbilityUI
│   └── Interfaces/        # 인터페이스 정의
├── Prefabs/
├── Sprites/
└── Scenes/
```

---

## 기술 스택

### DI Container
- 가벼운 커스텀 DI 컨테이너
- Singleton, Transient 지원
- Constructor/Field Injection

### SOLID 원칙
- **SRP**: 클래스당 최대 300행, 10메서드
- **OCP**: switch/if-else 금지, 인터페이스 사용
- **LSP**: 타입 체크 금지
- **ISP**: 인터페이스당 최대 5메서드
- **DIP**: 인터페이스 의존, DI 주입

---

## 참고사항

- 모든 신규 코드는 `SOLID_Coding_Standard.md` 검토 후 작성
- DI Container 사용 시 Inspector와의 조화 유지
- 플레이어 애니메이션: Nine Pines Animation - Penguin 에셋
- 능력 상자: Q키 상호작용
- 목숨 시스템: 추가 목숨 획득 시 죽어도 리스폰