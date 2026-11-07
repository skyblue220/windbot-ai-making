# EDOPro ↔ WindBot 데이터 흐름

- 작성 시점: 2026-11-06
- 목적: 기존 봇이 ProjectIgnis(EDOPro)와 어떤 방식으로 데이터를 주고받는지 확인하고, 새 봇 구조의 기준으로 삼는다.
- 근거: `WindBot-Git` 소스(`ryzeal-development`), `edopro-ai-test`의 EDOPro 소스(`gframe/windbot.cpp`)
- 표기: **(확인)** 소스로 확인 / **(미확인)** 근거는 있으나 확정 못 함

---

## 1. 실행과 연결

- **실행**: EDOPro가 `./WindBot/WindBot.exe`를 별도 프로세스로 띄운다 (확인, `gframe/windbot.cpp`).
  - 인자: `HostInfo`, `Deck`, `Port`, `Version`, `name="[AI] …"`, `Chat`, `Hand`, `DbPaths`(cards.cdb 경로 목록, base64 JSON), `AssetPath=./WindBot`
  - 작업 폴더는 ProjectIgnis다. 그래서 봇 로그가 `ProjectIgnis\logs`에 생긴다.
- **연결**: 봇은 EDOPro가 연 방에 TCP로 접속하는 일반 클라이언트다 (확인, `Game/GameClient.cs`). 사람 플레이어와 같은 자격이며 엔진에 직접 접근하지 않는다.
  - 접속 후 `CTOS.PlayerInfo`(이름)를 보내고, `CTOS.JoinGame`으로 기존 방에 들어가거나 `CTOS.CreateGame`으로 방을 만든다.
  - 방을 만들 때의 설정(`CreateGameInfo`, `WindBotInfo.cs`)에 `dontShuffleDeck`, `startingLP`, `startingDrawCount`, `duelFlags` 등이 있다.
- **카드 정보**: 시작할 때 `DbPaths`로 받은 cards.cdb를 직접 읽는다(`NamedCardsManager.LoadDatabase`, `Program.InitDatas`) (확인). Lua 스크립트는 읽지 않는다. 효과 처리는 전부 EDOPro 쪽 엔진(ocgcore)이 한다.
- **서버 모드**: `ServerMode=true`면 HTTP로 봇을 생성하는 모드가 있다(`Program.RunAsServer`) (확인). EDOPro의 기본 실행은 이 모드를 쓰지 않는다.

## 2. 메시지의 두 층

| 층 | 방향 | 내용 |
|---|---|---|
| 방 메시지 (STOC / CTOS) | EDOPro ↔ 봇 | 입장, 덱 제출(`UpdateDeck`), 준비(`HsReady`), 가위바위보(`HandResult`), 선후공(`TpResult`), 시간 확인, 듀얼 종료, 재대결 |
| 게임 메시지 (`STOC.GameMsg` 안의 `GameMessage`) | 엔진 → 봇 | 듀얼 중 모든 이벤트와 선택 요청 |
| 응답 (`CTOS.Response`) | 봇 → 엔진 | 선택 요청에 대한 답 (정수 또는 바이트 배열) |

- 패킷은 `GameBehavior.OnPacket`에서 첫 바이트로 STOC 종류를 나누고, `GameMsg`이면 다음 바이트로 `GameMessage` 종류를 나눠 처리기를 부른다 (확인).

## 3. 게임 메시지의 종류

### 3.1 상태 알림 (응답 없음)

| 분류 | 메시지 |
|---|---|
| 듀얼 진행 | `Start`, `NewTurn`, `NewPhase`, `Win`, `Retry`, `ReloadField` |
| 카드 이동·정보 | `Draw`, `Move`, `Swap`, `PosChange`, `UpdateCard`, `UpdateData`, `ShuffleDeck`, `ShuffleHand`, `ShuffleExtra`, `ShuffleSetCard`, `SwapGraveDeck`, `TagSwap` |
| 체인 | `Chaining`, `ChainSolving`, `ChainNegated`, `ChainDisabled`, `ChainSolved`, `ChainEnd` |
| 소환 | `Summoning`, `Summoned`, `SpSummoning`, `SpSummoned`, `FlipSummoning`, `FlipSummoned` |
| 전투 | `Attack`, `Battle`, `AttackDisabled` |
| LP | `Damage`, `PayLpCost`, `Recover`, `LpUpdate` |
| 대상·장착 | `BecomeTarget`, `CardTarget`, `CancelTarget`, `Equip`, `Unequip` |
| 힌트 | `Hint` (선택 힌트, 상대 선언, 존 힌트, 효과 설명 추가·삭제 등) |

