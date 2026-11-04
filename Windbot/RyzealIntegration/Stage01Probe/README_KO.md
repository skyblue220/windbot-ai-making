# 외부 DLL 단계 01: 로딩/인스턴스 생성 확인

이 패키지는 플레이 AI가 아닙니다. 배포본 ExecutorBase.dll을 직접 참조하는 최소 확장입니다. 기존 Step11 .cs 파일은 이번 단계에 사용하지 않습니다.

1. 이 폴더는 현재 프로젝트의 `Windbot/RyzealIntegration/Stage01Probe`에 있습니다.
2. Windows에서 build.cmd를 실행합니다. 실패하면 오류 전체를 전달하세요. 대안: Visual Studio 개발자 명령 프롬프트에서 `msbuild RyzealProbe.csproj /p:Configuration=Release`.
3. 성공 시 `RyzealResearchProbe.dll`만 `Windbot/Executors/`에 복사합니다. msbuild 결과는 `bin/Release`에 있습니다. 프로젝트는 WindBot 폴더의 현재 `ExecutorBase.dll`을 참조하며, 이를 교체하지 않습니다.
4. `bots.json`은 현재 설치본의 기존 항목을 보존합니다. 프로브 DLL을 설치한 뒤에만 새 항목을 추가하세요: `deck=RyzealResearchProbe`, `name=Ryzeal 연구 - 로딩 확인`, `difficulty=0`, `masterRules=[5]`. ZIP의 `bots.with-probe.json`은 현재 설치본과 다르므로 전체 파일을 덮어쓰지 마세요.
5. 이전에 제공한 실제 덱을 WindBot/Decks/AI_Ryzeal.ydk에 둡니다. 이 패키지는 덱을 임의 생성하지 않습니다.
6. EDOPro를 재시작하고 새 봇을 선택합니다. 실행 인수 Deck=RyzealResearchProbe인지 확인하세요. Deck=Lucky라면 다른 executor입니다.
7. WindBot.exe 옆 logs/ryzeal-plugin-probe.jsonl에 executorConstructed 이벤트가 생기면 DLL 로딩 및 인스턴스 생성 확인입니다. 듀얼을 플레이하는 검증은 아닙니다. 게임은 종료해도 됩니다.

원복: 외부 DLL 제거, bots.json 백업 복원. WindBot.exe/ExecutorBase.dll은 수정하지 않습니다.

공식 소스 확인: SampleExecutor는 .NET Framework 4.0 Library, DefaultExecutor 상속, public 생성자(GameAI,Duel)를 사용합니다. DecksManager는 attribute 첫 인자 Name으로 검색하고 두 번째 File로 덱을 연결합니다. 외부 로딩은 #if !NET 조건 아래에 있으므로 모든 배포 형식이 지원한다고 단정하지 않습니다. 이번 실제 DLL 로딩 테스트로 확인해야 합니다.

주의: 이 프로브는 로딩/인스턴스 생성만 확인하며 Step11 executor의 플레이 정책을 포함하지 않습니다. Step11 소스는 `../Step11`에 별도로 배치되어 있습니다. 프로브 DLL은 현재 WindBot의 `ExecutorBase.dll`을 참조해 빌드했으나 실제 WindBot 로딩은 실행 확인이 필요합니다.
