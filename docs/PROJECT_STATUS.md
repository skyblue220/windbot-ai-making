# Ryzeal AI — 프로젝트 현황 (정리본)

- 작성 시점: 2026-10-14
- 기준 작업 폴더: **`WindBot-Git`** (아래 2절 참고)
- 이 문서의 표기 규칙
  - **확인**: 업로드된 소스·로그·바이너리를 직접 열어 확인한 사실
  - **작성자 확인**: 개발자가 직접 알려준 내용 (이 문서 작성 중 코드로 재검증하지 않음)
  - **추정 / 미확인**: 근거가 있으나 확정하지 못한 내용

---

## 1. 목적

**단기 목표**: EDOPro/WindBot에서 실제로 실행되는 Ryzeal AI를 만들고, 판단 과정을 관측(로그)할 수 있게 한다.

**현재 단계**: 학습 모델이나 탐색(MCTS 등)은 없다. 현재는 다음 세 가지로 구성된다.

```text
엔진이 제공하는 합법 행동
  + 규칙 기반 실행기 (카드별 Commit / 후속 선택)
  + 임시 평가 정책 (BaselineScore)
  + 구조화 로그 (JSONL)
```

**장기 방향 (논의 중, 미확정)**: 현재 구조를 "v1: Seeded Heuristic Bot"으로 보고, 같은 후보 생성/실행 기반 위에서 판단 부분만 교체하는 "v2: Decision AI"를 별도 단계로 두는 안이 논의되었다. 아직 결정된 사항은 아니다.

---

## 2. 기준 환경과 빌드

### 2.1 기준 폴더는 `WindBot-Git`

`ExecutorBase` DLL 두 개를 비교한 결과 (**확인**):

| DLL | 내장 PDB 경로 |
|---|---|
| `ExecutorBase_Release.dll` | `...\working project\`**`WindBot-Git`**`\ExecutorBase\obj\Release\ExecutorBase.pdb` |
| `ExecutorBase_Deployed.dll` | `...\working project\`**`windbot-ai-making`**`\ExecutorBase\obj\Release\ExecutorBase.pdb` |

- 두 DLL은 같은 프로젝트를 **서로 다른 작업 폴더에서 빌드**한 산출물이다.
- 파일 크기는 같고(155,648 B) SHA-256은 다르다. 다른 바이트는 417개이며, IL 메서드 본문 영역(약 45 KB)의 차이는 1바이트(코드가 아닌 `.text` 맨 앞)뿐이다. 나머지는 PE 헤더, 모듈 GUID, 디버그 정보(PDB 경로·GUID), 그리고 경로 문자열 길이 차이로 뒤쪽 구조가 밀린 부분이다. → **컴파일된 코드 본문은 사실상 동일**하다.
- 한계: `#~` 메타데이터 테이블의 9바이트 차이는 테이블 종류까지 파싱하지 못했다(위치 이동에 따른 값 변경으로 **추정**). 전체 IL 비교는 도구 제약(디스어셈블러 크래시)으로 수행하지 못했다.
- 결론: `WindBot-Git`을 기준으로 삼고, `windbot-ai-making`은 이전 작업선의 잔재로 취급한다.

### 2.2 배포 경로는 두 가지가 있었고, 섞으면 안 된다

| 경로 | 내용 | 상태 |
|---|---|---|
| 경로 1 | WindBot 본체(`GameAI`, `Executor`) 수정 + Ryzeal 후보 정책 | **실제 실행·로그 생성 확인** (현재 개발 기준) |
| 경로 2 | 외부 Executor DLL(`Executors/`)로 확장 | Stage 01 로딩 확인용 Probe만 있음. 빌드·로딩 성공 **미확인**. 플레이 AI 아님 |

외부 DLL만으로는 본체에 추가한 `TryGetMainPhaseAction()` 훅이나 관측 코드가 생기지 않는다. 패치는 항상 **실제 실행 중인 프로젝트 기준**으로 적용한다.

### 2.3 과거 혼선 기록

- 기존 로그의 Extra Deck에 문서상 덱에 없는 카드 ID가 섞여 있었다 (`8728498`, `66011101`, `21044178`, `16643334`, `90590303`, 손패의 `87126721`). 문서상 덱의 Extra Deck 5종은 로그에 보이지 않았다.
- 원인에 대한 개발자 설명: 브랜치 생성·복사 과정에서 구버전 파일이 섞였다. (**작성자 확인, 코드로 검증하지 않음**)
- 같은 원인으로 보이는 사례: `ClientCard.cs`가 버전마다 다름(`LastLocation`, `IsOnField()`, `GetOriginCode()` 추가, `IsCode(long)`, `Setcode` 타입 변경 등).
- `deck.zip`의 카드 스크립트 목록(메인 25종, Extra 12종)은 기존에 확정한 덱 구성과 일치한다. (**확인**) 다만 이것은 종류 목록이며 매수는 포함하지 않는다.
- **따라서 기존 로그에서 얻은 결론은 `WindBot-Git` 빌드로 로그를 다시 수집하기 전까지 잠정으로 취급한다.**

---

## 3. 엔진 흐름 (소스로 확인한 내용)

```text
OCG Core
  └─ MSG_SELECT_IDLECMD 패킷 (합법 행동 목록을 직렬화)
        ↓
GameBehavior.OnSelectIdleCmd(BinaryReader)
  └─ new MainPhase() 생성, 패킷을 읽어 리스트를 채움
       SummonableCards / SpecialSummonableCards / ReposableCards /
       MonsterSetableCards / SpellSetableCards / ActivableCards + ActivableDescs
  └─ card.ActionIndex[k] = i,  card.ActionActivateIndex[desc] = i
        ↓
GameAI.OnSelectIdleCmd(main)
  ├─ [추가한 훅] Executor.TryGetMainPhaseAction(main, out action)
  │       true  → 그 action 반환
  │       false → 기존 Executors 루프(ShouldExecute)로 폴백
  └─ MainPhaseAction.ToValue() = (Index << 16) + Action  →  Core로 응답
```

- `ClientCard.ActionIndex`는 **`int[16]` 배열**이다 (현재 `ClientCard.cs`로 재확인). 현재 `RyzealExecutor.GetIdleActionIndex()`는 `card.ActionIndex[slot]`을 범위 검사와 함께 읽는다. (**확인**) 배열 전체를 `int`에 대입하는 코드는 없다.
- 합법성은 엔진이 먼저 판정한다. `MainPhase`의 리스트에 있다는 것은 "코어가 이 행동을 선택 가능하다고 보냈다"는 뜻이다.
- HOPT가 코어에서 걸러지는지 여부는 카드 Lua의 `SetCountLimit`으로 **강하게 시사**되나, 이 프로젝트의 코어 소스(`processor.cpp` 등)를 직접 보지는 못했다. 로그로 검증하기 전까지는 **미확정**이다.

---

## 4. 현재 후보 기반 경로

```text
GameAI.OnSelectIdleCmd()
      ↓
Executor.TryGetMainPhaseAction()
      ↓
RyzealExecutor.TryGetMainPhaseAction()
  ├─ GenerateCandidates(MainPhase main)       ← 엔진이 준 리스트에서 후보 생성
  ├─ 정렬: BaselineScore 내림차순, 동률이면 Label 문자열(Ordinal) 오름차순
  ├─ 순서대로 Commit() 시도
  │     성공 → MainPhaseAction 반환 (+ mainPhaseDecision 로그)
  └─ 후보 없음 / 모두 실패 → 레거시 Executor로 폴백 (+ 사유 기록)
```

- `GenerateCandidates(MainPhase main)`와 `TryGetMainPhaseAction`은 현재 `RyzealExecutor.cs`에 실제로 존재한다. (**확인**)
- **한계**: 정책 후보가 하나라도 실행되면 그 판단에서 레거시 Executor는 실행되지 않는다. 즉 정책이 지원하지 않는 행동과 정책 후보를 **같은 자리에서 비교하지 않는다.**
- **동률 처리는 전략이 아니라 라벨 문자열 순서다.** (`ThenBy(x => x.Label, StringComparer.Ordinal)`, **확인**) 현재 로그에서 동률이 결정된 사례:
  - `IceRyzeal normal summon` vs `IceRyzeal rule special summon` (둘 다 80) → 일반소환 선택 (`n` < `r`)
  - `Detonator Xyz summon` vs `Duo Drive Xyz summon` (둘 다 0) → Detonator 선택 (`De` < `Du`)

### 4.1 `ActionCandidate` 필드

```text
ActionType, CardId, ActionIndex, ActivationDescription,
BaselineScore, Label, GoalTag, SelectionPlan, Commit
```

`BaselineScore`는 최종 행동 가치나 미래 상태 가치가 아니라 **임시 평가값**이다. `SelectionPlan`은 예정된 선택이며 실제 결과가 아니다.

---

## 5. 카드별 구현 상태

범례: ✔ 확인 / △ 구현했으나 임시·미완성 / ✘ 미확인

