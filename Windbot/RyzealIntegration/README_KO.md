# Ryzeal 통합 소스

현재 배포 폴더에는 WindBot 실행 파일만 있고 WindBot 솔루션/소스 프로젝트가 없습니다. 따라서 제공된 Step11 C# 파일은 `Step11`에 소스 형태로 보관했고, 실행 중인 `WindBot.exe`, `ExecutorBase.dll`, 기존 `bots.json`은 덮어쓰지 않았습니다.

- 기존 `Windbot/Decks/AI_Ryzeal.ydk`와 `bots.json`의 `Ryzeal` 항목은 이미 존재합니다.
- 실제 Ryzeal AI 구현은 `Step11`입니다. 호스트 WindBot 소스에서 기존 Ryzeal executor 관련 클래스를 교체하고, 포함된 `GameAI.cs` 또는 패치를 해당 호스트 버전에 맞게 통합한 뒤 WindBot 솔루션을 빌드해야 합니다.
- `Step11/GameAI.cs`에는 ZIP의 잘못된 괄호 배치를 바로잡은 내용이 있습니다. 호스트 소스가 다르면 이 파일을 통째로 덮어쓰지 말고 `GameAI.integration.patch`를 기준으로 통합하세요.
- `Stage01Probe`는 별도의 로딩 확인용 더미 executor이며 Step11의 플레이 정책을 실행하지 않습니다. 빌드된 프로브 DLL은 `Windbot/Executors`에 배치했고 전용 항목을 기존 `bots.json` 끝에 병합했습니다.

Step11 패키지에 기재된 카드 ID placeholder와 미완성/휴리스틱 로직은 그대로 남아 있습니다. 프로브는 컴파일했지만 실제 WindBot 로딩은 실행 확인이 필요하고, Step11 빌드 및 듀얼 동작 검증은 실제 WindBot 소스 솔루션에서 수행해야 합니다.
