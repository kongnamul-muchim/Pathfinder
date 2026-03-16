# Player Animation Setup Guide

## 개요
PlayerAnimator 시스템이 구현되었습니다. Unity Editor에서 애니메이션을 연결해야 합니다.

## 완료된 작업

### 1. PlayerAnimator 컴포넌트 (`Assets/Scripts/Player/PlayerAnimator.cs`)
- 애니메이션 상태 관리 (Idle, Walk, Jump, Death)
- 방향 전환 (Sprite Flip X)
- 파라미터 기반 상태 전환

### 2. PlayerController 통합 (`Assets/Scripts/Player/PlayerController.cs`)
- 이동 입력에 따라 Walk 애니메이션 트리거
- 점프 시 Jump 애니메이션 트리거
- 사망 시 Death 애니메이션 트리거
- 좌우 이동 시 Sprite 자동 반전

### 3. Animator Controller (`Assets/Animations/PlayerAnimatorController.controller`)
- 상태: Idle → Walk → Jump → Death
- 파라미터: IsWalking, IsJumping, IsGrounded, Death

## Unity에서 설정해야 할 작업

### Step 1: Animation Clips 연결

1. **Unity Editor에서 `PlayerAnimatorController` 열기**
   - `Assets/Animations/PlayerAnimatorController.controller` 더블클릭

2. **각 상태에 Animation Clip 할당**
   - **Idle 상태**: `penguin_idle` 애니메이션 드래그
   - **Walk 상태**: `penguin_walk` 애니메이션 드래그
   - **Jump 상태**: `penguin_jump` 애니메이션 드래그
   - **Death 상태**: `penguin_slide` 또는 새로운 Death 애니메이션 사용

### Step 2: Player 프리팹 설정

1. **Player GameObject에 컴포넌트 추가**
   - `PlayerAnimator` 컴포넌트 추가 (자동으로 Animator, SpriteRenderer 필요)
   - 기존 `PlayerController`는 이미 애니메이션 통합됨

2. **Animator 컴포넌트 설정**
   - Controller: `PlayerAnimatorController` 할당

3. **Sprite Renderer 설정**
   - Sprite: Penguin 스프라이트 중 하나 선택 (예: `penguin_idle_01`)

### Step 3: Animator Controller 검증

**파라미터 확인:**
- IsWalking (Bool)
- IsJumping (Bool)
- IsGrounded (Bool)
- Death (Trigger)

**상태 전환 확인:**
- Idle ↔ Walk: IsWalking 파라미터
- Any State → Death: Death 트리거
- Idle/Walk → Jump: IsJumping 파라미터

## 파일 위치

```
Assets/
├── Scripts/
│   └── Player/
│       ├── PlayerController.cs      (애니메이션 통합 완료)
│       └── PlayerAnimator.cs        (새로 생성)
├── Animations/
│   └── PlayerAnimatorController.controller  (새로 생성)
└── Nine Pines Animation/
    └── 2D Character Sprite Animation - Penguin/
        ├── Animations/
        │   ├── penguin_idle.anim
        │   ├── penguin_walk.anim
        │   ├── penguin_jump.anim
        │   └── penguin_slide.anim
        └── sprites/
            ├── penguin_idle_01.png
            ├── penguin_walk_01.png
            └── ...
```

## 애니메이션 상태 다이어그램

```
         +--------+      IsWalking=True      +--------+
         |        | ------------------------> |        |
  Entry  |  Idle  |                           |  Walk  |
 ------->|        | <------------------------ |        |
         +--------+      IsWalking=False      +--------+
           |   ^
           |   | IsJumping=False
   IsJumping=True
           |   |
           v   |
         +--------+      Death (Any State)    +--------+
         |  Jump  | ------------------------> | Death  |
         +--------+                           +--------+
```

## 주의사항

1. **Sprite Flip**: PlayerAnimator에서 자동으로 처리됨
   - 오른쪽 이동: flipX = false
   - 왼쪽 이동: flipX = true

2. **Animation Transitions**: 모든 전환은 0.1초로 설정됨

3. **Death Animation**: Any State에서 Death로 즉시 전환

4. **테스트 시 확인할 것**:
   - 걷기 시 Walk 애니메이션 재생
   - 멈추면 Idle로 돌아감
   - 점프 시 Jump 애니메이션
   - 좌우 이동 시 스프라이트 반전
   - 함정 접촉 시 Death 애니메이션

## 다음 단계

- [ ] Unity에서 Animation Clip 연결
- [ ] Player 프리팹에 Animator 설정
- [ ] 게임 테스트 및 디버깅