### 3.2 선택 요청 (응답 필요)

엔진은 봇이 응답할 때까지 기다린다. 각 요청에는 엔진이 계산한 합법 선택지가 들어 있다.

| 요청 | 의미 | 현재 응답 함수 (`GameAI`) |
|---|---|---|
| `SelectIdleCmd` | 메인 페이즈 행동 (소환·특수소환·발동·세트·표시형식 변경·페이즈 이동) | `OnSelectIdleCmd` |
| `SelectBattleCmd` | 배틀 페이즈 행동 | `OnSelectBattleCmd` |
| `SelectChain` | 체인에 발동할지, 무엇을 발동할지 | `OnSelectChain` |
| `SelectEffectYn` | 이 효과를 발동할지 | `OnSelectEffectYn` |
| `SelectYesNo` | 예/아니오 | `OnSelectYesNo` (배틀 리플레이는 `OnSelectBattleReplay`) |
| `SelectOption` | 효과 선택지 중 하나 | `OnSelectOption` |
| `SelectCard`, `SelectUnselect` | 카드 선택 (대상, 소재, 서치 등) | `OnSelectCard` |
| `SelectTribute` | 릴리스할 카드 | `OnSelectTribute` |
| `SelectSum` | 합계 조건 선택 (싱크로·의식 등) | `OnSelectSum` |
| `SelectCounter` | 카운터 제거 | `OnSelectCounter` |
| `SelectPlace`, `SelectDisfield` | 존 선택 | `OnSelectPlace` |
| `SelectPosition` | 표시 형식 | `OnSelectPosition` |
| `SortCard`, `SortChain` | 순서 정렬 | `OnCardSorting` (체인 정렬은 자동 응답) |
| `AnnounceCard`, `AnnounceAttrib`, `AnnounceRace`, `AnnounceNumber` | 선언 | `OnAnnounceCard` 등 |
| `RockPaperScissors` | 가위바위보 | `OnRockPaperScissors` |

## 4. 현재 봇 내부 구조와 교체 지점

```
EDOPro (엔진)
   ↕ TCP 패킷
YGOSharp.Network (패킷 송수신)
   ↓
GameBehavior (패킷 해석)
   ├─ 상태 알림 → Duel / ClientField 갱신 (봇이 보는 듀얼 상태)
   │              + GameAI의 알림 함수 호출 (OnNewTurn, OnChaining, OnMove …)
   └─ 선택 요청 → GameAI.OnSelectXxx() 호출 → 반환값을 Response로 인코딩해 전송
                      ↓
                 Executor (카드별 판단)
```

- **교체 지점은 `GameAI` 하나다.** `GameBehavior`는 선택 요청마다 `_ai.OnSelectXxx(...)`를 부르고 반환값을 응답으로 보낸다 (확인). 새 봇은 이 자리에 들어가면 패킷 처리를 건드리지 않고 판단 계층 전체를 대체할 수 있다.
- `GameAI`는 생성 시 `DecksManager.Instantiate`로 덱 이름에 맞는 Executor를 붙인다. 덱 파일(ydk)도 Executor의 `Deck` 속성으로 정해진다 (확인, `GameBehavior` 생성자). 새 봇은 이 연결도 따로 정해야 한다.

### 4.1 GameBehavior가 AI를 거치지 않고 처리하는 경우 (확인)

- `SelectChain`: 선택지가 없으면 -1, 강제 발동인데 선택지가 하나면 0을 바로 보낸다.
- `SelectEffectYn`: 해당 카드를 찾지 못하면 0(아니오)을 보낸다. 유발 효과의 설명 ID 0과 221을 -1로 바꿔 넘긴다.
- `SelectPosition`: 가능한 표시 형식이 하나면 바로 보낸다.
- `SortChain`: 항상 기본 순서(-1)로 응답한다.

새 봇에서는 이 처리를 그대로 쓸지, 새 봇이 판단할지 정해야 한다.

### 4.2 봇이 볼 수 있는 상태의 한계 (확인)

- 봇의 듀얼 상태는 상태 알림에서만 만들어진다.
- 상대 패와 덱, 자신의 덱은 카드 ID 0으로 보인다 (`ClientField.Deck`).
- 턴당 1번 사용 여부 같은 효과 제약 정보는 엔진이 보내지 않는다.
- 따라서 "엔진 이벤트 + 카드 지식 DB의 규칙으로 듀얼 상태를 갱신하는 계층"이 새 봇에 필요하다 (`PROJECT_BASELINE.md` 2장).

## 5. 새 봇 설계에 주는 의미

