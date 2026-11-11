# 체인 중 제거 (2026-11-11)

Detonator ①이 발동 중인 Infinite Impermanence를 파괴하는 문제에서 시작했다. 카드별 입력이나 학습 대신, 게임 규칙과 Lua 데이터로 "지금 이 카드를 없애면 상대가 무엇을 잃는가"를 하나의 값으로 계산한다. 이 값을 모든 제거 효과의 대상 선택과 발동/보류 판단에 함께 쓴다.

대상 카드: Ghost Ogre & Snow Rabbit, Ryzeal Detonator ①, Tornado Dragon, Ryzeal Plasma Hole ①. Evilswarm Exciton Knight는 발동 타이밍이 늦어 방해로 치지 않으므로 제외했다(InterruptModel에서 방해로 분류되지 않음).

## 1. 근거로 삼는 규칙

1. 발동된 효과는 그 카드를 없애도 처리된다.
2. 일반 마법, 속공 마법, 일반 함정은 처리 후 어차피 필드를 벗어난다(`ChainRemovalModel.LeavesAfterResolve`: 카드의 발동이면서 지속·필드·장착·펜듈럼이 아닌 마법·함정). 그래서 발동 중인 이런 카드는 제거 대상에서 뺀다(`LeavesNow`, 2026-11-11 사용자 결정). 발동 시 처리가 있는 지속·필드·장착 마법·함정(예: 발동 시 서치하는 필드 마법)은 체인 중에 없애도 발동 처리는 그대로 되지만, 카드가 필드에 남아 계속 작동하므로 제거 대상에 넣고 값을 높게 본다(3절).
3. 예외: 효과 처리 중에 발동한 카드 자신을 확인하는 효과는 그 카드를 없애면 멈춘다. Lua에서 `c:IsRelateToEffect(e)`(c = `e:GetHandler()`)로 읽고 `EffectEntry.SourceStop`에 저장한다.
   - `all`: `if not c:IsRelateToEffect(e) then return end`, 즉 효과 전체가 멈춤. 예: Duo Drive ②, Detonator ②(묘지에서 소재 붙이기), Mereologic Aggregator ②.
   - `part`: 일부 분기만 확인. 예: Impermanence의 같은 세로열 무효.

## 2. 제거 범위 (`EffectEntry.Reach`, Lua에서 읽음)

- 대상 함수의 `chkc` 조건에서 읽는다: `IsFaceup`, `IsSpellTrap`, `IsMonster`, `IsControler(1-tp)`.
- CHAINING 응답이면서 처리에서 `Duel.Destroy(eg …)`를 하면 "발동한 그 카드"만 칠 수 있다(Ghost Ogre). 이 경우 그 효과에 응답할 수 있는지도 `CanHit`으로 확인한다. Ghost Ogre는 카드의 발동에는 응답할 수 없다.
- 읽은 결과: Detonator ① 양쪽 필드 아무 카드, Plasma Hole ① 상대 앞면 카드, Tornado Dragon 마법·함정.
- `Reach.Targets`(`EFFECT_FLAG_CARD_TARGET`)와 `Reach.Destroys`(`CATEGORY_DESTROY`)도 읽는다. 아래 보호와 비교하는 데 쓴다. Ghost Ogre는 대상을 지정하지 않는다.

### 보호 (2026-11-11 실전 192013 판 패배 후 추가)

상대 필드의 보호가 제거를 막으면 그 카드는 후보에서 뺀다(`ChainRemovalModel.Shielded`, 결과의 "보호로 못 침").
- `EffectEntry.Shield`: `EFFECT_CANNOT_BE_EFFECT_TARGET`(대상 지정 제거를 막음), `EFFECT_INDESTRUCTABLE_EFFECT`(파괴를 막음).
  - 단일(SINGLE)은 그 카드 자신을 보호한다.
  - 필드(FIELD)에 대상 함수가 `c~=e:GetHandler()`이면 같은 쪽 필드의 다른 카드를 모두 보호한다. 예: Number F0 ④는 상대가 F0 외의 필드 카드를 대상으로 지정하지 못하게 한다.
  - 조건이 붙은 보호와, 읽지 못한 필터(예: Traptrix Rafflesia의 "Traptrix" 몬스터만)는 반영하지 않고 카드 메모에 남긴다.
- `EffectEntry.GuardsSelf`: 유발 즉시 효과 중 처리에서 자기 카드에 `EFFECT_INDESTRUCTABLE_EFFECT`를 거는 것(F0 ⑤). 그 카드가 아직 이 효과를 쓸 수 있으면 파괴로 쳐도 응답으로 피한다. 쓸 수 있다는 것은 1턴 1번이 남았고, 소재가 충분하고, `CanHit`으로 우리 효과에 응답할 수 있다는 뜻이다. 이때 값은 × `removal.self_guard` (0.1)이다(`WorthAgainst`). 대상 선택(파괴 힌트 502)에도 같이 쓴다.

## 3. 지금 없애면 잃는 값 (`ChainRemovalModel.WorthNow`)

| 대상 | 값 |
|---|---|
| 뒷면 카드 | `removal.facedown` (20) |
| 체인 위에서 발동 중인 일반·속공 마법, 일반 함정 | 0, 제거 대상 아님 |
| 필드에서 쓰는 효과(기동·즉시·방해)가 있는 카드 | 이번 턴 남은 효과(`OpponentLossModel.TurnResidual`) + `removal.body` (10) |
| 필드에 남는 마법·함정(지속·필드·장착)으로 위 효과가 있는 카드 | 위 값과 카드 가치(planner Worth) 중 큰 값: 다음 턴에도 계속 작동 |
| 그런 효과가 없는 몬스터(서치를 마친 메인 덱 Ryzeal 등) | `removal.body` + 레벨이 있으면 `removal.material` (15): 몸체와 엑시즈 소재. 패에서의 카드 가치(50~60)는 쓰지 않음 |
| 그런 효과가 없는 마법·함정(지속 효과뿐) | 카드 가치(planner Worth) |
| (추가) 체인 위 효과가 카드를 필요로 함 | `all`이면 그 효과의 체인 가치 전체, `part`이면 × `removal.partial_stop` (0.25) |

- 남은 효과는 상대 턴이면 기동·즉시 효과와 방해를, 우리 턴이면 상대가 들고 있는 방해만 센다.
- 이미 쓴 1턴 1번 효과와 소재가 모자라 쓸 수 없는 효과는 빠진다. 그래서 효과를 쓰고 소재가 0장인 Detonator는 10이다. 아직 쓰지 않은 Detonator는 73이다.

## 4. 사용처

- **대상 선택**(Destroy / Remove / ToGrave / RtoHand / ToDeck 힌트, 상대 필드 카드): `−(50 + WorthNow)`. 여러 장을 고르는 효과(Plasma Hole)도 같은 순서를 쓴다.
- **상대 체인에 응답**(Detonator ①, Ghost Ogre, Plasma Hole ①, Tornado Dragon): 추정값은 `얻는 값 − 아껴 둘 값`이고, 패스는 0이다.
  - 얻는 값: 칠 수 있는 카드 중 상위 N장의 WorthNow 합. N은 Plasma Hole이면 우리 Ryzeal 엑시즈 수, 나머지는 1.
  - 아껴 둘 값: 방해 가치(`InterruptModel.Value`) × `removal.hold` (1.0).
  - 소재를 떼어 쓰는 제거(Detonator ①)는 남은 사용 횟수가 적을수록 아껴 둘 값이 커진다. 남은 횟수는 이번 턴 우리 필드의 같은 카드 소재 ÷ 떼는 수이고, 1턴 1번이 있으면 그 수로 제한한다. 아껴 둘 값에 `max(1, removal.reserve / 남은 횟수)`를 곱한다(`removal.reserve` = 3). 3회 남으면 20.1, 1회 남으면 60.3이다. 마지막 소재는 Ext 몸체(25)에는 쓰지 않고 Duo Drive ②(157.4)에는 쓴다.
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
- `AgentDbOurDetachUses`: 소재를 떼어 쓰는 제거의 이번 턴 남은 사용 횟수