| 기능 | 구현 | 후보 생성 | 실제 선택·실행 (로그) | 비고 |
|---|---|---|---|---|
| Ice 일반소환 | ✔ | ✔ | ✔ | 판단 3에서 선택 |
| Ice 덱 특수소환(일반소환 성공 시) | 기존 실행 로직 유지 | — | ✔ (다음 상태에서 Ice·Node 전개) | |
| Ice 룰 특수소환 | ✔ | ✔ | ✔ | 판단 7에서 선택(점수 60) |
| Ext 룰 특수소환·서치 | ✔ | ✔ | ✔ | Aggregator 묘지 송신, Sword 확보 (**작성자 확인**) |
| Sword 룰 특수소환·서치 | ✔ | ✔ | ✔ | Ice 확보 (**작성자 확인**) |
| Node 룰 특수소환·소생 효과 | △ 임시 선택 로직 | — | ✘ | 소생 실행 미확인 |
| Detonator Xyz 소환 | △ **하드코딩·미완성** | ✔ | ✔ (2회) | 점수 0, 아래 6절 참고 |
| Detonator 묘지 소재 장착 | △ | — | ✘ | |
| Detonator 파괴 대응 | △ 임시 정책 | — | ✔ (소재 제거·대상 선택 로그) | 파괴 대체는 미완성 |
| Duo Drive Xyz 소환 | △ **하드코딩·미완성** | ✔ | ✘ (선택·소환 없음) | 후보는 생성되었으나 선택되지 않음 |
| Duo Drive 소재 장착·2장 서치 | △ | — | ✘ | |
| Star 발동효과, Plugin, Cross, Plasma Hole | 구현 여부 **미확인** | | | 현재 `RyzealExecutor.cs` 기준으로 재점검 필요 |

"구현되었다"와 "검증되었다"는 다르다. 전체 카드 효과의 정상 동작이 검증된 상태가 아니다.

---

## 6. 로그에서 재확인한 사실 (잠정: 2.3절 참고)

`ryzeal-mainphase.jsonl`의 `mainPhaseDecision` 8건을 다시 읽어 정리했다. (**확인**)

| 판단 | 정책 후보 (BaselineScore) | 선택 |
|---|---|---|
| 1 | Ext 룰 특수소환 (75) | Ext |
| 2 | Sword 룰 특수소환 (80) | Sword |
| 3 | Ice 일반소환 (80), Ice 룰 특수소환 (80), Detonator (0), Duo Drive (0) | Ice 일반소환 (동률→라벨순) |
| 4 | Detonator (0), Duo Drive (0) | Detonator, 소재 Ice+Ext (동률→라벨순) |
| 5 | Detonator (0), Duo Drive (0) | Detonator, 소재 Sword+Node (동률→라벨순) |
| 6 | 없음 | 레거시 폴백 |
| 7 | Ice 룰 특수소환 (60) | Ice 룰 특수소환 |
| 8 | 없음 | 레거시 폴백 |

여기서 드러난 판단 문제:

1. **Detonator와 Duo Drive의 점수가 모두 0.** 점수는 `GetPowerOrZero(powers, id)`로 읽는다(코드 확인). 가치표에 항목이 없거나 0으로 평가되어 0이 나온 것이며, 리터럴 `0`이 아니다. 두 후보의 전략적 차이가 전혀 반영되지 않고 라벨 순서가 승부를 결정한다.
2. **Detonator를 연속 2회 소환**(판단 4, 5). 두 번째 판단에서 Duo Drive로 후속을 확보하는 선택과 비교하지 않았다.
3. **Ice 일반소환과 룰 특수소환이 같은 점수**(판단 3). 서로 다른 자원을 쓰는 행동인데 구분하지 않는다.
4. **Detonator 파괴 대상 가치 평가 부족.** 로그에서는 Bonfire가 파괴 대상으로 선택되었다. Bonfire의 효과가 이미 해결되었는지는 현재 로그만으로 **확정할 수 없다.** 따라서 "이미 해결된 카드를 무의미하게 파괴했다"고 단정하지 않는다. 현재 AI에 "파괴가 상대 행동을 실제로 방해하는지"를 평가하는 로직이 없다는 것까지가 확인된 범위다.

---

## 7. 프로그래밍 원칙

