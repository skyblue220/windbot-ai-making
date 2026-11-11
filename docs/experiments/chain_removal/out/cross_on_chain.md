# 체인 중 제거 시험 (우리 턴, 상대 필드: 34909328:2, 체인: Ryzeal Cross 효과 1, 우리 Ryzeal 엑시즈 2장)

## 지금 없애면 상대가 잃는 값

- Ryzeal Detonator: 60.1 (몸체 60.1; 이번 턴 남은 효과 효과 1 20.1, 효과 3 30)
- Ryzeal Cross (체인: 효과 1, 마지막): 53 (몸체 53; 이번 턴 남은 효과 효과 4 43, 체인 위 효과는 그대로 처리됨)

## 우리 제거 효과: 발동 / 보류

- Ghost Ogre & Snow Rabbit 효과 1 [발동한 그 카드, 상대 카드만, 상대 발동에 응답]: 보류: 얻는 값 0 vs 아껴 둘 값 35.1 (칠 카드 없음)
- Ryzeal Detonator 효과 1 [양쪽, 상대 발동에 응답]: 발동: 얻는 값 60.1 vs 아껴 둘 값 20.1 (최대 1장) → Ryzeal Detonator 60.1
- Ryzeal Plasma Hole 효과 1 [앞면, 상대 카드만, 프리 체인]: 발동: 얻는 값 113.1 vs 아껴 둘 값 30 (최대 2장) → Ryzeal Detonator 60.1, Ryzeal Cross 53
- Tornado Dragon 효과 1 [마법·함정, 양쪽, 프리 체인]: 발동: 얻는 값 53 vs 아껴 둘 값 15 (최대 1장) → Ryzeal Cross 53

## Lua에서 읽은 '효과 처리에 발동한 카드가 필요한가' (SourceStop)와 제거 범위

- Crossout Designator 효과 1: 카드 없어도 처리 / 제거 범위: 양쪽
- Eclipse Twins 효과 2: 카드 필요 part
- Evilswarm Exciton Knight 효과 1: 카드 필요 part
- Ghost Ogre & Snow Rabbit 효과 1: 카드 없어도 처리 / 제거 범위: 발동한 그 카드, 상대 카드만
- Infernal Flame Banshee 효과 2: 카드 필요 part
- Infinite Impermanence 효과 1: 카드 필요 part
- Mereologic Aggregator 효과 2: 카드 필요 all
- Nibiru, the Primal Being 효과 1: 카드 필요 all
- Number F0: Utopic Future Zexal 효과 5: 카드 필요 part
- Ryzeal Cross 효과 3: 카드 없어도 처리 / 제거 범위: 양쪽
- Ryzeal Detonator 효과 1: 카드 없어도 처리 / 제거 범위: 양쪽
- Ryzeal Detonator 효과 2: 카드 필요 all
- Ryzeal Duo Drive 효과 2: 카드 필요 all
- Ryzeal Plasma Hole 효과 1: 카드 없어도 처리 / 제거 범위: 앞면, 상대 카드만
- Sakitama 효과 3: 카드 없어도 처리 / 제거 범위: 양쪽
- Super Starslayer TY-PHON - Sky Crisis 효과 2: 카드 없어도 처리 / 제거 범위: 양쪽
- Tornado Dragon 효과 1: 카드 없어도 처리 / 제거 범위: 마법·함정, 양쪽