| 상황 | Ghost Ogre | Detonator ① | Plasma Hole ① | Tornado Dragon |
|---|---|---|---|---|
| 우리 턴, 상대 Impermanence 발동 중(상대 필드 Detonator 2소재, Duo Drive 2소재) | 응답 불가(카드의 발동) | 발동 → **Detonator** (60.1; Imperm은 대상 아님) | 발동 → Detonator, Duo Drive | 칠 카드 없음 (Imperm 제외) |
| 우리 턴, 상대 Ryzeal Cross 발동 중(필드 마법) + Detonator 2소재 | 응답 불가(카드의 발동) | 발동 → Detonator 60.1 | 발동 → Detonator, Cross 53 | 발동 → Cross 53 |
| 상대 턴, Cross 앞면 + 미사용 Detonator, 체인 없음 | 발동 불가 | 발동 불가 | 발동 → Detonator 73 | 발동 → Cross 53 |
| 상대 턴, Duo Drive ② 체인(묘지에 Ryzeal) | 발동 → Duo Drive 157.4 (② 정지 포함) | 발동 → Duo Drive | 발동 → Duo Drive, Detonator | 칠 카드 없음 |
| 상대 스탠바이, 미사용 Detonator + 뒷면 1장, 체인 없음 | 발동 불가 | 발동 불가 | **발동** → Detonator 73 | 발동 → 뒷면 20 |
| 상대 턴, Detonator ① 체인(소재 0장 남음) | 보류 (10) | 보류 | 보류 | 칠 카드 없음 |
| 상대 턴, Duo Drive ③ 서치 체인(② 사용함) | 보류 (서치는 처리됨, 몸체 10) | 보류 | 보류 | 칠 카드 없음 |
| 상대 턴, F0 2소재 + Duo Drive ② 체인 (실전 192013 3턴) | 발동 → Duo Drive 157.4 (대상 지정 아님) | **보류**: Duo Drive는 F0 ④로 대상 불가, F0는 ⑤로 피함 (40.2 × 0.1 = 4) | **보류** (같은 이유) | 칠 카드 없음 |
| 상대 턴, Ext ② 서치 체인, Detonator 남은 사용 3회 | 보류 (25 < 35.1) | 발동 → Ext 25 (아껴 둘 값 20.1) | 보류 | 칠 카드 없음 |
| 같은 상황, Detonator 남은 사용 1회 | 보류 | **보류** (아껴 둘 값 60.3) | 보류 | 칠 카드 없음 |
| 상대 턴, Duo Drive ② 체인, Detonator 남은 사용 1회 | 발동 | 발동 → Duo Drive 157.4 (> 60.3) | 발동 | 칠 카드 없음 |

- 계획 회귀: ice 346.7, ice_vs_det 478.7, agg_target 263.5로 변화 없음(플래너는 수정하지 않음).
- 실전 확인은 아직 하지 않았다.

## 6. 남은 것

- 가중치 `removal.hold`, `removal.body`, `removal.facedown`, `removal.partial_stop`, `removal.material`, `removal.reserve`, `removal.self_guard`는 학습 대상이다. 지금 값은 초기값이다.
- `removal.reserve`는 "이번 턴 더 큰 대상이 나올 가능성"을 상수로 대신한다. 상대 덱에 남은 전개 카드로 추정하는 것이 다음 단계다.
- 가치를 매기지 못한 우리 방해(예: Plasma Hole ② 상대 턴 엑시즈 소환, 값 0)는 −0.5로 두어 패스를 우선한다. 전에는 패스 0과 동점이라 무작위였다. 소환할 몬스터의 방해 가치 − 쓰는 소재로 값을 매기는 것은 남은 일이다.
- 제거 응답 옵션의 "상대 손실 N" 표시는 응답한 체인 카드 기준이라 고른 대상과 달랐다. 그래서 뺐고, "대상 …" 줄이 실제 값이다.
- 프리 체인 제거는 "이 창에서 쏠까"만 보고, "다음 창에서 더 좋은 대상이 생길 가능성"은 아직 보지 않는다.
- 플래너(우리 턴 탐색)는 여전히 상대 응답을 가정하지 않는다(INTERRUPTION_RISK.md).