1. **합법성은 엔진 우선.** 후보는 `MainPhase`의 리스트에서 만든다. 로컬 검사(`Can...`)는 전략 필터와 선택 보조 역할이며 규칙 전체를 재구현하지 않는다.
2. **가능 여부와 실행 분리.** `Can...()`은 선택 예약·사용 상태 변경을 피하고, `Commit...()`이 실제 `AI.Select*` 호출과 추적 상태 변경을 한다. 단, 일부 `Can...()`은 Executor의 가변 필드(`Card`)에 의존하므로 완전한 순수 함수는 아니다. 이 경로에서는 후보 확인 전에 `SetCard(...)`로 컨텍스트를 맞춰야 한다.
3. **효과는 카드 ID만으로 구분하지 않는다.** `카드 ID + ActivationDescription`을 함께 쓴다 (예: Duo Drive의 소재 장착 vs 서치).
4. **후속 선택은 예약 또는 콜백.** 효과 해결 중에 후보가 정해지는 경우 `OnSelectCard()`에서 엔진이 제시한 실제 후보를 고르고, 가능하면 카드 ID가 아니라 `ClientCard` 인스턴스를 쓴다 (동일 ID가 여러 위치에 있을 때 오선택 방지).
5. **HOPT 보조 추적.** 현재 Ryzeal 구현에서는 일부 룰 특수소환과 발동형 효과의 사용 여부를 보조적으로 분리 추적한다 (`_specialSummonEffectUsedThisTurn`, `_activateEffectUsedThisTurn`). 실제 규칙은 카드명 기준 1턴 1회, 효과별 1턴 1회, OATH, "발동 1회"와 "사용 1회"의 차이 등 다양하다. 이 추적은 엔진의 판정을 대체하지 않는다.
6. **임시 정책은 결정론적으로.** 재현과 디버깅을 위해 점수 정렬, 명시적 동률 규칙, 카드 ID 순서 등을 쓴다. 단 4절에서 보듯 **동률 규칙이 전략을 대신하게 두지 않는 것**이 개선 과제다.
7. **관측 구분을 유지한다.**

```text
엔진이 합법이라고 했다 (legalActions)
   → 우리 정책이 후보로 만들었다 (policyCandidates)
   → 정책이 선택했다 (selected)
   → Commit이 성공했다 (selectionResult)
   → 엔진이 실제로 효과를 해결했다            ← 아직 기록하지 않음
   → 그 결과 상태가 어떻게 변했다             ← 아직 기록하지 않음
```

현재는 앞의 4단계까지 관측 가능하고, 뒤의 2단계는 부족하다. "Detonator를 잘못 골랐다"와 "골랐는데 후속 처리가 잘못됐다"를 구분하려면 이 두 단계가 필요하다.

---

## 8. 로그

| 이벤트 | 내용 |
|---|---|
| `mainPhaseDecision` | 상태, 합법 행동, 정책 후보, 선택, 폴백 사유 |
| `legacyMainPhaseSelection` | 기존 Executor가 반환한 행동 |
| `cardSelection` | 구현된 소재·대상 선택의 후보와 선택 결과 |

- 식별자: `duelId`, `decisionId`, `timestampUtc`. 후보 판단과 레거시 선택은 같은 `decisionId`로 연결된다. `cardSelection`은 아직 체인 단위 연결이 없어 `decisionId: null`이다.
- 현재 `RyzealExecutor.cs`는 로거를 고정 경로 `logs/ryzeal-mainphase.jsonl`로 생성한다. (**확인**) 듀얼별 파일 분리 패치(`logs/yyyyMMdd_HHmmss_fff_.jsonl`)는 별도로 제공되었으나 **적용 여부는 확인하지 못했다.**
- 한계: 효과 해결 성공·행동 후 상태·승패 미기록 / 개별 카드 인스턴스·표시 형식·소재 정보 부족 / 상대 공개 여부 필터링 없음 / 후보별 Commit 실패 사유 없음 / 체인 순서 복원 불가.

---

## 9. 파일 맵

| 파일 | 역할 |
|---|---|
| `RyzealExecutor.cs` | 후보 생성, 선택 정책, 카드 실행, 후속 선택 (약 1,350줄) |
| `ActionCandidate.cs` | 후보 데이터 구조 |
| `CardValueEngine.cs` | 현재 상태 기준 임시 카드 가치 평가 |
| `GameStateAdapter.cs` | WindBot 상태 → 평가 엔진용 상태 변환 |
| `RyzealDeckProfile.cs` | 덱 카드별 매수 정의 |
| `DuelDecisionLogger.cs` | JSONL 기록 |
| `IMainPhaseDecisionObserver.cs` | 레거시 선택·듀얼 시작 관측 연결 |
| `GameAI.cs` (수정분) | 후보 훅 호출, 레거시 반환 행동 기록 |
| `Executor.cs` (수정분) | `TryGetMainPhaseAction()` 기본 훅 (기본값 false) |

---

## 10. 의도적으로 범위에서 제외한 것

- 상대 턴 체인 대응(`OnSelectChain`, `OnSelectEffectYn`)과 전투 페이즈
- **Nibiru**: Lua 기준 조건은 "상대가 이번 턴 5회 이상 소환"이며 퀵 이펙트다. 소환 횟수를 누적할 훅이 `Executor`에 없고 체인 대응 카드라 범위 밖이다.
- 스테이플 핸드트랩(`DefaultExecutor`의 `Default*` 경로 유지): 상대 카드 가치 기반 무효화 판단은 이번 계획에 포함하지 않는다.
- Python 연동(IPC), 학습 모델, 탐색
- 외부 Executor DLL 경로 (2.2절)
