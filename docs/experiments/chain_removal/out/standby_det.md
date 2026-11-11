# 체인 중 제거 시험 (상대 턴, 상대 필드: 34909328:2,0, 체인: 없음, 우리 Ryzeal 엑시즈 1장)

## 지금 없애면 상대가 잃는 값

- Ryzeal Detonator: 73 (몸체 73; 이번 턴 남은 효과 효과 1 33, 효과 3 30)
- 뒷면 카드: 20 (몸체 20; 뒷면: 기대값)

## 우리 제거 효과: 발동 / 보류

- Ghost Ogre & Snow Rabbit 효과 1 [발동한 그 카드, 상대 카드만, 상대 발동에 응답]: 체인 없음: 발동 불가
- Ryzeal Detonator 효과 1 [양쪽, 상대 발동에 응답]: 체인 없음: 발동 불가
- Ryzeal Plasma Hole 효과 1 [앞면, 상대 카드만, 프리 체인]: 발동: 얻는 값 73 vs 아껴 둘 값 30 (최대 1장) → Ryzeal Detonator 73
- Tornado Dragon 효과 1 [마법·함정, 양쪽, 프리 체인]: 발동: 얻는 값 20 vs 아껴 둘 값 15 (최대 1장) → 뒷면 카드 20

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

