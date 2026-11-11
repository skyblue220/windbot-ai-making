# 체인 중 제거 시험 (상대 턴, 상대 필드: 7511613:1, 체인: Ryzeal Duo Drive 효과 3, 우리 Ryzeal 엑시즈 1장, 아껴 둘 값 방식 reserve, 남은 턴 1, 상대 패 5)

## 지금 없애면 상대가 잃는 값

- Ryzeal Duo Drive (체인: 효과 3, 마지막): 10 (몸체 10; 이번 턴 남은 효과 없음, 체인 위 효과는 그대로 처리됨)

## 우리 제거 효과: 발동 / 보류

- Ghost Ogre & Snow Rabbit 효과 1 [발동한 그 카드, 상대 카드만, 파괴, 상대 발동에 응답]: 보류: 얻는 값 10 vs 아껴 둘 값 35.1 (최대 1장) → Ryzeal Duo Drive 10
- Ryzeal Detonator 효과 1 [양쪽, 대상 지정, 파괴, 상대 발동에 응답]: 보류: 얻는 값 10 vs 아껴 둘 값 20.1 (최대 1장) → Ryzeal Duo Drive 10
- Ryzeal Plasma Hole 효과 1 [앞면, 상대 카드만, 대상 지정, 파괴, 프리 체인]: 보류: 얻는 값 10 vs 아껴 둘 값 30 (최대 1장) → Ryzeal Duo Drive 10
- Tornado Dragon 효과 1 [마법·함정, 양쪽, 대상 지정, 파괴, 프리 체인]: 보류: 얻는 값 0 vs 아껴 둘 값 15 (칠 카드 없음)

## 상대 턴 엑시즈 소환 (Plasma Hole ② 등, 우리 필드: 없음)

- Ryzeal Plasma Hole 효과 2: 만들 수 있는 엑시즈 없음 → 패스

## Lua에서 읽은 효과 발동 봉인 (공격력 기준)

- Super Starslayer TY-PHON - Sky Crisis 효과 1: 공격력 3000 이상 몬스터 효과 발동 불가 (양쪽)
  - 상대 덱 몬스터 40장 중 해당 4장: Nibiru, the Primal Being 3000, Ryzeal Detonator 3000
  - 우리 덱에서 같이 막히는 몬스터: Nibiru, the Primal Being 3000, Ryzeal Detonator 3000

## Lua에서 읽은 보호 (대상 지정 불가 / 파괴 불가 / 응답으로 자기 파괴 내성)

- Number F0: Utopic Future Zexal 효과 4: 효과 대상 불가 (자기 필드의 다른 카드) [MZONE]
- Number F0: Utopic Future Zexal 효과 5: 처리 시 자신 효과 파괴 내성 (응답 보호, 소재 1)
- Traptrix Rafflesia: 보호 대상 필터 미해석: EFFECT_INDESTRUCTABLE_EFFECT return c:IsSetCard(SET_TRAPTRIX) and not c:IsCode(id)
- Traptrix Rafflesia: 보호 대상 필터 미해석: EFFECT_CANNOT_BE_EFFECT_TARGET return c:IsSetCard(SET_TRAPTRIX) and not c:IsCode(id)

## Lua에서 읽은 '효과 처리에 발동한 카드가 필요한가' (SourceStop)와 제거 범위

- Crossout Designator 효과 1: 카드 없어도 처리 / 제거 범위: 양쪽
- Eclipse Twins 효과 2: 카드 필요 part
- Evilswarm Exciton Knight 효과 1: 카드 필요 part
- Ghost Ogre & Snow Rabbit 효과 1: 카드 없어도 처리 / 제거 범위: 발동한 그 카드, 상대 카드만, 파괴
- Infernal Flame Banshee 효과 2: 카드 필요 part
- Infinite Impermanence 효과 1: 카드 필요 part
- Mereologic Aggregator 효과 2: 카드 필요 all
- Nibiru, the Primal Being 효과 1: 카드 필요 all
- Number F0: Utopic Future Zexal 효과 5: 카드 필요 part
- Ryzeal Cross 효과 3: 카드 없어도 처리 / 제거 범위: 양쪽, 대상 지정
- Ryzeal Detonator 효과 1: 카드 없어도 처리 / 제거 범위: 양쪽, 대상 지정, 파괴
- Ryzeal Detonator 효과 2: 카드 필요 all
- Ryzeal Duo Drive 효과 2: 카드 필요 all
- Ryzeal Plasma Hole 효과 1: 카드 없어도 처리 / 제거 범위: 앞면, 상대 카드만, 대상 지정, 파괴
- Sakitama 효과 3: 카드 없어도 처리 / 제거 범위: 양쪽, 대상 지정
- Super Starslayer TY-PHON - Sky Crisis 효과 2: 카드 없어도 처리 / 제거 범위: 양쪽
- Tornado Dragon 효과 1: 카드 없어도 처리 / 제거 범위: 마법·함정, 양쪽, 대상 지정, 파괴

