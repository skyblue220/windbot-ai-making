- 상대 드로우 감시: Mulcharmy Fuwalos: 상대 특수 소환 (DECK·EXTRA에서)마다 1장 드로우, 엔드 페이즈에 패가 (상대 필드 + 6)장을 넘으면 덱으로
# 가치 계산 시험 (패: 8633261,34022970,67972302,89753095,25311006, 묘지: 없음, 앞면 마법: 없음, 일반 소환 가능)

- 덱에 남은 카드(덱 매수 − 보이는 장수): Nibiru, the Primal Being 1, Star Ryzeal 1, Ice Ryzeal 2, Skull Meister 1, Sword Ryzeal 3, Node Ryzeal 1, Ext Ryzeal 2, Sakitama 1, Mulcharmy Fuwalos 3, Mulcharmy Purulia 2, Ash Blossom & Joyous Spring 2, Ghost Ogre & Snow Rabbit 1, Ghost Belle & Haunted Mansion 1, Maxx "C" 1, Seventh Tachyon 1, Pot of Prosperity 1, Bonfire 1, Lightning Crash 2, Ryzeal Plugin 1, Crossout Designator 1, Ryzeal Cross 1, Infinite Impermanence 3, Gravedigger's Trap Hole 1, Ryzeal Plasma Hole 1
- γ = 0.7, 가중치 출처: ./WindBot\agent_values.json
- 패: Ice Ryzeal → 최선 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4
  - 일반 소환 시: 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4
  - 다음 턴 후속: 30.5 = 0.3 × 101.6 = Ice Ryzeal(2) 덱 특수 소환 40 → Sword Ryzeal(2) 서치 30×γ^1 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^2 → Ext Ryzeal(2) 서치 30×γ^3 → Sword Ryzeal(1) 룰 특수 소환 20×γ^4
- 패: Ext Ryzeal → 최선 112.8 = Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52 → Ext Ryzeal(2) 서치 30×γ^1 → Sword Ryzeal(1) 룰 특수 소환 20×γ^2 → Sword Ryzeal(2) 서치 30×γ^3 → Ice Ryzeal(2) 덱 특수 소환 40×γ^4 → Star Ryzeal(2) 덱 세트 30×γ^5 → Ryzeal Cross(0) 상대 턴 방해 43×γ^6
  - 일반 소환 시: 76.5 = Ext Ryzeal(2) 서치 30 → Sword Ryzeal(1) 룰 특수 소환 20×γ^1 → Sword Ryzeal(2) 서치 30×γ^2 → Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52×γ^3
  - 다음 턴 후속: 33.8 = 0.3 × 112.8 = Ext Ryzeal(1) 룰 특수 소환+코스트(엑스트라→묘지: Mereologic Aggregator 32) 52 → Ext Ryzeal(2) 서치 30×γ^1 → Sword Ryzeal(1) 룰 특수 소환 20×γ^2 → Sword Ryzeal(2) 서치 30×γ^3 → Ice Ryzeal(2) 덱 특수 소환 40×γ^4 → Star Ryzeal(2) 덱 세트 30×γ^5 → Ryzeal Cross(0) 상대 턴 방해 43×γ^6
- 패: Sakitama → 최선 0
- 패: Triple Tactics Talent → 최선 0
- 패: Lightning Crash → 최선 30 = Lightning Crash(1) 서치 30
  - 다음 턴 후속: 9 = 0.3 × 30 = Lightning Crash(1) 서치 30

