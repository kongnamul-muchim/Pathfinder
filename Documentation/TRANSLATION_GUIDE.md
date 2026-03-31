# Unity 프로젝트 한글 → 영어 번역 가이드

## 개요

Unity 프로젝트의 모든 한글을 영어로 번역하여 WebGL 빌드 시 폰트 깨짐 문제를 해결하는 가이드입니다.

**적용 대상:** 한글 UI, CSV 데이터, C# 코드, Excel 원본

---

## 1. 번역 대상 파일 유형

### 1.1 데이터 파일 (CSV/Excel)

| 파일 위치 | 설명 |
|-----------|------|
| `Assets/EditorData/Data/csv/*.csv` | 게임 데이터 (아이템, 몬스터, 스킬 등) |
| `Assets/EditorData/Data/*.xlsm` | Excel 원본 (CSV 변환 소스) |

**번역 컬럼:**
- `Name` — 이름
- `Description` — 설명
- `Element` — 속성 (불→Fire, 물→Water, 풀→Grass)

### 1.2 C# 코드 파일

| 유형 | 예시 |
|------|------|
| UI 문자열 | `"승리!"` → `"Victory!"` |
| Debug.Log | `Debug.Log("초기화 완료")` → `Debug.Log("Initialized")` |
| Header/Tooltip | `[Header("기본 스탯")]` → `[Header("Base Stats")]` |
| Contains 체크 | `name.Contains("회복")` → `name.Contains("Recovery")` |

### 1.3 Unity 씬/프리팹

| 파일 위치 | 설명 |
|-----------|------|
| `Assets/Scenes/*.unity` | 씬 파일 (TextMeshPro 텍스트) |
| `Assets/Prefabs/**/*.prefab` | 프리팹 (TextMeshPro 텍스트) |

**확인 방법:**
```bash
# 한글 포함된 씬/프리팹 검색
rg '[가-힣]' Assets/Scenes/ --type-add 'unity:*.unity' --type unity
rg '[가-힣]' Assets/Prefabs/ --type-add 'prefab:*.prefab' --type prefab
```

---

## 2. 번역 워크플로우

### 2.1 전체 흐름

```
1. 한글 포함 파일 스캔
2. CSV 데이터 번역
3. Excel(xlsm) 번역
4. C# 코드 번역
5. Contains 체크 수정
6. ScriptableObject 재생성
7. WebGL 빌드
8. GitHub Pages 배포
```

### 2.2 파일 스캔

```bash
# 한글 포함 파일 검색
rg '[가-힣]' --include '*.cs'
rg '[가-힣]' --include '*.csv'
rg '[가-힣]' --include '*.unity'
rg '[가-힣]' --include '*.prefab'
```

---

## 3. CSV 번역 패턴

### 3.1 CSV 컬럼 구조 예시

```csv
ID,Name,Type,Description,BuyPrice,SellPrice,...
1,회복포션(소),Heal,체력을 소량 회복한다,50,25,...
```

**번역 후:**
```csv
ID,Name,Type,Description,BuyPrice,SellPrice,...
1,Healing Potion (S),Heal,Restores a small amount of HP,50,25,...
```

### 3.2 주의사항

- **콤마(,) 포함 금지** — CSV 파싱 깨짐
  - ❌ `"A dusty shield covered in cobwebs, long abandoned"`
  - ✅ `"A dusty shield covered in cobwebs - long abandoned"`
- **영어 enum 값 변경 금지** — `Fire`, `Water`, `Grass`, `None` 등
- **아이콘 경로 변경 금지** — `Weapons/Sword`, `Items/Potion` 등

---

## 4. C# 번역 패턴

### 4.1 UI 문자열

```csharp
// Before
_text.text = "승리! {goldEarned}G 획득";
_text.text = "패배...";
_text.text = "게임 오버!";

// After
_text.text = $"Victory! Earned {goldEarned}G";
_text.text = "Defeat...";
_text.text = "Game Over!";
```

### 4.2 Debug.Log

```csharp
// Before
Debug.Log($"초기화 완료 - 몬스터: {count}");
Debug.LogWarning("골드가 부족합니다.");

// After
Debug.Log($"Initialized - Monsters: {count}");
Debug.LogWarning("Not enough gold.");
```

### 4.3 Header/Tooltip (Inspector)

```csharp
// Before
[Header("기본 스탯")]
[Tooltip("레벨당 기본 스탯에 더해지는 비율 (0.1 = 10%)")]

// After
[Header("Base Stats")]
[Tooltip("Ratio added to base stats per level (0.1 = 10%)")]
```

### 4.4 Contains 체크 (중요!)

CSV 데이터 이름이 바뀌면 C#의 Contains 체크도 같이 수정해야 함:

```csharp
// Before (CSV가 한글일 때)
if (skill.Name.Contains("회복")) return BuffType.Heal;
if (skill.Name.Contains("공격력")) return BuffType.Attack;
if (skill.Name.Contains("방어력")) return BuffType.Defense;
if (skill.Name.Contains("속도")) return BuffType.Speed;
if (skill.Name.Contains("체력")) return BuffType.HP;

// After (CSV가 영어일 때)
if (skill.Name.Contains("Recovery")) return BuffType.Heal;
if (skill.Name.Contains("Attack")) return BuffType.Attack;
if (skill.Name.Contains("Defense")) return BuffType.Defense;
if (skill.Name.Contains("Speed")) return BuffType.Speed;
if (skill.Name.Contains("Vitality")) return BuffType.HP;
```

**Contains 체크 목록 확인 방법:**
```bash
rg 'Contains\(' --include '*.cs'
```

---

## 5. Excel(xlsm) 번역

### 5.1 시트 구조

Excel 파일은 여러 시트로 구성되며, 각 시트가 하나의 CSV에 대응:

| 시트 이름 | CSV 대응 |
|-----------|----------|
| MonsterData | MonsterData.csv |
| MonsterSkill | MonsterSkill.csv |
| EquipmentData | EquipmentData.csv |
| SkillData | SkillData.csv |
| ConsumableData | ConsumableData.csv |

### 5.2 번역 방법

1. Excel 파일 열기
2. 각 시트의 Name, Description 컬럼 번역
3. Element 컬럼 번역 (불→Fire, 물→Water, 풀→Grass)
4. 저장

---

## 6. ScriptableObject 재생성

번역 후 Unity에서 ScriptableObject를 재생성해야 함:

### 6.1 순서

```
1. Unity Editor 열기
2. Tools > CSV > Convert All 실행
3. Tools > Addressables > Set All SO Labels 실행
4. Tools > Addressables > Build Addressables 실행
```

### 6.2 확인

- `Assets/ScriptableObjects/Data/` 폴더의 .asset 파일명이 영어로 변경되었는지 확인
- 예: `Consumable_1_회복포션(소).asset` → `Consumable_1_Healing Potion (S).asset`

---

## 7. WebGL 빌드 및 배포

### 7.1 빌드 설정

```
File > Build Settings > WebGL > Build
빌드 경로: 프로젝트/docs/ 폴더
```

### 7.2 GitHub Pages 설정

```
GitHub repo Settings > Pages
Source: Deploy from a branch
Branch: main (또는 master)
Folder: /docs
```

### 7.3 .gitignore 주의

```
[Bb]uild/
[Bb]uilds/
```
위 패턴이 있으면 docs/Build/도 무시됨. `git add -f docs/Build/`으로 강제 추가하거나, .gitignore에서 제거.

---

## 8. 검증 체크리스트

### 8.1 번역 완료 후 확인

```bash
# 한글 잔여 검출
rg '[가-힣]' --include '*.cs'
rg '[가-힣]' --include '*.csv'
rg '[가-힣]' --include '*.unity'
rg '[가-힣]' --include '*.prefab'

# Contains 체크 확인
rg 'Contains\(' --include '*.cs'

# CSV 컬럼 수 확인
awk -F',' '{print NR": "NF" fields"}' 파일.csv
```

### 8.2 자주 놓치는 부분

| 항목 | 확인 방법 |
|------|-----------|
| Contains 체크 | `rg 'Contains\(' --include '*.cs'` |
| CSV 콤마 | 설명에 콤마(,) 포함 여부 |
| ScriptableObject 파일명 | Unity에서 재생성 후 영어명 확인 |
| .gitignore | `Build/` 패턴이 docs/Build/도 무시하는지 |
| xlsm 시트 | Excel 원본도 번역했는지 |

---

## 9. 번역 스타일 가이드

### 9.1 일반 RPG 스타일

| 한글 | 영어 | 비고 |
|------|------|------|
| 슬라임 | Slime | 일반 RPG 표준 |
| 역병쥐 | Plague Rat | 속성 반영 |
| 해골 | Skeleton | 일반 RPG 표준 |
| 켈베로스 | Cerberus | 고유명사 |
| 강타 | Power Strike | 스킬 느낌 반영 |
| 연속공격 | Double Strike | 동작 설명 |
| 회복포션(소) | Healing Potion (S) | 크기 표기 유지 |
| 승리! | Victory! | 감탄사 유지 |
| 패배... | Defeat... | 느낌표/마침표 유지 |

### 9.2 속성 번역

| 한글 | 영어 |
|------|------|
| 불 | Fire |
| 물 | Water |
| 풀 | Grass |
| 무 | None |

### 9.3 버프 타입 번역

| 한글 | 영어 |
|------|------|
| 공격력 | Attack |
| 방어력 | Defense |
| 속도 | Speed |
| 체력 | HP / Vitality |

---

## 10. 문제 해결

### Q: WebGL에서 한글이 깨져요
**A:** 모든 한글을 영어로 번역하세요. 이 가이드의 워크플로우를 따르세요.

### Q: 게임이 실행 안 돼요
**A:** Contains 체크가 번역된 이름과 맞는지 확인하세요.

### Q: ScriptableObject가 안 보여요
**A:** `Tools > CSV > Convert All`을 실행하세요.

### Q: GitHub Pages에서 404 에러
**A:** `git add -f docs/Build/`으로 빌드 폴더를 강제 추가하세요.

### Q: CSV 파싱이 깨져요
**A:** 설명에 콤마(,)가 포함되어 있는지 확인하세요. 대시(-)로 대체하세요.
