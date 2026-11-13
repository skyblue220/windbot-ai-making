# Ryzeal AI — 프로젝트 현황판

- 갱신: 2026-11-13 (브랜치 `new-agent`, 마지막 코드 커밋 `b13ff0c`)
- 이 문서는 **현황판**이다. 지금 무엇이 되어 있고, 무엇을 하는 중이며, 자세한 내용이 어느 문서에 있는지만 적는다. 세부 근거와 수치는 각 문서에 있다.
- 이전판(2026-10-14, 기존 Executor 기준): [PROJECT_STATUS_2026-10-14.md](PROJECT_STATUS_2026-10-14.md). 지금 구조와 맞지 않으므로 기록으로만 둔다.

---

## 1. 목표와 지켜야 할 규칙

- **최종 목표:** 카드 데이터(cdb + Lua)에서 출발해 대결 기록으로 스스로 배우는 AI. 손으로 맞춘 실행기를 만드는 것이 아니다.
- **연구 질문:** 같은 카드·같은 행동의 가치가 상태에 따라 달라진다는 것을 AI가 배울 수 있는가 ([PROJECT_BASELINE.md](PROJECT_BASELINE.md)).
- **규칙**
  - 새 봇은 기존 WindBot Executor(`RyzealExecutor`)와 구조가 다르다. 기존 Executor는 고치거나 검증하지 않고, 정답이나 비교 기준으로 쓰지 않는다.
  - 값은 cdb와 Lua에서 읽은 사실로 만든다. 기능 종류별 기본값(서치 30, 덱 특수 소환 40, 효과 무효 40 …)은 모든 테마가 공유하고 고정이다.
  - **2026-11-13부터 카드 id가 붙은 수치는 사람이 넣거나 고치지 않는다.** 카드(효과)별 보정값은 대결 기록에서 계산한 갱신으로만 바뀐다 ([VALUE_LEARNING_PLAN.md](VALUE_LEARNING_PLAN.md) 0절).

## 2. 지금 봇의 구조

```text
EDOPro ──TCP── GameBehavior (WindBot의 패킷 해석·응답 인코딩만 사용)
                   │  IGameDecider
                   ▼
              AgentBridge ── 게임별 카드 DB (덱 확정 시 cdb + Lua로 생성, 상대 덱 포함)
                   │           └ GameCardDb / LuaScript: 효과·연결·횟수 제한·응답 조건·방해 사실
                   │── DuelZoneView: 모든 판단이 읽는 하나의 전체 상태 (DECISION_STATE.md)
                   │── 값 모델들
                   │     TurnPlanner      우리 메인 페이즈: 한 턴 계획 탐색 (흐름 + 턴 종료 방해 + 후속 + 전투)
                   │     InterruptModel   방해 1회 값 = (종류 기본값 + 보정) × 사용 기회 × 계수 + 가산
                   │     ValueModel       패 카드 가치(유한 등비 합), 다음 턴 후속
                   │     ChainRemoval / OpponentLoss / BaitModel / GravePayoff / BattleMath
                   ▼
              ValuePolicy (선택지마다 추정값 → 최대 선택, 탐색률 explore)
                   │
                   └─ AgentLog: logs/agent/agent_<시각>_<duelId>.jsonl (+ _state.md, _carddb.md)
```

- 덱 이름이 `Agent:`로 시작하면 새 봇(`AgentBridge`), 아니면 기존 `GameAI`(`LegacyDecider`)가 쓰인다.
- `bots.json` 항목: "Ryzeal (New Agent)" = `Agent:AI_Ryzeal`, "Ryzeal (New Agent, vs Ryzeal)" = `Agent:AI_Ryzeal@AI_Ryzeal`(상대 덱 목록을 카드 DB에 넣음).

| 파일 | 역할 |
|---|---|
| `Agent/AgentBridge.cs` | 요청 수신, 카드 DB·모델 준비, 선택지 추정값과 항목(`terms`) 달기 |
| `Agent/DuelZoneView.cs` | 듀얼 상태 → 판단 입력(ZoneSnapshot) |
| `Agent/CardDbFactory.cs` | 카드 DB 생성, 가중치 읽기, `_state.md`·오프라인 덤프 출력 |
| `Agent/Core/DecisionModel.cs` | 요청·선택지·`ValueTerm` 공통 표현 |
| `Agent/Core/ValuePolicy.cs`, `RandomPolicy.cs` | 판단 정책 |
| `Agent/Core/AgentLog.cs` | JSONL 기록 (`header`, `weights`, `carddb`, `decision`, `message`, `result`) |
| `Agent/Knowledge/*.cs` | 카드 DB(`GameCardDb`, `LuaScript`)와 값 모델들 |
| `ProjectIgnis/WindBot/agent_values.json` | 공유 기본값·계수 (사람이 검토하는 값) |

## 3. 진행 상황

| 영역 | 상태 | 자세한 문서 |
|---|---|---|
| 새 봇 골격 (P0): 모든 선택 요청에 기존 Executor 없이 응답 | 완료. 2026-11-13 실전 4판 모두 `Retry` 0회, 그중 2판은 승패까지 기록 | [TODO.md](TODO.md) P0, [WINDBOT_DATA_FLOW.md](WINDBOT_DATA_FLOW.md) |
| 게임별 카드 DB (cdb + Lua) | 완료, 계속 확장 중 (응답 조건, 방해 사실, 묘지 이득, 드로우 감시) | [CARDDB_SAMPLE_AI_Ryzeal.md](CARDDB_SAMPLE_AI_Ryzeal.md) |
| 판단 입력 하나로 통일 | 완료 (2026-11-13) | [DECISION_STATE.md](DECISION_STATE.md) |
| 값 모델: 패 가치, 턴 계획, 턴 종료 방해 | 판단에 사용 중 | [VALUE_MODEL.md](VALUE_MODEL.md) |
| 체인 중 제거 (지금 칠지, 아낄지) | 판단에 사용 중. 아끼는 방식 기본값은 `reserve` | [CHAIN_REMOVAL.md](CHAIN_REMOVAL.md) |
| 묘지 이득 (Twins, Aggregator)을 여러 턴에 걸쳐 | 부분 완료 | [GRAVE_PAYOFF.md](GRAVE_PAYOFF.md) |
| 드로우 계열 패트랩 (Maxx "C", Mulcharmy) | 오프라인 확인, 실전 미확인 | [DRAW_HANDTRAPS.md](DRAW_HANDTRAPS.md) |
| 상대 1회 방해 위험 항 | 설계·오프라인 검증만, 구현 보류 (학습 단계에서 다시) | [INTERRUPTION_RISK.md](INTERRUPTION_RISK.md) |
| **카드 수치를 대결 결과로 조정 (학습)** | **진행 중: 0·1단계 완료, 다음 2단계** | [VALUE_LEARNING_PLAN.md](VALUE_LEARNING_PLAN.md) |

## 4. 지금 하는 일: 대결 결과로 배우기

[VALUE_LEARNING_PLAN.md](VALUE_LEARNING_PLAN.md)의 단계와 상태:

| 단계 | 내용 | 상태 |
|---|---|---|
| 0 | 카드별 직접 입력 수치 31개 제거, Lua에서 읽은 사실로 방해 값 계산 | 완료 `2fca519` |
| 1 | 값 분해 기록: 로그의 선택지마다 `terms`, 항목 합 = 추정값 | 완료 `5e09682`. 첫 실전 4판 확인 후 보완 `b13ff0c` (식이 곱해지게, 계획 재현이 끊기지 않게) |
| 2 | 예측과 실제를 짝지은 결과 기록 (예: "N104 ① 예측 57, 사용 0") | **진행 중**: 1번 우리 방해(`outcome` 줄) 구현, 실전 확인 전. 남은 것: 전개가 막힌 단계, 드로우 감시, 전투 |
| 3a | 상황 보정 (지금 하는 수동 대전 로그로 시작) | 대기 |
| 4 | 봇끼리 대량 대전과 새 보정값 평가 | 대기 |
| 3b | 결과 보정 (TD) | 대기 |

- 학습이 바꾸는 것은 `terms`의 `offset`(카드 효과별 보정값)뿐이다. 보정값 d가 값을 d × `chance` × `factor`만큼 움직인다는 것이 로그에 그대로 남는다.
- 계획이 아닌 결정(대상 고르기, 비용, 표시 형식 등)은 `rule` 항목이며 학습 대상이 아니다.

## 5. 확인 상태

- **실전으로 확인한 것:** 듀얼 진행·종료, 결정 로그 형식, 항목 합 = 추정값(2026-11-13: 1,812개, 고친 뒤 2,485개), 계획 재현 끊김 22개 → 2개.
- **오프라인으로만 확인한 것** (실전 미확인 표시가 남아 있는 항목): 드로우 패트랩 감점, 응답 보류, Aggregator ③ 자기 카드 무효 회피, Lightning Crash 일반 소환 잠금. 2단계 `outcome` 기록은 빌드만 했고 실전 확인 전이다. 목록은 [TODO.md](TODO.md)의 "실전 미확인" 표시.
- 오프라인 확인은 `AgentDbDump` 덤프로 한다. 재현 스크립트는 `docs/experiments/<주제>/run_dumps.py`에 있다 (`value_from_lua`, `decision_state`, `draw_handtraps`, `grave_payoff`, `chain_removal`, `interruption_risk`). 계획 탐색에 시간 예산이 있어 같은 상황도 실행마다 조금씩 다르므로, 스크립트는 하나씩 돌린다.

## 6. 열린 결정과 보류

