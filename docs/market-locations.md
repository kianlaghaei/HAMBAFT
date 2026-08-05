# Market locations

`presentation/locations.json` defines 13 Hezar Cheragh locations. Coordinates are presentation percentages and never authoritative game positions.

| ID | Persian identity | Gameplay role |
| --- | --- | --- |
| market-entrance | ورودی بازار | first guided stop and arrival context |
| central-crossroads | چهارراه مرکزی | messenger/news distribution |
| clock-courtyard | حیاط ساعت | public gathering and shared attention |
| haj-sadegh-office | دفتر حاج صادق | absence evidence; never a walking Haj marker |
| bakery-sepideh | نانوایی سپیده | queue, oven, flour and customer trust |
| logistics-rah-no | باربری راه نو | carts, routes, cargo and readiness |
| printing-roshan | چاپخانه روشن | press, paper, notices and source confidence |
| exchange-mizan | صرافی میزان | clients, ledgers, guarantees and cash boxes |
| north-gate | دروازه شمالی | cargo arrival and delay |
| caravanserai | کاروانسرا | drivers and spare capacity context |
| warehouse-alley | کوچه انبارها | storage and blocked supply route |
| old-water-reservoir | آب‌انبار قدیمی | authored night/crisis context |
| avan-temporary-office | دفتر موقت آوان | visible proposal presence, never implicit ownership |

Each projection contains display name, compact identity, relevance, current public condition, visible presence, recent change, Team-authorized actions and presentation tags. The default UI exposes only hotspot label/state; selection moves focus to the compact context panel. The equivalent location list supports keyboard, screen reader, reduced-motion and Pixi failure.

## Guided opening

First Team entry follows entrance -> crossroads -> courtyard -> closed Haj office -> controlled business. Each high-motion stop lasts four seconds (about 20 seconds total), is skippable, and is recorded only in local session presentation state. Reduced-motion shows the same route as a compact static checklist. Refresh restores the current Backend scene; the tour does not rewind the narrative clock.