## 턴 계획 (흐름 + 턴 종료 방해 − 효율)
- 최선 계획: 392.7 (흐름 126.4 + 턴 종료 방해 173.2 + 후속 118.1 − 효율 5장 − 상대 드로우 1장 25) 행동: Ext Ryzeal 룰 특수 소환 → Ext Ryzeal 유발(2) → Sword Ryzeal → Ice Ryzeal 일반 소환 → Ice Ryzeal 유발(2) → Sword Ryzeal → Sword Ryzeal 유발(2) → Ext Ryzeal → Sakitama 기동(2) → Sakitama 추가 일반 소환 → Lightning Crash 발동(1) → Sword Ryzeal → Triple Tactics Talent 발동(1) → Sword Ryzeal 룰 특수 소환 → Ryzeal Detonator 엑시즈 소환 [소재: Ext Ryzeal, Sword Ryzeal] → Infernal Flame Banshee 엑시즈 소환 [소재: Sword Ryzeal, Ice Ryzeal] → Infernal Flame Banshee 기동(1) [떼기: Sword Ryzeal] → Star Ryzeal → Star Ryzeal 룰 특수 소환 [떼기: Ice Ryzeal] → Star Ryzeal 유발(2) → Ryzeal Cross → Ryzeal Duo Drive 엑시즈 소환 [소재: Star Ryzeal, Sakitama] → Ryzeal Cross 세트 카드 발동 → Ryzeal Duo Drive 기동(3) [떼기: Star Ryzeal, Sakitama] → Ryzeal Plasma Hole → Ryzeal Cross 기동(3) [덱으로: Sword Ryzeal, Star Ryzeal] +드로우 | 방해: Ryzeal Plasma Hole(세트 ×2), Ryzeal Cross, Ryzeal Detonator(1) ×2, Ryzeal Detonator(3)
- 아무것도 안 하고 턴 종료: 73.3 (흐름 0 + 턴 종료 방해 0 + 후속 73.3 − 효율 0장)
  - 첫 행동 Ext Ryzeal 룰 특수 소환 → 392.7
  - 첫 행동 Ice Ryzeal 일반 소환 → 391.2
  - 첫 행동 Lightning Crash 발동(1) → Sword Ryzeal → 365
  - 첫 행동 Sakitama 기동(2) → Sakitama 추가 일반 소환 → 358.4
  - 첫 행동 Triple Tactics Talent 발동(1) → 345.5
  - 첫 행동 Lightning Crash 발동(1) → Node Ryzeal → 325.6
  - 첫 행동 Ext Ryzeal 일반 소환 → 316.4
  - 첫 행동 Ice Ryzeal 룰 특수 소환 [코스트: Sakitama] → 279.2
  - 첫 행동 Sakitama 일반 소환 → 274.1
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
- Eclipse Twins: -1739.64
- Mereologic Aggregator: -1589.47
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

# 미끼 시험 (상대 숨은 카드 40장, 손패 9장, 세트 0장, 본 카드 없음)

| 카드 | 효과 | 가치(필드) | 막힐 확률 | 막을 수 있는 상대 카드 |
|---|---|---|---|---|
| Ash Blossom & Joyous Spring | 1 | 32 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Crossout Designator | 1 | 32 | 0.23 | Crossout Designator ×1(무효, 이름 선언) |
| Evilswarm Exciton Knight | 1 | 12.5 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Full Armored Utopic Ray Lancer | 2 | 9.5 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ghost Belle & Haunted Mansion | 1 | 19 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Ghost Ogre & Snow Rabbit | 1 | 20.1 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Gravedigger's Trap Hole | 1 | 30.4 | 0.23 | Crossout Designator ×1(무효, 이름 선언) |
| Infinite Impermanence | 1 | 38 | 0.23 | Crossout Designator ×1(무효, 이름 선언) |
| Maxx "C" | 1 | 60 | 0.55 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mereologic Aggregator | 3 | 32 | 0.4 | Skull Meister ×1(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mulcharmy Fuwalos | 1 | 50 | 0.55 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Mulcharmy Purulia | 1 | 40 | 0.55 | Ash Blossom & Joyous Spring ×2(무효), Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Nibiru, the Primal Being | 1 | 25 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Number 104: Masquerade | 1 | 18.1 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Number F0: Utopic Future Zexal | 5 | 30.2 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ryzeal Detonator | 1 | 20.1 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Ryzeal Plasma Hole | 1 | 30 | 0.23 | Crossout Designator ×1(무효, 이름 선언) |
| Skull Meister | 1 | 20 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Gravedigger's Trap Hole ×1(무효), Number 104: Masquerade ×1(무효) |
| Tornado Dragon | 1 | 15 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |
| Traptrix Rafflesia | 5 | 65 | 0.23 | Crossout Designator ×1(무효, 이름 선언), Ryzeal Cross ×1(무효), Infinite Impermanence ×3(무효), Number 104: Masquerade ×1(무효) |