- 제거를 아끼는 방식의 실전 기본값: 지금 `reserve`, `future`로 바꾸는 것을 권했으나 결정 전 ([CHAIN_REMOVAL.md](CHAIN_REMOVAL.md)).
- 공유 계수(`int.battle_factor` 등)도 학습 대상인지: 지금은 고정. 카드 보정값만으로 안 맞으면 다시 논의 ([VALUE_LEARNING_PLAN.md](VALUE_LEARNING_PLAN.md) 4절).
- 상대 1회 방해 위험 항: 학습 단계에서 다시 본다.
- 대량 대전 수단: 지금은 사람이 방을 열어 한 판에 1~1.5분. 자동 대전과 시작 패 고정(`dontShuffleDeck`, 시드)은 4단계에서.
- 로그의 효과 번호가 Lua 등록 순서라 카드 텍스트 ①②③과 다르다 (예: Detonator DB 효과 1 = 텍스트 ②).
- TY-PHON ①은 우리 Detonator ②도 막는다. 계획기가 TY-PHON을 내게 되면 반영해야 한다.

## 7. 빌드·배포·실행

```bash
"/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" WindBot.csproj -p:Configuration=Release -p:TargetFrameworkVersion=v4.8 -v:q -nologo
```

```bash
cp bin/Release/WindBot.exe bin/Release/ExecutorBase.dll ../ProjectIgnis/WindBot/
```

- 실전 로그: `ProjectIgnis/logs/agent/` — 듀얼마다 `.jsonl`(모든 결정·메시지), `_state.md`(턴 끝 상태와 계획·항목), `_carddb.md`(그 게임의 카드 DB).
- 오프라인 덤프 (ProjectIgnis 폴더에서): `WindBot.exe AssetPath=./WindBot DbPaths=<base64 경로 목록> AgentDbDump=AI_Ryzeal AgentOpponentDeck=AI_Ryzeal AgentDbOut=<파일>`에 상황 인자(`AgentDbHand`, `AgentDbGrave`, `AgentDbMonsters=id:소재1/소재2`, `AgentDbOppWatch`, `AgentDbOppHand` …)를 더한다. 예시는 각 `run_dumps.py`.

## 8. 문서 지도

| 문서 | 내용 | 상태 |
|---|---|---|
| [PROJECT_STATUS.md](PROJECT_STATUS.md) | 이 현황판 | 현재 |
| [TODO.md](TODO.md) | 전체 작업 목록 (P0~P3, 완료 기록) | 현재 |
| [VALUE_LEARNING_PLAN.md](VALUE_LEARNING_PLAN.md) | 대결 결과로 카드 수치 조정: 계획과 단계별 결과 | 현재, 지금 작업 |
| [VALUE_MODEL.md](VALUE_MODEL.md) | 값 모델 규칙 (기능 기본값, 턴 계획, 방해 값) | 현재 |
| [DECISION_STATE.md](DECISION_STATE.md) | 판단 입력 (하나의 전체 상태) | 현재 |
| [CHAIN_REMOVAL.md](CHAIN_REMOVAL.md) | 체인 중 제거 | 현재 |
| [GRAVE_PAYOFF.md](GRAVE_PAYOFF.md) | 묘지 이득 | 현재 |
| [DRAW_HANDTRAPS.md](DRAW_HANDTRAPS.md) | 드로우 계열 패트랩 | 현재 |
| [INTERRUPTION_RISK.md](INTERRUPTION_RISK.md) | 상대 1회 방해 위험 항 (보류) | 현재 |
| [WINDBOT_DATA_FLOW.md](WINDBOT_DATA_FLOW.md) | EDOPro ↔ WindBot 데이터 흐름 | 참고 |
| [PROJECT_BASELINE.md](PROJECT_BASELINE.md) | 방향과 연구 질문 (2026-11-06) | 방향은 유효, 세부 구조 설명은 일부 오래됨 |
| [CARDDB_SAMPLE_AI_Ryzeal.md](CARDDB_SAMPLE_AI_Ryzeal.md) | 카드 DB 출력 예시 | 참고 (예전 출력) |
| [TODO_2026-10-14.md](TODO_2026-10-14.md), [PROJECT_STATUS_2026-10-14.md](PROJECT_STATUS_2026-10-14.md) | 기존 Executor 시기의 기록 | 보관 |

## 9. 최근 흐름 (2026-11-07 ~ 11-13)

- 11-07~09: 값 모델을 판단에 연결, 턴 계획 탐색(엑시즈 위 엑시즈 포함), 전투 판단, 응답 조건 읽기, 응답 보류.
- 11-10~11: 방해 사용/아끼기 1단계(상대 손실), 체인 중 제거, 묘지 이득, 드로우 패트랩, 병렬 계획 탐색과 힌트.
- 11-12~13: 엑스트라 덱 코스트 순서, 판단 입력 통일, 가위바위보 오류 수정, 학습 계획 수립, 0단계(`2fca519`), 1단계(`5e09682`, `b13ff0c`).
