# WarpPoint 시스템 구현 현황

> **작성일**: 2026-03-17  
> **목표**: WarpPoint 시스템 구현 및 테스트 준비 완료

---

## ✅ 완료된 작업

### 1. GameInstaller 수정
**파일**: `Assets/Scripts/Core/GameInstaller.cs`

**수정 내용**:
```csharp
// 기존: 주석만 있고 등록 코드 없음
// 맵 관리자 (MonoBehaviour이므로 씬에서 찾아서 등록)
// 참고: MapManager는 씬에 배치된 후 RootContext에서 자동 주입됨

// 수정 후: 실제 등록 코드 추가
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

**결과**: 
- IMapManager 인터페이스가 DI Container에 등록됨
- WarpPoint와 Portal이 IMapManager를 주입받을 수 있게 됨

---

### 2. 테스트 계획 수립

**테스트 시나리오**: 1-1Map → 1-2Map 워프포인트 이동

**Phase 1**: 단일 맵 테스트 (체크포인트만)
- 같은 맵 내에서 워프포인트 활성화 테스트
- DeathManager에 체크포인트 저장 확인

**Phase 2**: 맵 간 이동 테스트
- MapA에서 MapB로 이동
- MapManager의 SetActive로 맵 전환
- 플레이어 위치 이동 확인

**Phase 3**: 완전 통합 테스트
- 체크포인트 → 워프 → 리스폰 체인 테스트

---

### 3. Unity Editor 설정 가이드 작성
**파일**: `Docs_WarpPoint_Test_Setup.md`

**포함 내용**:
- 씬 구조 설정 방법
- MapManager Inspector 설정
- WarpPoint 컴포넌트 설정
- 단계별 테스트 방법
- 문제 해결 가이드

---

## 📋 현재 시스템 상태

### DI Container 설정
- ✅ RootContext 오브젝트 존재
- ✅ GameInstaller가 RootContext의 Installers에 등록됨
- ✅ MapManager 오브젝트가 씬에 존재
- ✅ IAbilityManager 등록됨
- ✅ IDeathManager 등록됨
- ✅ IMapManager 등록됨 (GameInstaller 수정으로 추가)

### WarpPoint 시스템
- ✅ WarpPoint.cs 구현 완료
- ✅ Portal.cs 구현 완료
- ✅ MapManager.cs 구현 완료
- ✅ IMapManager 인터페이스 정의 완료
- ⏳ Unity Editor에서 맵 설정 필요
- ⏳ 실제 테스트 필요

### 캐릭터 시스템
- ✅ PlayerController 구현 완료
- ✅ 벽 감지 및 미끄러짐 시스템 구현 완료
- ✅ Physics Material 2D 생성 완료

---

## 🔧 Unity Editor에서 해야 할 작업

### 맵 구조 설정
```
SampleScene
├── RootContext
│   └── GameInstaller (✅ 이미 설정됨)
├── MapManager (✅ 이미 존재)
├── Player (✅ 이미 존재)
└── Maps (⏳ 새로 생성 필요)
    ├── 1-1Map (⏳ 기존 맵을 이 그룹으로 이동)
    │   ├── Tilemap
    │   ├── SpawnPoint_1-1 (⏳ 새로 생성)
    │   └── WarpPoint_1-1 (⏳ 새로 생성)
    └── 1-2Map (⏳ 기존 맵을 이 그룹으로 이동)
        ├── Tilemap
        ├── SpawnPoint_1-2 (⏳ 새로 생성)
        └── WarpPoint_1-2 (⏳ 새로 생성)
```

### MapManager 설정
**Inspector에서 설정**:
- Maps 리스트 Size: 2
- Element 0: MapId="1-1Map", MapRoot=1-1Map, SpawnPoint=SpawnPoint_1-1
- Element 1: MapId="1-2Map", MapRoot=1-2Map, SpawnPoint=SpawnPoint_1-2
- Starting Map Index: 0

### WarpPoint 설정
**WarpPoint_1-1**:
- Warp Point Id: "Warp_1-1_to_1-2"
- Target Map Id: "1-2Map"
- Target Warp Point Id: "" (빈칸)
- Start Activated: false

**WarpPoint_1-2**:
- Warp Point Id: "Warp_1-2_Entrance"
- Target Map Id: "" (빈칸 - 체크포인트만)
- Start Activated: true

---

## 🎮 테스트 체크리스트

### 설정 단계
- [ ] Maps 부모 오브젝트 생성
- [ ] 1-1Map, 1-2Map 그룹화
- [ ] SpawnPoint 오브젝트 생성 (각 맵마다 1개)
- [ ] WarpPoint 오브젝트 생성 (스프라이트 선택사항)
- [ ] MapManager에 Maps 데이터 등록
- [ ] WarpPoint Inspector 설정

### 실행 단계
- [ ] Play 모드 진입
- [ ] Console 로그 확인: "[GameInstaller] MapManager registered"
- [ ] 시작 맵 자동 활성화 확인
- [ ] 플레이어를 WarpPoint_1-1로 이동
- [ ] E키 입력으로 활성화
- [ ] 맵 전환 확인 (1-1Map 비활성화, 1-2Map 활성화)
- [ ] 플레이어가 SpawnPoint_1-2로 이동 확인
- [ ] 죽었을 때 마지막 체크포인트로 리스폰 확인

---

## 📁 관련 파일 목록

### 스크립트 파일
- `Assets/Scripts/Core/GameInstaller.cs` - DI 등록 수정됨
- `Assets/Scripts/World/WarpPoint.cs` - 워프포인트 구현
- `Assets/Scripts/World/MapManager.cs` - 맵 관리자
- `Assets/Scripts/World/Portal.cs` - 포탈
- `Assets/Scripts/Player/PlayerController.cs` - 플레이어 컨트롤러

### 문서 파일
- `Docs_WarpPoint_Test_Setup.md` - 테스트 설정 가이드
- `Docs_WarpPoint_Implementation_Summary.md` - 이 문서

### 리소스 파일
- `Assets/Materials/PlayerPhysicsMaterial.physicsMaterial2D` - 플레이어 물리 재질

---

## 🎯 다음 목표

1. **Unity Editor 설정**: 맵 구조 정리 및 컴포넌트 설정
2. **기능 테스트**: WarpPoint 시스템 작동 확인
3. **버그 수정**: 테스트 중 발견된 문제 해결
4. **추가 기능**: ScreenFade, 사운드 이펙트 등

---

## 💡 참고 사항

- 스프라이트 없이도 WarpPoint는 작동함 (Gizmos로 위치 확인 가능)
- Input System 설정 확인 필요 (Project Settings → Player)
- Console 로그 필터: "[MapManager]", "[DeathManager]", "[GameInstaller]"

---

**Git Commit**: `237f72e` - MapManager DI 등록 및 테스트 가이드 작성