- **유지**: 실행 방식(EDOPro가 띄우는 프로세스), 네트워크(`YGOSharp.Network`), `GameBehavior`의 패킷 해석과 응답 인코딩.
- **새로 만듦**: `GameAI` 자리에 들어갈 판단 계층.
  - 3.2의 선택 요청을 카드별 코드 없이 같은 경로로 처리한다.
  - 3.1의 상태 알림으로 듀얼 상태를 갱신하고, 카드 지식 DB를 함께 사용한다.
  - 판단 코드는 "상태와 선택지를 받아 하나를 고른다"는 경계만 갖고, 패킷 형식에 묶지 않는다.
- **반복 가능한 시작 상태의 단서**: `CreateGameInfo.dontShuffleDeck`로 덱을 섞지 않는 방을 만들 수 있다 (확인). 실제로 시작 패가 고정되는지는 확인하지 않았다 (미확인).

## 6. 구현된 새 봇 골격 (브랜치 `new-agent`, 커밋 `97f19ae`)

```
GameBehavior (기존, 패킷 해석·응답 인코딩)
   ↓ IGameDecider
   ├─ LegacyDecider → GameAI → Executor          (기존 덱. 동작 변화 없음)
   └─ AgentBridge (Agent/AgentBridge.cs)          (덱 이름이 "Agent:"로 시작할 때)
         ↓ WindBot 타입 → 중립 표현
       Agent/Core (WindBot 타입을 모름)
         ├─ DecisionModel.cs: RequestKind(21종), RequestShape(5종), DecisionOption, DecisionRequest, Decision, StateSnapshot, IDecisionPolicy
         ├─ RandomPolicy.cs: 모든 형태에 합법 응답을 무작위로
         └─ AgentLog.cs: 듀얼별 JSONL 기록
```

- **실행**: `bots.json`의 `"deck": "Agent:AI_Ryzeal"` 항목("Ryzeal (New Agent)")으로 EDOPro에서 선택한다.
- **선택 요청 → 응답 형태**

| RequestShape | 요청 종류 |
|---|---|
| One | 가위바위보, 선후공, 메인 페이즈 행동, 배틀 페이즈 행동, 체인, 효과 발동 여부, 예/아니오, 배틀 리플레이, 효과 선택지, 존, 표시 형식, 카드 선언, 숫자 선언 |
| Many | 카드 선택, 선택/해제 선택, 릴리스, 속성 선언, 종족 선언 |
| Sum | 합계 선택 (싱크로·의식 등) |
| Counter | 카운터 제거 |
| Order | 카드 정렬 |

- **기록**: `logs\agent\agent_<시각>_<duelId>.jsonl` (EDOPro 실행 시 `ProjectIgnis\logs\agent\`).
- **아직 안 된 것**: 게임 메시지의 내용 전체(이동한 카드 등) 전달, `GameBehavior` 자동 응답 처리 결정.

## 7. 게임별 카드 DB (커밋 `6c8f974`)

```
GameBehavior 생성자: Deck.Load → IGameDecider.OnDeckLoaded(deck)        (가위바위보 전)
   → AgentBridge → CardDbFactory.Build(deck)
        ├─ cards.cdb (DbPaths로 받은 DB들): 스탯, 텍스트, str1~16
        ├─ script/official/c<id>.lua: 효과 등록부·함수·필터 (Agent/Knowledge/LuaScript.cs)
        └─ script/constant.lua, archetype_setcode_constants.lua: 상수 값
   → GameCardDb (Agent/Knowledge/GameCardDb.cs): 카드·효과·연결·상태

듀얼 중 갱신:
   Chaining(체인 정보의 카드·플레이어·설명 ID) → 발동 기록 + 횟수 소비
   ChainNegated → 발동 무효. "발동" 제약(OATH)이면 횟수를 돌려줌 (엔진 확인: Bonfire)
   ChainDisabled → 효과 무효 (횟수 소비 유지) / ChainSolved → 처리 완료
   새 봇이 고른 패의 SpSummon + SpSummoned → 룰 특수 소환 사용
   새 봇이 고른 Summon/SetMonster → 일반 소환 사용
   NewTurn → 이전 턴 DB 파일 저장 후 이번 턴 횟수 초기화
   상대 카드 → 처음 보일 때 추가
```

- 엔진의 효과 설명 ID는 `(카드 코드 << 20) | 문자열 번호`이고, 문자열 번호가 Lua의 `aux.Stringid(id, n)`과 cdb의 `str(n+1)`에 대응한다. 이것으로 선택지와 DB의 효과를 잇는다.
