# 체인 중 제거 (2026-11-11)

Detonator ①이 발동 중인 Infinite Impermanence를 파괴하는 문제에서 시작했다. 카드별 입력이나 학습 대신, 게임 규칙과 Lua 데이터로 "지금 이 카드를 없애면 상대가 무엇을 잃는가"를 하나의 값으로 계산한다. 이 값을 모든 제거 효과의 대상 선택과 발동/보류 판단에 함께 쓴다.

대상 카드: Ghost Ogre & Snow Rabbit, Ryzeal Detonator ①, Tornado Dragon, Ryzeal Plasma Hole ①. Evilswarm Exciton Knight는 발동 타이밍이 늦어 방해로 치지 않으므로 제외했다(InterruptModel에서 방해로 분류되지 않음).

## 1. 근거로 삼는 규칙

1. 발동된 효과는 그 카드를 없애도 처리된다.
2. 일반 마법, 속공 마법, 일반 함정은 처리 후 어차피 묘지로 간다(`ChainRemovalModel.LeavesAfterResolve`: 카드의 발동이면서 지속·필드·장착·펜듈럼이 아닌 마법·함정). 체인 중에 없애도 몸체 가치는 0이다.
3. 예외: 효과 처리 중에 발동한 카드 자신을 확인하는 효과는 그 카드를 없애면 멈춘다. Lua에서 `c:IsRelateToEffect(e)`(c = `e:GetHandler()`)로 읽고 `EffectEntry.SourceStop`에 저장한다.
   - `all`: `if not c:IsRelateToEffect(e) then return end`, 즉 효과 전체가 멈춤. 예: Duo Drive ②, Detonator ②(묘지에서 소재 붙이기), Mereologic Aggregator ②.
   - `part`: 일부 분기만 확인. 예: Impermanence의 같은 세로열 무효.

## 2. 제거 범위 (`EffectEntry.Reach`, Lua에서 읽음)

- 대상 함수의 `chkc` 조건에서 읽는다: `IsFaceup`, `IsSpellTrap`, `IsMonster`, `IsControler(1-tp)`.
- CHAINING 응답이면서 처리에서 `Duel.Destroy(eg …)`를 하면 "발동한 그 카드"만 칠 수 있다(Ghost Ogre). 이 경우 그 효과에 응답할 수 있는지도 `CanHit`으로 확인한다. Ghost Ogre는 카드의 발동에는 응답할 수 없다.
- 읽은 결과: Detonator ① 양쪽 필드 아무 카드, Plasma Hole ① 상대 앞면 카드, Tornado Dragon 마법·함정.

## 3. 지금 없애면 잃는 값 (`ChainRemovalModel.WorthNow`)

| 대상 | 값 |
|---|---|
| 뒷면 카드 | `removal.facedown` (20) |
| 체인 위에서 발동 중인 일반 마법·함정 | 몸체 0 |
| 필드에서 쓰는 효과(기동·즉시·방해)가 있는 카드 | 이번 턴 남은 효과(`OpponentLossModel.TurnResidual`) + `removal.body` (10) |
| 그런 효과가 없는 카드(지속 효과뿐, 일반 몬스터) | 카드 가치(planner Worth) |
| (추가) 체인 위 효과가 카드를 필요로 함 | `all`이면 그 효과의 체인 가치 전체, `part`이면 × `removal.partial_stop` (0.25) |

- 남은 효과는 상대 턴이면 기동·즉시 효과와 방해를, 우리 턴이면 상대가 들고 있는 방해만 센다.
- 이미 쓴 1턴 1번 효과와 소재가 모자라 쓸 수 없는 효과는 빠진다. 그래서 효과를 쓰고 소재가 0장인 Detonator는 10이다. 아직 쓰지 않은 Detonator는 73이다.

## 4. 사용처

- **대상 선택**(Destroy / Remove / ToGrave / RtoHand / ToDeck 힌트, 상대 필드 카드): `−(50 + WorthNow)`. 여러 장을 고르는 효과(Plasma Hole)도 같은 순서를 쓴다.
- **상대 체인에 응답**(Detonator ①, Ghost Ogre, Plasma Hole ①, Tornado Dragon): 추정값은 `얻는 값 − 아껴 둘 값`이고, 패스는 0이다.
  - 얻는 값: 칠 수 있는 카드 중 상위 N장의 WorthNow 합. N은 Plasma Hole이면 우리 Ryzeal 엑시즈 수, 나머지는 1.
  - 아껴 둘 값: 방해 가치(`InterruptModel.Value`) × `removal.hold` (1.0).
  - 기존 `HoldReason`에서 Plasma Hole에 쓰던 `int.partial_hold` 보류 규칙은 이 비교로 대체했다.
- **상대가 아무것도 안 한 창**(프리 체인 제거: Plasma Hole ①, Tornado Dragon): 추정값은 `1 + 얻는 값 − 아껴 둘 값`이고, 패스는 1이다. 전에는 이 창에서 제거를 절대 먼저 쓰지 않았다. 이제 상대 스탠바이에 아직 효과를 쓰지 않은 Detonator가 있으면 Plasma Hole을 발동한다.
- **상대 모델**: Ghost Ogre처럼 효과는 처리되고 몸체만 없애는 응답(`HitKind.Disrupt`)의 상대 손실을 상수 `hit.disrupt_body` 대신 같은 WorthNow로 계산한다(`OpponentLossModel.DisruptBody`).

## 5. 시험 (`docs/experiments/chain_removal/run_dumps.py`)

덤프 인자:
- `AgentDbRemoval=true`
- `AgentDbOppField`: id, 엑시즈는 id:소재 수, 0은 뒷면
- `AgentDbOppChain=id:효과,…`: 오래된 것부터, 마지막이 응답 대상
- `AgentDbOppGrave`, `AgentDbOppUsed`, `AgentDbOppTurn`
- `AgentDbOurXyz`: 우리 Ryzeal 엑시즈 수

| 상황 | Ghost Ogre | Detonator ① | Plasma Hole ① | Tornado Dragon |
|---|---|---|---|---|
| 우리 턴, 상대 Impermanence 발동 중(상대 필드 Detonator 2소재, Duo Drive 2소재) | 응답 불가(카드의 발동) | 발동 → **Detonator** (60.1; Imperm은 8.8) | 발동 → Detonator, Duo Drive | 보류 (Imperm 8.8 < 15) |
| 상대 턴, Duo Drive ② 체인(묘지에 Ryzeal) | 발동 → Duo Drive 157.4 (② 정지 포함) | 발동 → Duo Drive | 발동 → Duo Drive, Detonator | 칠 카드 없음 |
| 상대 스탠바이, 미사용 Detonator + 뒷면 1장, 체인 없음 | 발동 불가 | 발동 불가 | **발동** → Detonator 73 | 발동 → 뒷면 20 |
| 상대 턴, Detonator ① 체인(소재 0장 남음) | 보류 (10) | 보류 | 보류 | 칠 카드 없음 |
| 상대 턴, Duo Drive ③ 서치 체인(② 사용함) | 보류 (서치는 처리됨, 몸체 10) | 보류 | 보류 | 칠 카드 없음 |

- 계획 회귀: ice 346.7, ice_vs_det 478.7, agg_target 263.5로 변화 없음(플래너는 수정하지 않음).
- 실전 확인은 아직 하지 않았다.

## 6. 남은 것

- 가중치 `removal.hold`, `removal.body`, `removal.facedown`, `removal.partial_stop`은 학습 대상이다. 지금 값은 초기값이다.
- 프리 체인 제거는 "이 창에서 쏠까"만 보고, "다음 창에서 더 좋은 대상이 생길 가능성"은 아직 보지 않는다.
- 플래너(우리 턴 탐색)는 여전히 상대 응답을 가정하지 않는다(INTERRUPTION_RISK.md).
