# Agent Guidelines

## 📚 문서 참조 가이드

### 상황별 참조 문서

| 상황 | 참조 문서 |
|------|-----------|
| 프로젝트 개요 확인 | `ProjectOverview.md` |
| 진행중 작업 확인 | `TaskList.md` |
| 개발 히스토리 | `DevelopmentLog.md` |
| 코어 아키텍처 작업 | `DI_Library.md`, `SOLID_Coding_Standard.md` |
| 워프/체크포인트 시스템 | `WarpPoint_Guide.md` |
| Unity 씬 구성 | `Unity_Scene_Setup_Guide.md` |
| 애니메이션 설정 | `Animation_Setup_Guide.md` |
| 리팩토링 기록 | `RefactoringLog.md` |

---

## ⚠️ 타일맵 작업 규칙

**코드로 자동 생성하지 않음**
- 모든 타일맵은 Unity 에디터에서 수동으로 배치
- Tile Palette 사용

---

## 일반 개발 가이드라인

- 기존 코드 컨벤션 준수
- 공개 API는 XML 주석으로 문서화
- 명시적 코드 선호 (유지보수성)
- 새로운 타입 추가 시 기존 코드 수정 금지 (OCP)