# 게임 카드 DB

- 메인 덱 40장, 엑스트라 덱 15장, DB 카드 종류 37
- 턴마다 바뀌는 발동·사용 기록과 카드 위치는 같은 이름의 `_state.md`에 있다.

## Ash Blossom & Joyous Spring (14558127) ×2 (상대 덱 ×2)
- MONSTER+EFFECT+TUNER · FIRE · ZOMBIE · 레벨 3 · 0 / 1800
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 설명: Negate that effect
  - 분류: DISABLE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 14558127#0
  - 응답 대상: 발동에 체인 / 포함 효과: 덱에서 패에 넣기, 덱에서 특수 소환, 덱에서 묘지로 보내기, 드로우, 서치, 덱에서 묘지로
  - 발동 위치: HAND / 처리: DISABLE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 전부
  - 미해석: inline function: Cost.SelfDiscard
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Bonfire (85106525) ×1 (상대 덱 ×1)
- SPELL
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Search 1 Level 4 or lower Pyro monster
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 85106525#0
  - 발동 위치: SZONE / 처리: TOHAND, SEARCH, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 패에 넣기 [조건: IsLevelBelow(4) and IsRace(RACE_PYRO) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Crossout Designator (65681983) ×1 (상대 덱 ×1)
- SPELL+QUICKPLAY
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: REMOVE
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 65681983#0
  - 응답 대상: 이름 선언 / 선언한 이름의 카드만
  - 발동 위치: SZONE / 처리: REMOVE, REMOVE@DECK
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 전부
  - 효과: 덱에서 제외 [조건: IsAbleToRemove()] → 조건에 맞는 아무 카드
  - 효과: 덱에서 제외 [조건: IsCode(ac) and IsAbleToRemove()] → 이 덱에서: (없음)
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ext Ryzeal (34022970) ×3 (상대 덱 ×3)
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 500 / 2000 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Ext Ryzeal")
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 34022970#0
  - 사용 후 이번 턴: 엑스트라 덱에서 랭크 4 엑시즈만 특수 소환
  - 조건: 존재 조건 (엑스트라 덱에 있어야 함) [조건: IsType(TYPE_XYZ) and IsAbleToGraveAsCost()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
  - 코스트: 코스트: 엑스트라 덱에서 묘지로 [조건: IsType(TYPE_XYZ) and IsAbleToGraveAsCost()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS, SPSUMMON_SUCCESS
  - 설명: Add 1 FIRE Thunder monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 34022970#1
  - 발동 위치: MZONE / 처리: TOHAND, SEARCH, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 조건: 부재 조건 (몬스터 존에 없어야 함) [조건: IsFaceup() and not (IsLevel(4) or IsRank(4))] → 이 덱에서: Nibiru, the Primal Being, Ash Blossom & Joyous Spring, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Number F0: Utopic Future Zexal
  - 효과: 덱에서 패에 넣기 [조건: IsAttribute(ATTRIBUTE_FIRE) and IsRace(RACE_THUNDER) and IsAbleToHand()] → 이 덱에서: Sword Ryzeal, Node Ryzeal
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 효과 1: 덱에서 패에 넣기; 효과 2: 존재 조건 (몬스터 존에 있어야 함); 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Ghost Belle & Haunted Mansion (73642296) ×1 (상대 덱 ×1)
- MONSTER+EFFECT+TUNER · EARTH · ZOMBIE · 레벨 3 · 0 / 1800
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 분류: NEGATE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 73642296#0
  - 응답 대상: 발동에 체인 / 포함 효과: 묘지에서 특수 소환, 묘지에서 제외, 묘지에서 패에 넣기, 묘지에서 덱으로, 묘지에서 엑스트라 덱으로
  - 발동 위치: HAND / 처리: NEGATE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 전부
  - 미해석: inline function: Cost.SelfDiscard
  - 미해석: inline function: function(e,tp,eg,ep,ev) Duel.NegateActivation(ev) end
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Ghost Ogre & Snow Rabbit (59438930) ×1 (상대 덱 ×1)
- MONSTER+EFFECT+TUNER · LIGHT · PSYCHIC · 레벨 3 · 0 / 1800
- 효과 1: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE|HAND
  - 분류: DESTROY
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 59438930#0
  - 응답 대상: 발동에 체인 / 몬스터 효과 또는 카드의 발동이 아닌 마법·함정 효과 / 발동 위치: 필드
  - 발동 위치: MZONE/HAND / 처리: DESTROY
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: Cost.SelfToGrave
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Gravedigger's Trap Hole (31548215) ×1 (상대 덱 ×1)
- TRAP · 카드군 0x89
- 효과 1: 카드의 발동 / 계기: CHAINING
  - 분류: DISABLE, DAMAGE
  - 응답 대상: 발동에 체인 / 상대 효과만 / 몬스터 효과 / 발동 위치: 묘지/패/제외 존
  - 발동 위치: SZONE / 처리: DISABLE, DAMAGE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 몬스터 효과
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴
  - Traptrix Rafflesia — 효과 5: 코스트: 덱에서 묘지로

## Ice Ryzeal (8633261) ×3 (상대 덱 ×3)
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 1700 / 1000 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Ice Ryzeal")
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 8633261#0
  - 사용 후 이번 턴: 엑스트라 덱에서 랭크 4 엑시즈만 특수 소환
  - 조건: 존재 조건 (패/필드에 있어야 함) [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
  - 코스트: 코스트: 패/필드에서 묘지로 [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS
  - 설명: Special Summon 1 "Ryzeal" monster from your Deck
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 8633261#1
  - 발동 위치: MZONE / 처리: SPECIAL_SUMMON, SPECIAL_SUMMON@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 특수 소환 [조건: IsSetCard(SET_RYZEAL) and not IsCode(id) and IsCanBeSpecialSummoned(e,0,tp,false,false)] → 이 덱에서: Star Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 효과 1: 덱에서 패에 넣기; 효과 2: 존재 조건 (몬스터 존에 있어야 함); 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Infinite Impermanence (10045474) ×3 (상대 덱 ×3)
- TRAP
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: DISABLE
  - 응답 대상: 필드의 앞면 카드 선택 / 몬스터 효과 / 발동 위치: 필드
  - 발동 위치: SZONE / 처리: DISABLE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 몬스터 효과
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsNegatableMonster()] → 상대 카드
- 효과 2 [설명 0]: 적용 효과 (SINGLE, TRAP_ACT_IN_HAND)
  - 설명: Activate using "Infinite Impermanence"
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Lightning Crash (89753095) ×3 (상대 덱 ×3)
- SPELL
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Add 1 Thunder monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 89753095#0
  - 발동 위치: SZONE / 처리: TOHAND, SEARCH, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 제약: 이 효과로 패에 넣은 카드는 일반 소환할 수 없음
  - 효과: 덱에서 패에 넣기 [조건: IsRace(RACE_THUNDER) and IsAbleToHand()] → 이 덱에서: Sword Ryzeal, Node Ryzeal
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Maxx "C" (23434538) ×1 (상대 덱 ×1)
- MONSTER+EFFECT · EARTH · INSECT · 레벨 2 · 500 / 200
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Draw 1 card for eatch Special Summoned card on opponent's Field
  - 분류: DRAW
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 23434538#0
  - 발동 위치: HAND / 처리: DRAW
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: Cost.SelfToGrave
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Mulcharmy Fuwalos (42141493) ×3 (상대 덱 ×3)
- MONSTER+EFFECT · WIND · WINGEDBEAST · 레벨 4 · 100 / 600 · 카드군 0x1ac
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Apply the "●" effects for the rest of the turn
  - 분류: DRAW, TODECK
  - 발동 위치: HAND / 처리: DRAW, TODECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: function(e,tp) return Duel.GetFieldGroupCount(tp,LOCATION_ONFIELD,0)==0 end
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Mulcharmy Purulia (84192580) ×2 (상대 덱 ×2)
- MONSTER+EFFECT · WATER · AQUA · 레벨 4 · 100 / 600 · 카드군 0x1ac
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 설명: Apply the "●" effects for the rest of the turn
  - 분류: DRAW, TODECK
  - 발동 위치: HAND / 처리: DRAW, TODECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: function(e,tp) return Duel.GetFieldGroupCount(tp,LOCATION_ONFIELD,0)==0 end
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Nibiru, the Primal Being (27204311) ×1 (상대 덱 ×1)
- MONSTER+EFFECT · LIGHT · ROCK · 레벨 11 · 3000 / 600
- 효과 1: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: HAND
  - 분류: RELEASE, SPECIAL_SUMMON, TOKEN
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 27204311#0
  - 응답 대상: 상대 소환 5회 이후
  - 발동 위치: HAND / 처리: RELEASE, SPECIAL_SUMMON, TOKEN
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 몬스터 존에서 릴리스 [원문: aux.FaceupFilter(Card.IsReleasableByEffect)] → (미해석, 연결 계산 안 함)
  - 미해석: filter aux.FaceupFilter(Card.IsReleasableByEffect)
- 효과 2: 지속 처리 (SPSUMMON_SUCCESS) / 계기: SPSUMMON_SUCCESS
- 효과 3: 지속 처리 (SUMMON_SUCCESS) / 계기: SUMMON_SUCCESS
- 이 카드를 불러오는 카드:
  - Ext Ryzeal — 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로

## Node Ryzeal (72238166) ×1 (상대 덱 ×1)
- MONSTER+EFFECT · FIRE · THUNDER · 레벨 4 · 1400 / 1600 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Node Ryzeal")
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 72238166#0
  - 사용 후 이번 턴: 엑스트라 덱에서 랭크 4 엑시즈만 특수 소환
  - 조건: 존재 조건 (몬스터 존/묘지에 있어야 함) [조건: IsFaceup() and IsType(TYPE_XYZ)] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Special Summon 1 "Ryzeal" monster from your GY
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 72238166#1
  - 발동 위치: MZONE / 처리: SPECIAL_SUMMON, SPECIAL_SUMMON@GRAVE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Ghost Belle & Haunted Mansion ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 코스트: 코스트: 패/필드에서 묘지로 [조건: IsAbleToGraveAsCost()] → 조건에 맞는 아무 카드
  - 효과: 묘지에서 특수 소환 [조건: IsSetCard(SET_RYZEAL) and IsCanBeSpecialSummoned(e,0,tp,false,false,POS_FACEUP_DEFENSE) and not IsCode(id)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 2: 덱에서 패에 넣기
  - Lightning Crash — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Pot of Prosperity (84211599) ×1 (상대 덱 ×1)
- SPELL
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 84211599#0
  - 발동 위치: SZONE / 처리: TOHAND, SEARCH
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 코스트: 코스트: 엑스트라 덱에서 제외 [조건: IsAbleToRemoveAsCost()] → 조건에 맞는 아무 카드
- 효과 2: 지속 처리 (DRAW) / 계기: DRAW
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ryzeal Cross (6798031) ×1 (상대 덱 ×1)
- SPELL+FIELD · 카드군 0x1b6
- 앞면인 동안: 같은 이름 엑시즈 몬스터 특수 소환 불가
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 발동 위치: FZONE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
- 효과 2: 적용 효과 (FIELD, CANNOT_SPECIAL_SUMMON) / 위치: FZONE
- 효과 3 [설명 0]: 기동 효과 / 위치: FZONE
  - 설명: Place 2 "Ryzeal" cards on the bottom of the Deck
  - 분류: TODECK, DRAW
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 6798031#0
  - 발동 위치: FZONE / 처리: TODECK, DRAW, TODECK@GRAVE
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Ghost Belle & Haunted Mansion ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 덱으로 [조건: IsSetCard(SET_RYZEAL) and IsAbleToDeck() and not IsCode(id)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Plasma Hole, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 4 [설명 1]: 지속 처리 (CHAIN_SOLVING) / 계기: CHAIN_SOLVING / 위치: FZONE
  - 설명: Effect already applied
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 응답 대상: 처리 시 적용 / 상대 효과만 / 몬스터 효과
  - 무효 대상: 몬스터 효과
  - 조건: 존재 조건 (몬스터 존에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
- 이 카드를 불러오는 카드:
  - Star Ryzeal — 효과 2: 덱에서 세트
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 3: 덱에서 패에 넣기
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Ryzeal Plasma Hole (33787730) ×1 (상대 덱 ×1)
- TRAP · 카드군 0x1b6,0x89
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Destroy face-up cards your opponent controls
  - 분류: DESTROY, LEAVE_GRAVE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 33787730#0
  - 발동 위치: SZONE / 처리: DESTROY, LEAVE_GRAVE, DESTROY@ONFIELD
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 조건: 존재 조건 (몬스터 존에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsType(TYPE_XYZ) and IsFaceup()] → 이 덱에서: Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 필드의 카드 파괴 [조건: IsFaceup()] → 상대 카드
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole, Ryzeal Detonator, Ryzeal Duo Drive
  - 효과: 몬스터 존에서 엑시즈 소재로 [조건: IsType(TYPE_XYZ) and IsRank(4) and IsFaceup() and IsCanBeXyzMaterial(c,tp,REASON_EFFECT)] → 이 덱에서: Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: GRAVE
  - 설명: Xyz Summon using monsters you control, including a "Ryzeal" monster
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 33787730#1
  - 발동 위치: GRAVE / 처리: SPECIAL_SUMMON
  - 막을 수 있는 상대 카드: Skull Meister ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
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
  - Traptrix Rafflesia — 효과 5: 코스트: 덱에서 묘지로

## Ryzeal Plugin (60394026) ×1 (상대 덱 ×1)
- SPELL+QUICKPLAY · 카드군 0x1b6
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Special Summon 1 Xyz Monster or "Ryzeal" monster
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 60394026#0
  - 사용 후 이번 턴: 랭크 4 엑시즈만 공격 선언
  - 발동 위치: SZONE / 처리: SPECIAL_SUMMON, SPECIAL_SUMMON@GRAVE, SPECIAL_SUMMON@REMOVED
  - 막을 수 있는 상대 카드: Ghost Belle & Haunted Mansion ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지/제외에서 특수 소환 [조건: IsFaceup() and IsCanBeSpecialSummoned(e,0,tp,false,false) and (IsType(TYPE_XYZ) or IsSetCard(SET_RYZEAL))] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
  - 효과: 덱에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
  - 효과: 몬스터 존에서 엑시즈 소재로 [조건: IsType(TYPE_XYZ) and IsRank(4) and IsFaceup() and IsCanBeXyzMaterial(c,tp,REASON_EFFECT)] → 이 덱에서: Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 이 카드를 불러오는 카드:
  - Star Ryzeal — 효과 2: 덱에서 세트
  - Ryzeal Plugin — 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 3: 덱에서 패에 넣기
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Sakitama (67972302) ×2 (상대 덱 ×2)
- MONSTER+EFFECT+SPIRIT · LIGHT · FAIRY · 레벨 4 · 400 / 900
- 효과 1: 적용 효과 (SINGLE, SPSUMMON_CONDITION)
- 효과 2 [설명 0]: 기동 효과 / 위치: HAND
  - 설명: Normal Summon 1 Spirit monster from your hand
  - 분류: SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 67972302#0
  - 발동 위치: HAND / 처리: SUMMON
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 패의 카드 선택 (처리 미해석) [조건: IsType(TYPE_SPIRIT) and IsSummonable(true,nil)] → 이 덱에서: Sakitama
  - 미해석: inline function: Cost.SelfReveal
- 효과 3 [설명 1]: 유발 효과 (강제) / 계기: RELEASE
  - 설명: Add 1 Spirit monster in your GY to your hand
  - 분류: TOHAND
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 67972302#1
  - 발동 위치: 모름 / 처리: TOHAND, TOHAND@GRAVE
  - 막을 수 있는 상대 카드: Skull Meister ×1(무효), Ghost Ogre & Snow Rabbit ×1(방해), Ghost Belle & Haunted Mansion ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 패에 넣기 [조건: IsType(TYPE_SPIRIT) and IsAbleToHand()] → 이 덱에서: Sakitama
- 이 카드를 불러오는 카드:
  - Sakitama — 효과 2: 패의 카드 선택 (처리 미해석); 효과 3: 묘지에서 패에 넣기
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Seventh Tachyon (7477101) ×1 (상대 덱 ×1)
- SPELL · 카드군 0x177,0x1ab
- 효과 1 [설명 0]: 카드의 발동 / 계기: FREE_CHAIN
  - 설명: Add 1 monster with the same Type and/or Attribute as the revealed monster from your Deck to your hand
  - 분류: TOHAND, SEARCH, TODECK
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 7477101#0
  - 사용 후 이번 턴: 엑스트라 덱에서 엑시즈만 특수 소환
  - 발동 위치: SZONE / 처리: TOHAND, SEARCH, TODECK, TOHAND@DECK, TODECK@HAND
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 엑스트라 덱의 카드를 공개 [원문: s.revfilter = s.revfilter ; false end local number=c.xyz_number return number and number>=101 and number<=107 and Duel.IsExistingMatchingCard(s.thfilter,tp,LOCATION_DECK,0,1,nil,c:GetRace(),c:GetAttribute(),c:GetRank())] → (미해석, 연결 계산 안 함)
  - 효과: 덱에서 패에 넣기 [조건: IsLevel(rank) and IsAbleToHand() and (IsRace(race) or IsAttribute(attr))] → 이 덱에서: (없음)
  - 효과: 패에서 덱으로 [조건: IsAbleToDeck()] → 조건에 맞는 아무 카드
  - 미해석: filter s.revfilter = s.revfilter ; false end local number=c.xyz_number return number and number>=101 and number<=107 and Duel.IsExistingMatchingCard(s.thfilter,tp,LOCATION_DECK,0,1,nil,c:GetRace(),c:GetAttribute(),c:GetRank())
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Skull Meister (67750322) ×1 (상대 덱 ×1)
- MONSTER+EFFECT · DARK · FIEND · 레벨 4 · 1700 / 400
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: HAND
  - 설명: Negate card effect in opponent's Graveyard
  - 분류: DISABLE
  - 응답 대상: 발동에 체인 / 상대 효과만 / 발동 위치: 묘지
  - 발동 위치: HAND / 처리: DISABLE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 전부
  - 미해석: inline function: Cost.SelfToGrave
- 효과 2: 적용 효과 (SINGLE, 3682106) / 위치: HAND
- 이 카드를 불러오는 카드:
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Star Ryzeal (84433129) ×1 (상대 덱 ×1)
- MONSTER+EFFECT · LIGHT · PYRO · 레벨 4 · 1800 / 0 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Star Ryzeal")
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 84433129#0
  - 사용 후 이번 턴: 엑스트라 덱에서 랭크 4 엑시즈만 특수 소환
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Set 1 "Ryzeal" Spell/Trap from your Deck
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 84433129#1
  - 발동 위치: MZONE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 세트 [조건: IsSetCard(SET_RYZEAL) and IsSpellTrap() and IsSSetable()] → 이 덱에서: Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 덱에서 패에 넣기
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Bonfire — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 효과 1: 덱에서 패에 넣기; 효과 2: 존재 조건 (몬스터 존에 있어야 함); 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Sword Ryzeal (35844557) ×3 (상대 덱 ×3)
- MONSTER+EFFECT · FIRE · THUNDER · 레벨 4 · 1500 / 200 · 카드군 0x1b6
- 효과 1 [설명 0]: 룰 특수 소환 (체인 없음) / 위치: HAND
  - 설명: Special Summon ("Sword Ryzeal")
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 35844557#0
  - 사용 후 이번 턴: 엑스트라 덱에서 랭크 4 엑시즈만 특수 소환
  - 조건: 존재 조건 (몬스터 존/묘지에 있어야 함) [조건: IsSetCard(SET_RYZEAL) and IsMonster() and IsFaceup()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SUMMON_SUCCESS, SPSUMMON_SUCCESS
  - 설명: Add 1 LIGHT Pyro monster from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 35844557#1
  - 발동 위치: MZONE / 처리: TOHAND, SEARCH, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 패에 넣기 [조건: IsAttribute(ATTRIBUTE_LIGHT) and IsRace(RACE_PYRO) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal
- 이 카드를 불러오는 카드:
  - Ice Ryzeal — 효과 2: 덱에서 특수 소환
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 2: 덱에서 패에 넣기
  - Lightning Crash — 효과 1: 덱에서 패에 넣기
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 덱에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로
  - Ryzeal Plasma Hole — 효과 1: 묘지에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Number 104: Masquerade — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Infernal Flame Banshee — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로; 효과 3: 덱에서 패에 넣기; 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Eclipse Twins — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Full Armored Utopic Ray Lancer — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 3장)
  - Tornado Dragon — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Evilswarm Exciton Knight — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)
  - Traptrix Rafflesia — 소재로 쓰여 소환됨: 엑시즈 소재 (레벨 4 × 2장)

## Triple Tactics Talent (25311006) ×1 (상대 덱 ×1)
- SPELL
- 효과 1: 카드의 발동 / 계기: FREE_CHAIN
  - 횟수: 1턴 1번 ('발동' 제약: 발동 무효되면 다시 발동 가능), 키 25311006#0
  - 발동 위치: SZONE
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Detonator ×3(방해), Number F0: Utopic Future Zexal ×1(방해)
- 이 카드를 불러오는 카드:
  - Tornado Dragon — 효과 1: 필드의 카드 파괴

## Eclipse Twins (45852939) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · LIGHT · WARRIOR · 랭크 4 · 2500 / 1200
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (FIELD, XYZ_LEVEL) / 위치: EXTRA
  - 미해석: inline function: function(e,c) return c:IsRank(4) end
- 효과 2 [설명 0]: 기동 효과 / 위치: MZONE
  - 설명: Allow this card tto make up to 2 attacks on monsters
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: function() return Duel.IsAbleToEnterBP() end
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 3 [설명 1]: 유발 효과 (임의) / 계기: TO_GRAVE
  - 설명: Special Summon 1 other Rank 4 or lower Xyz Monster from your GY
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 45852939#0
  - 발동 위치: GRAVE / 처리: SPECIAL_SUMMON, SPECIAL_SUMMON@GRAVE
  - 막을 수 있는 상대 카드: Skull Meister ×1(무효), Ghost Belle & Haunted Mansion ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 특수 소환 [조건: IsType(TYPE_XYZ) and IsRankBelow(4) and IsCanBeEffectTarget(e)] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Evilswarm Exciton Knight (46772449) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · LIGHT · FIEND · 랭크 4 · 1900 / 0 · 카드군 0xa
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Destroy all other cards on the field
  - 분류: DESTROY
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 0#0
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE / 처리: DESTROY, DESTROY@ONFIELD
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 필드의 카드 파괴 → 조건에 맞는 아무 카드
  - 미해석: inline function: Cost.Detach(1)
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Full Armored Utopic Ray Lancer (1269512) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · WATER · BEASTWARRIOR · 랭크 4 · 2500 / 600 · 카드군 0x7f
- 소환 조건: 엑시즈 소재 (레벨 4 × 3장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 다른 소환 방법: 엑시즈 몬스터 1장 위 (랭크 4 이하), 패 1장 버림, 1턴 1번 → 버릴 수 있는 카드: Seventh Tachyon, Triple Tactics Talent, Pot of Prosperity, Bonfire, Lightning Crash, Ryzeal Plugin, Crossout Designator, Ryzeal Cross, Infinite Impermanence, Gravedigger's Trap Hole, Ryzeal Plasma Hole
- 전투: 상대 몬스터 공격력 -500, 전투 파괴 후 연속 공격 (소재 1개), 배틀 페이즈 개시 시 상대 공격 표시 몬스터 효과 무효
- 효과 1: 적용 효과 (FIELD, UPDATE_ATTACK) / 위치: MZONE
- 효과 2 [설명 0]: 유발 효과 (임의) / 계기: PHASE, PHASE_BATTLE_START / 위치: MZONE
  - 설명: Xyz Summon by discarding 1 Spell/Trap
  - 분류: DISABLE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card1269512:effect2
  - 응답 대상: 필드의 앞면 카드 선택 / 몬스터 효과 / 발동 위치: 필드
  - 발동 위치: MZONE / 처리: DISABLE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 몬스터 효과
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsAttackPos() and IsNegatableMonster()] → 상대 카드
- 효과 3 [설명 1]: 유발 효과 (임의) / 계기: BATTLE_DESTROYING
  - 설명: Negate the effects of all your opponent's monsters
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: 모름
  - 막을 수 있는 상대 카드: Skull Meister ×1(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 미해석: inline function: Cost.Detach(1,1,nil)
  - 미해석: inline function: function() Duel.ChainAttack() end
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Infernal Flame Banshee (29423048) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · FIRE · PYRO · 랭크 4 · 1700 / 600
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 기동 효과 / 위치: MZONE
  - 설명: Take 1 Pyro monster from your Deck
  - 분류: TOHAND, SEARCH, TOGRAVE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 29423048#0
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE / 처리: TOHAND, SEARCH, TOGRAVE, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 패에 넣기 [조건: IsRace(RACE_PYRO) and (IsAbleToHand() or IsAbleToGrave())] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: REMOVE
  - 설명: Special Summon this card
  - 분류: SPECIAL_SUMMON
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 29423048#1
  - 발동 위치: REMOVED / 처리: SPECIAL_SUMMON
  - 막을 수 있는 상대 카드: Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 조건: 존재 조건 (몬스터 존에 있어야 함) [조건: IsFaceup() and IsRace(RACE_PYRO)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Ext Ryzeal, Ryzeal Detonator, Infernal Flame Banshee
  - 조건: 존재 조건 (제외에 있어야 함) [원문: aux.FaceupFilter(Card.IsMonster)] → (미해석, 연결 계산 안 함)
  - 미해석: filter aux.FaceupFilter(Card.IsMonster)
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Infernal Flame Banshee — 효과 2: 존재 조건 (몬스터 존에 있어야 함)
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Mereologic Aggregator (9940036) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · DARK · CYBERSE · 랭크 9 · 2600 / 3000
- 소환 조건: 엑시즈 소재 (레벨 9 × 2장) → 이 덱에서: (없음)
- 효과 1 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Send to the GY
  - 분류: TOGRAVE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 9940036#0
  - 발동 위치: MZONE / 처리: TOGRAVE, TOGRAVE@EXTRA
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 엑스트라 덱에서 묘지로 [조건: IsMonster() and IsAbleToGrave()] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Detach and attach
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 9940036#1
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsMonster()] → 이 덱에서: Nibiru, the Primal Being, Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia, Ash Blossom & Joyous Spring, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 3 [설명 2]: 유발 효과 (임의) / 계기: TO_GRAVE
  - 설명: Negate
  - 분류: DISABLE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 9940036#2
  - 응답 대상: 필드의 앞면 카드 선택 / 발동 위치: 필드
  - 발동 위치: GRAVE / 처리: DISABLE
  - 막을 수 있는 상대 카드: Skull Meister ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 전부
  - 효과: 필드의 카드 선택 (처리 미해석) [조건: IsNegatable()] → 조건에 맞는 아무 카드
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로; 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Number 104: Masquerade (2061963) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · LIGHT · SPELLCASTER · 랭크 4 · 2700 / 1200 · 카드군 0x48
- 소환 조건: 엑시즈 소재 (레벨 4 × 3장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Negate effect
  - 분류: NEGATE, DAMAGE
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 응답 대상: 발동에 체인 / 상대 효과만 / 몬스터 효과
  - 발동 위치: MZONE / 처리: NEGATE, DAMAGE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 무효 대상: 몬스터 효과
  - 미해석: inline function: Cost.Detach(1)
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Send to Graveyard
  - 분류: DECKDES
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card2061963:effect2
  - 발동 위치: MZONE / 처리: DECKDES
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Number F0: Utopic Future Zexal (41522092) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · LIGHT · WARRIOR · 랭크 1 · 0 / 0 · 카드군 0x48,0x207f,0x7e
- 소환 조건: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장) [조건: IsType(TYPE_XYZ)] → 이 덱에서: Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 전투: 공격력 +500 × (자신 필드 + 상대 묘지 엑시즈 랭크 합)
- 효과 1: 적용 효과 (SINGLE, UPDATE_ATTACK) / 위치: MZONE
- 효과 2: 적용 효과 (SINGLE, UPDATE_DEFENSE) / 위치: MZONE
- 효과 3: 적용 효과 (FIELD, CANNOT_SELECT_BATTLE_TARGET) / 위치: MZONE
- 효과 4: 적용 효과 (FIELD, CANNOT_BE_TARGET) / 위치: MZONE
  - 미해석: inline function: function(e,c) return c~=e:GetHandler() end
- 효과 5 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Take control of 1 monster your opponent controls
  - 분류: CONTROL
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card41522092:effect5
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 응답 대상: 발동에 체인 / 조건 없음(모든 효과)
  - 발동 위치: MZONE / 처리: CONTROL
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 몬스터 존의 카드 선택 (처리 미해석) [조건: IsControlerCanBeChanged()] → 상대 카드
  - 미해석: inline function: Cost.DetachFromSelf(1)
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로; 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Ryzeal Detonator (34909328) ×3 [엑스트라] (상대 덱 ×3)
- MONSTER+EFFECT+XYZ · LIGHT · PYRO · 랭크 4 · 3000 / 2500 · 카드군 0x1b6
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) [조건: IsSetCard(SET_RYZEAL)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: CHAINING / 위치: MZONE
  - 설명: Destroy 1 card on the field
  - 분류: DESTROY
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 응답 대상: 발동에 체인 / 상대 효과만 / 그 외 조건 없음
  - 발동 위치: MZONE / 처리: DESTROY, DESTROY@ONFIELD
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 필드의 카드 파괴 → 조건에 맞는 아무 카드
  - 미해석: inline function: function(e,tp,eg,ep,ev,re,r,rp) return rp==1-tp end
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 2 [설명 1]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Attach 1 monster from your GY to this card as material
  - 분류: LEAVE_GRAVE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 34909328#0
  - 발동 위치: MZONE / 처리: LEAVE_GRAVE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsMonster() and IsCanBeXyzMaterial(xyzc,tp,REASON_EFFECT)] → 이 덱에서: Nibiru, the Primal Being, Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia, Ash Blossom & Joyous Spring, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
- 효과 3: 지속 처리 (DESTROY_REPLACE) / 위치: MZONE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 34909328#1
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 전투: 전투 파괴 대체 (자기 쪽 몬스터: s.repfilter ; c:IsLocation(LOCATION_MZONE) and c:IsControler(tp) and c:IsType(TYPE_XYZ) and c:IsFaceup() and c:IsReason(REASON_BATTLE|REASON_EFFECT) and not c:IsReason(REASON_REPLACE)), 소재가 있을 때
  - 미해석: inline function: function(e,tp) e:GetHandler():RemoveOverlayCard(tp,1,1,REASON_EFFECT) end
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로; 효과 4: 존재 조건 (몬스터 존에 있어야 함); 효과 4: 몬스터 존의 카드 선택 (처리 미해석)
  - Ryzeal Plasma Hole — 효과 1: 존재 조건 (몬스터 존에 있어야 함); 효과 1: 묘지에서 엑시즈 소재로; 효과 1: 몬스터 존에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Infernal Flame Banshee — 효과 2: 존재 조건 (몬스터 존에 있어야 함)
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Ryzeal Duo Drive (7511613) ×2 [엑스트라] (상대 덱 ×2)
- MONSTER+EFFECT+XYZ · FIRE · THUNDER · 랭크 4 · 2500 / 2000 · 카드군 0x1b6
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (FIELD, UPDATE_ATTACK) / 위치: MZONE
- 효과 2 [설명 0]: 유발 효과 (임의) / 계기: SPSUMMON_SUCCESS
  - 설명: Attach 1 "Ryzeal" monster from your GY to this card as material
  - 분류: LEAVE_GRAVE
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 7511613#0
  - 발동 위치: MZONE / 처리: LEAVE_GRAVE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 묘지에서 엑시즈 소재로 [조건: IsSetCard(SET_RYZEAL) and IsMonster() and IsCanBeXyzMaterial(xyzc,tp,REASON_EFFECT)] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Detonator, Ryzeal Duo Drive
- 효과 3 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Add 2 "Ryzeal" cards from your Deck to your hand
  - 분류: TOHAND, SEARCH
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 7511613#1
  - 엑시즈 소재 제거: 2개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE / 처리: TOHAND, SEARCH, TOHAND@DECK
  - 막을 수 있는 상대 카드: Ash Blossom & Joyous Spring ×2(무효), Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 덱에서 패에 넣기 [조건: IsSetCard(SET_RYZEAL) and IsAbleToHand()] → 이 덱에서: Star Ryzeal, Ice Ryzeal, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Ryzeal Plugin, Ryzeal Cross, Ryzeal Plasma Hole
- 이 카드를 불러오는 카드:
  - Sword Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함); 효과 2: 묘지에서 특수 소환
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Cross — 효과 3: 묘지에서 덱으로; 효과 4: 존재 조건 (몬스터 존에 있어야 함); 효과 4: 몬스터 존의 카드 선택 (처리 미해석)
  - Ryzeal Plasma Hole — 효과 1: 존재 조건 (몬스터 존에 있어야 함); 효과 1: 묘지에서 엑시즈 소재로; 효과 1: 몬스터 존에서 엑시즈 소재로; 효과 2: 몬스터 존의 카드를 소재로 엑시즈 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Duo Drive — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Super Starslayer TY-PHON - Sky Crisis (93039339) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · DARK · FIEND · 랭크 12 · 2900 / 2900
- 소환 조건: 엑시즈 소재 (레벨 12 × 2장) → 이 덱에서: (없음)
- 효과 1: 적용 효과 (FIELD, CANNOT_ACTIVATE) / 위치: MZONE
  - 미해석: inline function: function(e) return e:GetHandler():IsXyzSummoned() end
- 효과 2 [설명 1]: 기동 효과 / 위치: MZONE
  - 설명: Return 1 monster on the field to the hand
  - 분류: TOHAND
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card93039339:effect2
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE / 처리: TOHAND, TOHAND@MZONE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 몬스터 존에서 패에 넣기 [조건: IsAbleToHand()] → 조건에 맞는 아무 카드
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 효과 3: 지속 처리 (SPSUMMON_SUCCESS) / 계기: SPSUMMON_SUCCESS
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로; 효과 2: 부재 조건 (몬스터 존에 없어야 함)
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Tornado Dragon (6983839) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · WIND · WYRM · 랭크 4 · 2100 / 2000
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Destroy S&T
  - 분류: DESTROY
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card6983839:effect1
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE / 처리: DESTROY, DESTROY@ONFIELD
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 효과: 필드의 카드 파괴 [조건: IsSpellTrap()] → 이 덱에서: Seventh Tachyon, Triple Tactics Talent, Pot of Prosperity, Bonfire, Lightning Crash, Ryzeal Plugin, Crossout Designator, Ryzeal Cross, Infinite Impermanence, Gravedigger's Trap Hole, Ryzeal Plasma Hole
  - 미해석: inline function: Cost.Detach(1,1,nil)
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

## Traptrix Rafflesia (6511113) ×1 [엑스트라] (상대 덱 ×1)
- MONSTER+EFFECT+XYZ · EARTH · PLANT · 랭크 4 · 300 / 2500 · 카드군 0x108a
- 소환 조건: 엑시즈 소재 (레벨 4 × 2장) → 이 덱에서: Star Ryzeal, Ice Ryzeal, Skull Meister, Sword Ryzeal, Node Ryzeal, Ext Ryzeal, Sakitama, Mulcharmy Fuwalos, Mulcharmy Purulia
- 효과 1: 적용 효과 (SINGLE, IMMUNE_EFFECT) / 위치: MZONE
- 효과 2: 적용 효과 (FIELD, INDESTRUCTABLE_BATTLE) / 위치: MZONE
  - 전투: 전투로 파괴되지 않음 (자기 쪽 몬스터: s.imtg ; c:IsSetCard(SET_TRAPTRIX) and not c:IsCode(id))
- 효과 3: 적용 효과 (FIELD, INDESTRUCTABLE_EFFECT) / 위치: MZONE
- 효과 4: 적용 효과 (FIELD, CANNOT_BE_TARGET) / 위치: MZONE
- 효과 5 [설명 0]: 유발 즉시 효과 / 계기: FREE_CHAIN / 위치: MZONE
  - 설명: Copy the effect of 1 "Hole" Normal Trap
  - 횟수: 1턴 1번 ('사용' 제약: 발동 무효돼도 소비), 키 card6511113:effect5
  - 엑시즈 소재 제거: 1개 (필드에 그만큼 소재가 있어야 함)
  - 발동 위치: MZONE
  - 막을 수 있는 상대 카드: Ghost Ogre & Snow Rabbit ×1(방해), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Ryzeal Detonator ×3(방해), Number 104: Masquerade ×1(무효), Number F0: Utopic Future Zexal ×1(방해)
  - 코스트: 코스트: 덱에서 묘지로 [조건: IsNormalTrap() and IsAbleToGraveAsCost() and (IsSetCard(SET_TRAP_HOLE) or IsSetCard(SET_HOLE))] → 이 덱에서: Gravedigger's Trap Hole, Ryzeal Plasma Hole
- 이 카드를 불러오는 카드:
  - Node Ryzeal — 효과 1: 존재 조건 (몬스터 존/묘지에 있어야 함)
  - Ext Ryzeal — 효과 1: 존재 조건 (엑스트라 덱에 있어야 함); 효과 1: 코스트: 엑스트라 덱에서 묘지로
  - Ryzeal Plugin — 효과 1: 묘지/제외에서 특수 소환; 효과 1: 몬스터 존에서 엑시즈 소재로
  - Ryzeal Plasma Hole — 효과 1: 몬스터 존에서 엑시즈 소재로
  - Mereologic Aggregator — 효과 1: 엑스트라 덱에서 묘지로; 효과 2: 묘지에서 엑시즈 소재로
  - Ryzeal Detonator — 효과 2: 묘지에서 엑시즈 소재로
  - Number F0: Utopic Future Zexal — 소재로 쓰여 소환됨: 엑시즈 소재 (같은 랭크 엑시즈 몬스터 × 2장)
  - Eclipse Twins — 효과 3: 묘지에서 특수 소환

# 가치 계산 시험 (패: 8633261, 묘지: 34909328, 앞면 마법: 없음, 일반 소환 가능)

- 덱에 남은 카드(덱 매수 − 보이는 장수): Nibiru, the Primal Being 1, Star Ryzeal 1, Ice Ryzeal 2, Skull Meister 1, Sword Ryzeal 3, Node Ryzeal 1, Ext Ryzeal 3, Sakitama 2, Mulcharmy Fuwalos 3, Mulcharmy Purulia 2, Ash Blossom & Joyous Spring 2, Ghost Ogre & Snow Rabbit 1, Ghost Belle & Haunted Mansion 1, Maxx "C" 1, Seventh Tachyon 1, Triple Tactics Talent 1, Pot of Prosperity 1, Bonfire 1, Lightning Crash 3, Ryzeal Plugin 1, Crossout Designator 1, Ryzeal Cross 1, Infinite Impermanence 3, Gravedigger's Trap Hole 1, Ryzeal Plasma Hole 1
- γ = 0.7, 가중치 출처: ./WindBot\agent_values.json
- 패: Ice Ryzeal → 최선 110.1 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Eclipse Twins 35) 55×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Node Ryzeal(1) 룰 특수 소환 20×γ^4 → Node Ryzeal(2) 묘지 특수 소환+코스트(패/필드→묘지 0) 35×γ^5 → Ryzeal Detonator(2) 묘지→소재 10×γ^6
  - 일반 소환 시: 110.1 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Eclipse Twins 35) 55×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Node Ryzeal(1) 룰 특수 소환 20×γ^4 → Node Ryzeal(2) 묘지 특수 소환+코스트(패/필드→묘지 0) 35×γ^5 → Ryzeal Detonator(2) 묘지→소재 10×γ^6
  - 다음 턴 후속: 33 = 0.3 × 110.1 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Eclipse Twins 35) 55×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Node Ryzeal(1) 룰 특수 소환 20×γ^4 → Node Ryzeal(2) 묘지 특수 소환+코스트(패/필드→묘지 0) 35×γ^5 → Ryzeal Detonator(2) 묘지→소재 10×γ^6

## 턴 계획 (흐름 + 턴 종료 방해 − 효율)
- 최선 계획: 364.8 (흐름 132.2 + 턴 종료 방해 186.9 + 후속 45.7 − 효율 1장) 행동: Ice Ryzeal 일반 소환 → Ice Ryzeal 유발(2) → Sword Ryzeal → Sword Ryzeal 유발(2) → Star Ryzeal → Star Ryzeal 룰 특수 소환 [떼기: Ryzeal Detonator] → Star Ryzeal 유발(2) → Ryzeal Cross → Ryzeal Duo Drive 엑시즈 소환 [소재: Star Ryzeal, Ice Ryzeal] → Ryzeal Duo Drive 유발(2) +소재: Ryzeal Detonator → Ryzeal Duo Drive 기동(3) [떼기: Star Ryzeal, Ice Ryzeal] → Ext Ryzeal → Ext Ryzeal 룰 특수 소환 [코스트: Eclipse Twins] → Ryzeal Detonator 엑시즈 소환 [소재: Sword Ryzeal, Ext Ryzeal] → Ryzeal Detonator 유발(2) +소재: Eclipse Twins → Ryzeal Cross 세트 카드 발동 → Ryzeal Cross 기동(3) [덱으로: Star Ryzeal, Ryzeal Detonator] +드로우 | 방해: Ryzeal Cross, Number F0: Utopic Future Zexal(5), Ryzeal Detonator(1) ×3, Ryzeal Detonator(3), Eclipse Twins(Ryzeal Detonator 소재, 상대 턴 묘지 효과)
- 아무것도 안 하고 턴 종료: 63.2 (흐름 0 + 턴 종료 방해 30.2 + 후속 33 − 효율 0장) | 방해: Number F0: Utopic Future Zexal(5)
  - 첫 행동 Ice Ryzeal 일반 소환 → 364.8
- (탐색 예산 소진: 일부 경로만 확인)

## 덱에 대상이 없어 0점인 효과 (서치·특수 소환·세트·덱에서 사용)
- 없음

## 카드 가치 (카드 선택 시, 덱 잔량 반영)
- Nibiru, the Primal Being: 40
- Star Ryzeal: 50
- Ice Ryzeal: 60
- Skull Meister: 35
- Sword Ryzeal: 50
- Node Ryzeal: 55
- Ext Ryzeal: 50
- Sakitama: 0
- Mulcharmy Fuwalos: 65
- Mulcharmy Purulia: 55
- Ash Blossom & Joyous Spring: 47
- Ghost Ogre & Snow Rabbit: 35.1
- Ghost Belle & Haunted Mansion: 34
- Maxx "C": 75
- Seventh Tachyon: 30
- Triple Tactics Talent: 0
- Pot of Prosperity: 0
- Bonfire: 30
- Lightning Crash: 30
- Ryzeal Plugin: 45
- Crossout Designator: 32
- Ryzeal Cross: 53
- Infinite Impermanence: 53
- Gravedigger's Trap Hole: 30.4
- Ryzeal Plasma Hole: 40
- Super Starslayer TY-PHON - Sky Crisis: 17.5
- Mereologic Aggregator: 52
- Ryzeal Detonator: 60.1
- Number 104: Masquerade: 18.1
- Infernal Flame Banshee: 30
- Ryzeal Duo Drive: 40
- Number F0: Utopic Future Zexal: 30.2
- Eclipse Twins: 35
- Full Armored Utopic Ray Lancer: 9.5
- Tornado Dragon: 15
- Evilswarm Exciton Knight: 12.5
- Traptrix Rafflesia: 65

## 엑스트라 덱 코스트 순서 (낮을수록 먼저 묘지로, 상대 앞면 -1장)
- Eclipse Twins: -3489.64
- Mereologic Aggregator: -3189.47
- Ryzeal Duo Drive: 0.41
- Ryzeal Detonator: 0.61
- Full Armored Utopic Ray Lancer: 10.11
- Evilswarm Exciton Knight: 10.14
- Tornado Dragon: 10.16
- Super Starslayer TY-PHON - Sky Crisis: 10.19
- Number 104: Masquerade: 10.19
- Infernal Flame Banshee: 10.31
- Number F0: Utopic Future Zexal: 10.31
- Traptrix Rafflesia: 10.66

## 묘지 이득 (이번 턴 사용 후 → 이번 턴 / 상대 턴 / 다음 턴, 원값 × 시점 가중치)
- Mereologic Aggregator (3) 키 9940036#2 상대 카드 대상: 원값 32 → × / 21.4 / 9.6
- Eclipse Twins (3) 키 45852939#0: 원값 35 → × / 23.5 / 10.5

# 미끼 시험 (상대 숨은 카드 40장, 손패 -1장, 세트 0장, 본 카드 없음)

| 카드 | 효과 | 가치(필드) | 막힐 확률 | 막을 수 있는 상대 카드 |
|---|---|---|---|---|
| Ash Blossom & Joyous Spring | 1 | 32 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Crossout Designator | 1 | 32 | 0 | Crossout Designator ×1(무효, 이름 선언) |
| Evilswarm Exciton Knight | 1 | 12.5 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Full Armored Utopic Ray Lancer | 2 | 9.5 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ghost Belle & Haunted Mansion | 1 | 19 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Ghost Ogre & Snow Rabbit | 1 | 20.1 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Gravedigger's Trap Hole | 1 | 30.4 | 0 | Crossout Designator ×1(무효, 이름 선언) |
| Infinite Impermanence | 1 | 38 | 0 | Crossout Designator ×1(무효, 이름 선언) |
| Maxx "C" | 1 | 60 | 0 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mereologic Aggregator | 3 | 32 | 0 | Skull Meister ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mulcharmy Fuwalos | 1 | 50 | 0 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mulcharmy Purulia | 1 | 40 | 0 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Nibiru, the Primal Being | 1 | 25 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Number 104: Masquerade | 1 | 18.1 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Number F0: Utopic Future Zexal | 5 | 30.2 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ryzeal Detonator | 1 | 20.1 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ryzeal Plasma Hole | 1 | 30 | 0 | Crossout Designator ×1(무효, 이름 선언) |
| Skull Meister | 1 | 20 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Tornado Dragon | 1 | 15 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Traptrix Rafflesia | 5 | 65 | 0 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |

