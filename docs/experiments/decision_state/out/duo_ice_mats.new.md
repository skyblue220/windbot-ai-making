## 판단 입력 (모든 판단이 읽는 상태)
- 우리: LP 8000, 덱 모름, 일반 소환 가능
  - 패: Ice Ryzeal, Star Ryzeal
  - 몬스터: Ryzeal Duo Drive (소재 2: Ice Ryzeal, Ice Ryzeal)
  - 마법·함정: 없음
  - 묘지: 없음 / 제외: 없음
  - 엑스트라 덱(덱 리스트 − 보이는 카드): Super Starslayer TY-PHON - Sky Crisis, Mereologic Aggregator, Ryzeal Detonator ×3, Number 104: Masquerade, Infernal Flame Banshee, Ryzeal Duo Drive, Number F0: Utopic Future Zexal, Eclipse Twins, Full Armored Utopic Ray Lancer, Tornado Dragon, Evilswarm Exciton Knight, Traptrix Rafflesia
  - 덱 남은 카드(덱 리스트 − 보이는 카드, 소재 포함): Nibiru, the Primal Being, Skull Meister, Sword Ryzeal ×3, Node Ryzeal, Ext Ryzeal ×3, Sakitama ×2, Mulcharmy Fuwalos ×3, Mulcharmy Purulia ×2, Ash Blossom & Joyous Spring ×2, Ghost Ogre & Snow Rabbit, Ghost Belle & Haunted Mansion, Maxx "C", Seventh Tachyon, Triple Tactics Talent, Pot of Prosperity, Bonfire, Lightning Crash ×3, Ryzeal Plugin, Crossout Designator, Ryzeal Cross, Infinite Impermanence ×3, Gravedigger's Trap Hole, Ryzeal Plasma Hole
- 상대: LP 8000, 앞면 -1장, 패 모름, 덱 모름, 엑스트라 모름, 비공개 모름
  - 묘지: 없음 / 제외: 없음

- 덱에 남은 카드(덱 매수 − 보이는 장수): Nibiru, the Primal Being 1, Skull Meister 1, Sword Ryzeal 3, Node Ryzeal 1, Ext Ryzeal 3, Sakitama 2, Mulcharmy Fuwalos 3, Mulcharmy Purulia 2, Ash Blossom & Joyous Spring 2, Ghost Ogre & Snow Rabbit 1, Ghost Belle & Haunted Mansion 1, Maxx "C" 1, Seventh Tachyon 1, Triple Tactics Talent 1, Pot of Prosperity 1, Bonfire 1, Lightning Crash 3, Ryzeal Plugin 1, Crossout Designator 1, Ryzeal Cross 1, Infinite Impermanence 3, Gravedigger's Trap Hole 1, Ryzeal Plasma Hole 1
- γ = 0.7, 가중치 출처: ./WindBot\agent_values.json
- 패: Star Ryzeal → 최선 60.1 = Star Ryzeal(1) 룰 특수 소환+소재 제거 1 18 → Star Ryzeal(2) 덱 세트 30×γ^1 → Ryzeal Cross(0) 상대 턴 방해 43×γ^2
  - 다음 턴 후속: 18 = 0.3 × 60.1 = Star Ryzeal(1) 룰 특수 소환+소재 제거 1 18 → Star Ryzeal(2) 덱 세트 30×γ^1 → Ryzeal Cross(0) 상대 턴 방해 43×γ^2
- 패: Ice Ryzeal → 최선 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4
  - 일반 소환 시: 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4
  - 다음 턴 후속: 30.5 = 0.3 × 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4

## 턴 계획 (흐름 + 턴 종료 방해 − 효율)
- 최선 계획: 444.8 (흐름 132.2 + 턴 종료 방해 259.6 + 후속 53 − 효율 2장) 행동: Star Ryzeal 룰 특수 소환 [떼기: Ice Ryzeal] → Star Ryzeal 유발(2) → Ryzeal Plasma Hole → Ice Ryzeal 일반 소환 → Ice Ryzeal 유발(2) → Sword Ryzeal → Sword Ryzeal 유발(2) → Ext Ryzeal → Ext Ryzeal 룰 특수 소환 [코스트: Mereologic Aggregator] → Ryzeal Detonator 엑시즈 소환 [소재: Star Ryzeal, Ice Ryzeal] → Ryzeal Detonator 유발(2) +소재: Mereologic Aggregator → Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Star Ryzeal] → Ryzeal Cross → Ryzeal Cross 필드 마법 발동 → Ryzeal Cross 기동(3) [덱으로: Star Ryzeal, Ice Ryzeal] +드로우 → Traptrix Rafflesia 엑시즈 소환 [소재: Sword Ryzeal, Ext Ryzeal] | 방해: Ryzeal Plasma Hole ×2, Ryzeal Cross, Ryzeal Detonator(1) ×2, Ryzeal Detonator(3), Traptrix Rafflesia(5), Mereologic Aggregator(Ryzeal Detonator 소재, 상대 턴 묘지 효과)
- 아무것도 안 하고 턴 종료: 48.5 (흐름 0 + 턴 종료 방해 0 + 후속 48.5 − 효율 0장)
  - 첫 행동 Star Ryzeal 룰 특수 소환 [떼기: Ice Ryzeal] → 444.8
  - 첫 행동 Ice Ryzeal 일반 소환 → 443.7
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Ryzeal Cross → 432.8
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Ryzeal Plasma Hole → 416
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Ext Ryzeal → 398.6
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Ryzeal Plugin → 394.3
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Node Ryzeal → 384.2
  - 첫 행동 Ryzeal Duo Drive 기동(3) [떼기: Ice Ryzeal, Ice Ryzeal] → Sword Ryzeal → 371.5
  - 첫 행동 Ice Ryzeal 룰 특수 소환 [코스트: Star Ryzeal] → 302.5
  - 첫 행동 Star Ryzeal 일반 소환 → 284.5
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
- Mereologic Aggregator: -3189.47
- Eclipse Twins: -2334.64
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

