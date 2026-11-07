# 게임 카드 DB

- 메인 덱 40장, 엑스트라 덱 15장, DB 카드 종류 37
- 턴마다 바뀌는 발동·사용 기록과 카드 위치는 같은 이름의 `_state.md`에 있다.

## Ash Blossom & Joyous Spring (14558127) ×2
- MONSTER+EFFECT+TUNER · FIRE · ZOMBIE · 레벨 3 · 0 / 1800
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 설명: Negate that effect
  - 분류: DISABLE
  - 횟수: 1턴 1번 (처리 시 소비), 키 14558127#0
  - 미해석: inline function: Cost.SelfDiscard
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Bonfire (85106525) ×1
- SPELL
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Search 1 Level 4 or lower Pyro monster
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 85106525#0
  - 효과: 덱에서 패에 넣기 [조건: IsLevelBelow(4) and IsRace(RACE_PYRO) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Crossout Designator (65681983) ×1
- SPELL+QUICKPLAY
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: REMOVE
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 65681983#0
  - 효과: 덱에서 제외 [조건: IsAbleToRemove()] → 조건에 맞는 아무 카드
  - 효과: 덱에서 제외 [조건: IsCode(ac) and IsAbleToRemove()] → 이 덱에서: (없음)
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ext Ryzeal (34022970) ×3
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 500 / 2000 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Ext Ryzeal")
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 34022970#0
  - 조건: 존재 조건 (엑스트라 덱에 있어야 함) [조건: IsType(TYPE_XYZ) and IsAbleToGraveAsCost()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
  - 코스트: 코스트: 엑스트라 덱에서 묘지로 [조건: IsType(TYPE_XYZ) and IsAbleToGraveAsCost()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS, SPSUMMON_SUCCESS
  - 설명: Add 1 FIRE Thunder monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (처리 시 소비), 키 34022970#1
  - 조건: 부재 조건 (몬스터 존에 없어야 함) [원문: s.thconfilter = s.thconfilter ; c:IsFaceup() and not (c:IsLevel(4) or c:IsRank(4))] → (미해석, 연결 계산 안 함)
  - 효과: 덱에서 패에 넣기 [조건: IsAttribute(ATTRIBUTE_FIRE) and IsRace(RACE_THUNDER) and IsAbleToHand()] → 이 덱에서: Sword Ryzeal, Node Ryzeal
  - 미해석: filter s.thconfilter = s.thconfilter ; c:IsFaceup() and not (c:IsLevel(4) or c:IsRank(4))
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Ghost Belle & Haunted Mansion (73642296) ×1
- MONSTER+EFFECT+TUNER · EARTH · ZOMBIE · 레벨 3 · 0 / 1800
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 분류: NEGATE
  - 횟수: 1턴 1번 (처리 시 소비), 키 73642296#0
  - 미해석: inline function: Cost.SelfDiscard
  - 미해석: inline function: function(e,tp,eg,ep,ev) Duel.NegateActivation(ev) end
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Ghost Ogre & Snow Rabbit (59438930) ×1
- MONSTER+EFFECT+TUNER · LIGHT · PSYCHIC · 레벨 3 · 0 / 1800
- 효과 1: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE|HAND
  - 분류: DESTROY
  - 횟수: 1턴 1번 (처리 시 소비), 키 59438930#0
  - 미해석: inline function: Cost.SelfToGrave
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Gravedigger's Trap Hole (31548215) ×1
- TRAP · 카드군 0x89
- 효과 1: 카드의 발동 / 계기: CHAINING
  - 분류: DISABLE, DAMAGE
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ice Ryzeal (8633261) ×3
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 1700 / 1000 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Ice Ryzeal")
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 8633261#0
  - 조건: 존재 조건 (패/필드에 있어야 함) [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
  - 코스트: 코스트: 패/필드에서 묘지로 [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS
  - 설명: Special Summon 1 "Ryzeal" monster from your Deck
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 8633261#1
  - 효과: 덱에서 특수 소환 [조건: IsSetCard(SET_RYZEAL) and not IsCode(id) and IsCanBeSpecialSummoned(e,0,tp,false,false)] → 이 덱에서: Star Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Infinite Impermanence (10045474) ×3
- TRAP
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: DISABLE
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsNegatableMonster()] → 상대 카드
- 효과 2 [설명 0]: 적용 효과 (SINGLE, TRAP_ACT_IN_HAND)
  - 설명: Activate using "Infinite Impermanence"
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Lightning Crash (89753095) ×3
- SPELL
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Add 1 Thunder monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 89753095#0
  - 제약: 이 효과로 패에 넣은 카드는 일반 소환할 수 없음
  - 효과: 덱에서 패에 넣기 [조건: IsRace(RACE_THUNDER) and IsAbleToHand()] → 이 덱에서: Sword Ryzeal, Node Ryzeal
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Maxx "C" (23434538) ×1
- MONSTER+EFFECT · EARTH · INSECT · 레벨 2 · 500 / 200
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Draw 1 card for eatch Special Summoned card on opponent's Field
  - 분류: DRAW
  - 횟수: 1턴 1번 (처리 시 소비), 키 23434538#0
  - 미해석: inline function: Cost.SelfToGrave
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Mulcharmy Fuwalos (42141493) ×3
- MONSTER+EFFECT · WIND · WINGEDBEAST · 레벨 4 · 100 / 600 · 카드군 0x1ac
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Apply the "●" effects for the rest of the turn
  - 분류: DRAW, TODECK
  - 미해석: inline function: function(e,tp) return Duel.GetFieldGroupCount(tp,LOCATION_ONFIELD,0)==0 end
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Mulcharmy Purulia (84192580) ×2
- MONSTER+EFFECT · WATER · AQUA · 레벨 4 · 100 / 600 · 카드군 0x1ac
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Apply the "●" effects for the rest of the turn
  - 분류: DRAW, TODECK
  - 미해석: inline function: function(e,tp) return Duel.GetFieldGroupCount(tp,LOCATION_ONFIELD,0)==0 end
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Nibiru, the Primal Being (27204311) ×1
- MONSTER+EFFECT · LIGHT · ROCK · 레벨 11 · 3000 / 600
- 효과 1: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 분류: RELEASE, SPECIAL_SUMMON, TOKEN
  - 횟수: 1턴 1번 (처리 시 소비), 키 27204311#0
  - 효과: 몬스터 존에서 릴리스 [원문: aux.FaceupFilter(Card.IsReleasableByEffect)] → (미해석, 연결 계산 안 함)
  - 미해석: filter aux.FaceupFilter(Card.IsReleasableByEffect)
- 효과 2: 지속 처리 (SPSUMMON_SUCCESS) / 계기: SPSUMMON_SUCCESS
- 효과 3: 지속 처리 (SUMMON_SUCCESS) / 계기: SUMMON_SUCCESS
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Node Ryzeal (72238166) ×1
- MONSTER+EFFECT · FIRE · THUNDER · 레벨 4 · 1400 / 1600 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Node Ryzeal")
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 72238166#0
  - 조건: 존재 조건 (몬스터 존/묘지에 있어야 함) [원문: aux.FaceupFilter(Card.IsType,TYPE_XYZ)] → (미해석, 연결 계산 안 함)
  - 미해석: filter aux.FaceupFilter(Card.IsType,TYPE_XYZ)
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Special Summon 1 "Ryzeal" monster from your GY
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 72238166#1
  - 코스트: 코스트: 패/필드에서 묘지로 [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
  - 효과: 묘지에서 특수 소환 [조건: IsSetCard(SET_RYZEAL) and IsCanBeSpecialSummoned(e,0,tp,false,false,POS_FACEUP_DEFENSE) and not IsCode(id)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 2: 덱에서 패에 넣기
  - Lightning Crash — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Pot of Prosperity (84211599) ×1
- SPELL
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 84211599#0
  - 코스트: 코스트: 엑스트라 덱에서 제외 [조건: IsAbleToRemoveAsCost()] → 조건에 맞는 아무 카드
- 효과 2: 지속 처리 (DRAW) / 계기: DRAW
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ryzeal Cross (6798031) ×1
- SPELL+FIELD · 카드군 0x1b6
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
- 효과 2: 적용 효과 (FIELD, CANNOT_SPECIAL_SUMMON) / 위치: FZONE
- 효과 3 [설명 0]: 기동 효과 / 위치: FZONE
  - 설명: Place 2 "Ryzeal" cards on the bottom of the Deck
  - 분류: TODECK, DRAW
  - 횟수: 1턴 1번 (처리 시 소비), 키 6798031#0
  - 효과: 묘지에서 덱으로 [조건: IsSetCard(SET_RYZEAL) and IsAbleToDeck() and not IsCode(id)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Plasma Hole, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 4 [설명 1]: 지속 처리 (CHAIN_SOLVING) / 계기: CHAIN_SOLVING / 위치: FZONE
  - 설명: Effect already applied
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 조건: 존재 조건 (몬스터 존에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
- 이 카드를 불러오는 카드:
  - Star Ryzeal — 효과 2: 덱에서 세트
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 3: 덱에서 패에 넣기
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ryzeal Plasma Hole (33787730) ×1
- TRAP · 카드군 0x1b6,0x89
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Destroy face-up cards your opponent controls
  - 분류: DESTROY, LEAVE_GRAVE
  - 횟수: 1턴 1번 (처리 시 소비), 키 33787730#0
  - 조건: 존재 조건 (몬스터 존에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 필드의 카드 파괴 [조건: IsFaceup()] → 상대 카드
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole, Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 몬스터 존에서 엑시즈 소재로 [조건: IsType(TYPE_XYZ) and IsRank(4) and IsFaceup() and IsCanBeXyzMaterial(c,tp,REASON_EFFECT)] → 이 덱에서: Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: GRAVE
  - 설명: Xyz Summon using monsters you control, including a "Ryzeal" monster
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 33787730#1
  - 효과: 몬스터 존의 카드를 소재로 엑시즈 소환 [조건: IsSetCard(SET_RYZEAL) and IsFaceup()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 엑스트라 덱에서 엑시즈 소환 [조건: IsXyzSummonable()] → 조건에 맞는 아무 카드
  - 미해석: inline function: Cost.SelfBanish
- 이 카드를 불러오는 카드:
  - Star Ryzeal — 효과 2: 덱에서 세트
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 3: 덱에서 패에 넣기
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ryzeal Plugin (60394026) ×1
- SPELL+QUICKPLAY · 카드군 0x1b6
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Special Summon 1 Xyz Monster or "Ryzeal" monster
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 60394026#0
  - 효과: 묘지/제외에서 특수 소환 [원문: s.spfilter = s.spfilter ; (c:IsType(TYPE_XYZ) or c:IsSetCard(SET_RYZEAL)) and c:IsFaceup() and c:IsCanBeSpecialSummoned(e,0,tp,false,false)] → (미해석, 연결 계산 안 함)
  - 효과: 덱에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
  - 효과: 몬스터 존에서 엑시즈 소재로 [조건: IsType(TYPE_XYZ) and IsRank(4) and IsFaceup() and IsCanBeXyzMaterial(c,tp,REASON_EFFECT)] → 이 덱에서: Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
  - 미해석: filter s.spfilter = s.spfilter ; (c:IsType(TYPE_XYZ) or c:IsSetCard(SET_RYZEAL)) and c:IsFaceup() and c:IsCanBeSpecialSummoned(e,0,tp,false,false)
- 이 카드를 불러오는 카드:
  - Star Ryzeal — 효과 2: 덱에서 세트
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 3: 덱에서 패에 넣기
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Sakitama (67972302) ×2
- MONSTER+EFFECT+SPIRIT · LIGHT · FAIRY · 레벨 4 · 400 / 900
- 효과 1: 적용 효과 (SINGLE, SPSUMMON_CONDITION)
- 효과 2 [설명 0]: 기동 효과 / 위치: HAND
  - 설명: Normal Summon 1 Spirit monster from your hand
  - 분류: SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 67972302#0
  - 효과: 패의 카드 선택 (처리 미해석) [조건: IsType(TYPE_SPIRIT) and IsSummonable(true,nil)] → 이 덱에서: Sakitama
  - 미해석: inline function: Cost.SelfReveal
- 효과 3 [설명 1]: 유발 효과 (강제) / 계기: RELEASE
  - 설명: Add 1 Spirit monster in your GY to your hand
  - 분류: TOHAND
  - 횟수: 1턴 1번 (처리 시 소비), 키 67972302#1
  - 효과: 묘지에서 패에 넣기 [조건: IsType(TYPE_SPIRIT) and IsAbleToHand()] → 이 덱에서: Sakitama
- 이 카드를 불러오는 카드:
  - Sakitama — 효과 2: 패의 카드 선택 (처리 미해석); 효과 3: 묘지에서 패에 넣기
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Seventh Tachyon (7477101) ×1
- SPELL · 카드군 0x177,0x1ab
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Add 1 monster with the same Type and/or Attribute as the revealed monster from your Deck to your hand
  - 분류: TOHAND, SEARCH, TODECK
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 7477101#0
  - 효과: 엑스트라 덱의 카드를 공개 [원문: s.revfilter = s.revfilter ; false end local number=c.xyz_number return number and number>=101 and number<=107 and Duel.IsExistingMatchingCard(s.thfilter,tp,LOCATION_DECK,0,1,nil,c:GetRace(),c:GetAttribute(),c:GetRank())] → (미해석, 연결 계산 안 함)
  - 효과: 덱에서 패에 넣기 [원문: s.thfilter = s.thfilter ; (c:IsRace(race) or c:IsAttribute(attr)) and c:IsLevel(rank) and c:IsAbleToHand()] → (미해석, 연결 계산 안 함)
  - 효과: 패에서 덱으로 [조건: IsAbleToDeck()] → 조건에 맞는 아무 카드
  - 미해석: filter s.revfilter = s.revfilter ; false end local number=c.xyz_number return number and number>=101 and number<=107 and Duel.IsExistingMatchingCard(s.thfilter,tp,LOCATION_DECK,0,1,nil,c:GetRace(),c:GetAttribute(),c:GetRank())
  - 미해석: filter s.thfilter = s.thfilter ; (c:IsRace(race) or c:IsAttribute(attr)) and c:IsLevel(rank) and c:IsAbleToHand()
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Skull Meister (67750322) ×1
- MONSTER+EFFECT · DARK · FIEND · 레벨 4 · 1700 / 400
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 설명: Negate card effect in opponent's Graveyard
  - 분류: DISABLE
  - 미해석: inline function: Cost.SelfToGrave
- 효과 2: 적용 효과 (SINGLE, 3682106) / 위치: HAND
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Dark Rebellion Xyz Dragon of the Four Heavenly Dragons — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Star Ryzeal (84433129) ×1
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 1800 / 0 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Star Ryzeal")
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 84433129#0
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Set 1 "Ryzeal" Spell/Trap from your Deck
  - 횟수: 1턴 1번 (처리 시 소비), 키 84433129#1
  - 효과: 덱에서 세트 [조건: IsSetCard(SET_RYZEAL) and IsSpellTrap() and IsSSetable()] → 이 덱에서: Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Sword Ryzeal (35844557) ×3
- MONSTER+EFFECT · FIRE · THUNDER · 레벨 4 · 1500 / 200 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Sword Ryzeal")
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 35844557#0
  - 조건: 존재 조건 (몬스터 존/묘지에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsMonster() and IsFaceup()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS, SPSUMMON_SUCCESS
  - 설명: Add 1 LIGHT Pyro monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (처리 시 소비), 키 35844557#1
  - 효과: 덱에서 패에 넣기 [조건: IsAttribute(ATTRIBUTE_LIGHT) and IsRace(RACE_PYRO) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 2: 덱에서 패에 넣기
  - Lightning Crash — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Triple Tactics Talent (25311006) ×1
- SPELL
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 횟수: 1턴 1번 (OATH: 발동 시 소비), 키 25311006#0
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Dark Rebellion Xyz Dragon of the Four Heavenly Dragons (90091224) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · DARK · DRAGON · 랭크 4 · 2500 / 2000 · 카드군 0x13b,0x2073,0x10db
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) [조건: IsAttribute(ATTRIBUTE_DARK)] → 이 덱에서: Skull Meister
- 효과 1 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Destroy an equal number of cards on each field
  - 분류: DESTROY
  - 횟수: 1턴 1번 (처리 시 소비), 키 90091224#0
  - 조건: 존재 조건 (필드에 있어야 함) → 조건에 맞는 아무 카드
  - 조건: 존재 조건 (필드에 있어야 함) → 상대 카드
- 효과 2 [설명 1]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Special Summon 1 Rank 4 DARK Xyz Monster
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 90091224#1
  - 효과: 엑스트라 덱에서 특수 소환 [조건: IsRank(4) and IsAttribute(ATTRIBUTE_DARK) and IsXyzMonster() and IsCanBeXyzMaterial(c,tp) and IsCanBeSpecialSummoned(e,SUMMON_TYPE_XYZ,tp,false,false)] → 이 덱에서: Exosister Karmael, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons
  - 미해석: inline function: function(e,tp,eg,ep,ev,re,r,rp) return rp==1-tp end
  - 미해석: inline function: Cost.DetachFromSelf(1)
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Dark Rebellion Xyz Dragon of the Four Heavenly Dragons — 효과 2: 엑스트라 덱에서 특수 소환
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Eclipse Twins (45852939) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · LIGHT · WARRIOR · 랭크 4 · 2500 / 1200
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (FIELD, XYZ_LEVEL) / 위치: EXTRA
  - 미해석: inline function: function(e,c) return c:IsRank(4) end
- 효과 2 [설명 0]: 기동 효과 / 위치: MZONE
  - 설명: Allow this card tto make up to 2 attacks on monsters
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 미해석: inline function: function() return Duel.IsAbleToEnterBP() end
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 3 [설명 1]: 유발 효과 (임의) / 계기: TO_GRAVE
  - 설명: Special Summon 1 other Rank 4 or lower Xyz Monster from your GY
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 (처리 시 소비), 키 45852939#0
  - 효과: 묘지에서 특수 소환 [조건: IsType(TYPE_XYZ) and IsRankBelow(4) and IsCanBeEffectTarget(e)] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Evilswarm Exciton Knight (46772449) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · LIGHT · FIEND · 랭크 4 · 1900 / 0 · 카드군 0xa
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Destroy all other cards on the field
  - 분류: DESTROY
  - 횟수: 1턴 1번 (처리 시 소비), 키 0#0
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 효과: 필드의 카드 파괴 → 조건에 맞는 아무 카드
  - 미해석: inline function: Cost.Detach(1)
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Exosister Karmael (77675029) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · DARK · FAIRY · 랭크 4 · 2600 / 1800 · 카드군 0x174
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) [조건: IsSetCard(SET_EXOSISTER)] → 이 덱에서: (없음)
- 효과 1 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: During the End Phase of this turn, Set 1 "Exosister" Spell/Trap from your Deck, GY, or banishment
  - 분류: SET
  - 횟수: 1턴 1번 (처리 시 소비), 키 77675029#0
  - 미해석: inline function: function(e) return e:GetHandler():IsXyzSummoned() end
- 효과 2 [설명 1]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Make the activated effect become "Add 1 card from your opponent's GY to their hand"
  - 횟수: 1턴 1번 (처리 시 소비), 키 77675029#1
  - 조건: 존재 조건 (묘지에 있어야 함) [조건: IsAbleToHand()] → 조건에 맞는 아무 카드
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Dark Rebellion Xyz Dragon of the Four Heavenly Dragons — 효과 2: 엑스트라 덱에서 특수 소환
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Full Armored Utopic Ray Lancer (1269512) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · WATER · BEASTWARRIOR · 랭크 4 · 2500 / 600 · 카드군 0x7f
- 소환 조건: 엑시즈 소재 (레벨 4 × 3장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (FIELD, UPDATE_ATTACK) / 위치: MZONE
- 효과 2 [설명 0]: 유발 효과 (임의) / 계기: PHASE, PHASE_BATTLE_START / 위치: MZONE
  - 설명: Xyz Summon by discarding 1 Spell/Trap
  - 분류: DISABLE
  - 횟수: 1턴 1번 (처리 시 소비), 키 card1269512:effect2
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsAttackPos() and IsNegatableMonster()] → 상대 카드
- 효과 3 [설명 1]: 유발 효과 (임의) / 계기: BATTLE_DESTROYING
  - 설명: Negate the effects of all your opponent's monsters
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 미해석: inline function: Cost.Detach(1,1,nil)
  - 미해석: inline function: function() Duel.ChainAttack() end
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Mereologic Aggregator (9940036) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · DARK · CYBERSE · 랭크 9 · 2600 / 3000
- 소환 조건: 엑시즈 소재 (레벨 9 × 2장) → 이 덱에서: (없음)
- 효과 1 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Send to the GY
  - 분류: TOGRAVE
  - 횟수: 1턴 1번 (처리 시 소비), 키 9940036#0
  - 효과: 엑스트라 덱에서 묘지로 [조건: IsMonster() and IsAbleToGrave()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Detach and attach
  - 횟수: 1턴 1번 (처리 시 소비), 키 9940036#1
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsMonster()] → 이 덱에서: Nibiru, the Primal Being, Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia, Ash Blossom & Joyous Spring, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 3 [설명 2]: 유발 효과 (임의) / 계기: TO_GRAVE
  - 설명: Negate
  - 분류: DISABLE
  - 횟수: 1턴 1번 (처리 시 소비), 키 9940036#2
  - 효과: 필드의 카드 선택 (처리 미해석) [조건: IsNegatable()] → 조건에 맞는 아무 카드
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Number 104: Masquerade (2061963) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · LIGHT · SPELLCASTER · 랭크 4 · 2700 / 1200 · 카드군 0x48
- 소환 조건: 엑시즈 소재 (레벨 4 × 3장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Negate effect
  - 분류: NEGATE, DAMAGE
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 미해석: inline function: Cost.Detach(1)
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Send to Graveyard
  - 분류: DECKDES
  - 횟수: 1턴 1번 (처리 시 소비), 키 card2061963:effect2
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Ryzeal Detonator (34909328) ×3 [엑스트라]
- MONSTER+EFFECT+XYZ · LIGHT · PYRO · 랭크 4 · 3000 / 2500 · 카드군 0x1b6
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Destroy 1 card on the field
  - 분류: DESTROY
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 효과: 필드의 카드 파괴 → 조건에 맞는 아무 카드
  - 미해석: inline function: function(e,tp,eg,ep,ev,re,r,rp) return rp==1-tp end
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Attach 1 monster from your GY to this card as material
  - 분류: LEAVE_GRAVE
  - 횟수: 1턴 1번 (처리 시 소비), 키 34909328#0
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsMonster() and IsCanBeXyzMaterial(xyzc,tp,REASON_EFFECT)] → 이 덱에서: Nibiru, the Primal Being, Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia, Ash Blossom & Joyous Spring, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Exosister Karmael, Ryzeal Duo Drive, Dark Rebellion Xyz Dragon of the Four Heavenly Dragons, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 3: 지속 처리 (DESTROY_REPLACE) / 위치: MZONE
  - 횟수: 1턴 1번 (처리 시 소비), 키 34909328#1
  - 미해석: inline function: function(e,tp) e:GetHandler():RemoveOverlayCard(tp,1,1,REASON_EFFECT) end
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로; 효과 4: 존재 조건 (몬스터 존에 있어야 함); 효과 4: 몬스터 존의 카드 선택 (처리 미해석)
  - Ryzeal Plasma Hole — 효과 1: 존재 조건 (몬스터 존에 있어야 함); 효과 1: 묘지에서 엑시즈 소재로; 효과 1: 몬스터 존에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Ryzeal Duo Drive (7511613) ×2 [엑스트라]
- MONSTER+EFFECT+XYZ · FIRE · THUNDER · 랭크 4 · 2500 / 2000 · 카드군 0x1b6
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (FIELD, UPDATE_ATTACK) / 위치: MZONE
- 효과 2 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Attach 1 "Ryzeal" monster from your GY to this card as material
  - 분류: LEAVE_GRAVE
  - 횟수: 1턴 1번 (처리 시 소비), 키 7511613#0
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL) and IsMonster() and IsCanBeXyzMaterial(xyzc,tp,REASON_EFFECT)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 3 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Add 2 "Ryzeal" cards from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 (처리 시 소비), 키 7511613#1
  - 엑시즈 소재 제거: 2개 (필드에 그만큼 소재가 있어야 함)
  - 효과: 덱에서 패에 넣기 [조건: IsSetCard(SET_RYZEAL) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로; 효과 4: 존재 조건 (몬스터 존에 있어야 함); 효과 4: 몬스터 존의 카드 선택 (처리 미해석)
  - Ryzeal Plasma Hole — 효과 1: 존재 조건 (몬스터 존에 있어야 함); 효과 1: 묘지에서 엑시즈 소재로; 효과 1: 몬스터 존에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Super Starslayer TY-PHON - Sky Crisis (93039339) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · DARK · FIEND · 랭크 12 · 2900 / 2900
- 소환 조건: 엑시즈 소재 (레벨 12 × 2장) → 이 덱에서: (없음)
- 효과 1: 적용 효과 (FIELD, CANNOT_ACTIVATE) / 위치: MZONE
  - 미해석: inline function: function(e) return e:GetHandler():IsXyzSummoned() end
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Return 1 monster on the field to the hand
  - 분류: TOHAND
  - 횟수: 1턴 1번 (처리 시 소비), 키 card93039339:effect2
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 효과: 몬스터 존에서 패에 넣기 [조건: IsAbleToHand()] → 조건에 맞는 아무 카드
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 3: 지속 처리 (SPSUMMON_SUCCESS) / 계기: SPSUMMON_SUCCESS
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Tornado Dragon (6983839) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · WIND · WYRM · 랭크 4 · 2100 / 2000
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Destroy S&T
  - 분류: DESTROY
  - 횟수: 1턴 1번 (처리 시 소비), 키 card6983839:effect1
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 효과: 필드의 카드 파괴 [조건: IsSpellTrap()] → 이 덱에서: Seventh Tachyon, Triple Tactics Talent, Pot of Prosperity, Bonfire, Lightning Crash, Ryzeal Plugin, Crossout Designator, Ryzeal Cross, Infinite Impermanence, Gravedigger's Trap Hole, Ryzeal Plasma Hole
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Traptrix Rafflesia (6511113) ×1 [엑스트라]
- MONSTER+EFFECT+XYZ · EARTH · PLANT · 랭크 4 · 300 / 2500 · 카드군 0x108a
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (SINGLE, IMMUNE_EFFECT) / 위치: MZONE
- 효과 2: 적용 효과 (FIELD, INDESTRUCTABLE_BATTLE) / 위치: MZONE
- 효과 3: 적용 효과 (FIELD, INDESTRUCTABLE_EFFECT) / 위치: MZONE
- 효과 4: 적용 효과 (FIELD, CANNOT_BE_TARGET) / 위치: MZONE
- 효과 5 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Copy the effect of 1 "Hole" Normal Trap
  - 횟수: 1턴 1번 (처리 시 소비), 키 card6511113:effect5
  - 코스트: 코스트: 덱에서 묘지로 [원문: s.filter = s.filter ; c:IsNormalTrap() and (c:IsSetCard(SET_TRAP_HOLE) or c:IsSetCard(SET_HOLE)) and c:IsAbleToGraveAsCost() and c:CheckActivateEffect(false,true,true)~=nil] → (미해석, 연결 계산 안 함)
  - 미해석: filter s.filter = s.filter ; c:IsNormalTrap() and (c:IsSetCard(SET_TRAP_HOLE) or c:IsSetCard(SET_HOLE)) and c:IsAbleToGraveAsCost() and c:CheckActivateEffect(false,true,true)~=nil
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

# 가치 계산 시험 (패: 8633261,35844557,34022970,72238166,84433129,60394026,89753095,85106525,84211599,7477101,14558127, 필드·묘지 비어 있음, 일반 소환 가능)

- 덱에 남은 카드(덱 매수 − 보이는 장수): Nibiru, the Primal Being 1, Ice Ryzeal 2, Skull Meister 1, Sword Ryzeal 2, Ext Ryzeal 2, Sakitama 2, Mulcharmy Fuwalos 3, Mulcharmy Purulia 2, Ash Blossom & Joyous Spring 1, Ghost Ogre & Snow Rabbit 1, Ghost Belle & Haunted Mansion 1, Maxx "C" 1, Triple Tactics Talent 1, Lightning Crash 2, Crossout Designator 1, Ryzeal Cross 1, Infinite Impermanence 3, Gravedigger's Trap Hole 1, Ryzeal Plasma Hole 1
- γ = 0.7, 가중치 출처: ./WindBot\agent_values.json
- 패: Star Ryzeal → 최선 0
- 패: Ice Ryzeal → 최선 85.9 = Ice Ryzeal(2) 덱 특수 소환 40 → Ext Ryzeal(2) 서치 30×γ^1 → Sword Ryzeal(1) 룰 특수 소환 20×γ^2 → Sword Ryzeal(2) 서치 30×γ^3 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^4
  - 일반 소환 시: 85.9 = Ice Ryzeal(2) 덱 특수 소환 40 → Ext Ryzeal(2) 서치 30×γ^1 → Sword Ryzeal(1) 룰 특수 소환 20×γ^2 → Sword Ryzeal(2) 서치 30×γ^3 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^4
- 패: Sword Ryzeal → 최선 44 = Sword Ryzeal(2) 서치 30 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^1
  - 일반 소환 시: 44 = Sword Ryzeal(2) 서치 30 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^1
- 패: Node Ryzeal → 최선 20 = Node Ryzeal(1) 룰 특수 소환 20
- 패: Ext Ryzeal → 최선 65.6 = Ext Ryzeal(2) 서치 30 → Sword Ryzeal(1) 룰 특수 소환 20×γ^1 → Sword Ryzeal(2) 서치 30×γ^2 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^3
  - 일반 소환 시: 65.6 = Ext Ryzeal(2) 서치 30 → Sword Ryzeal(1) 룰 특수 소환 20×γ^1 → Sword Ryzeal(2) 서치 30×γ^2 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^3
- 패: Ash Blossom & Joyous Spring → 최선 0
- 패: Seventh Tachyon → 최선 30 = Seventh Tachyon(1) 서치 30
- 패: Pot of Prosperity → 최선 30 = Pot of Prosperity(1) 서치(분류 기준) 30
- 패: Bonfire → 최선 90.1 = Bonfire(1) 서치 30 → Ice Ryzeal(2) 덱 특수 소환 40×γ^1 → Ext Ryzeal(2) 서치 30×γ^2 → Sword Ryzeal(1) 룰 특수 소환 20×γ^3 → Sword Ryzeal(2) 서치 30×γ^4 → Ice Ryzeal(1) 룰 특수 소환+코스트(패/필드→묘지 0) 20×γ^5
- 패: Lightning Crash → 최선 30 = Lightning Crash(1) 서치 30
- 패: Ryzeal Plugin → 최선 0

