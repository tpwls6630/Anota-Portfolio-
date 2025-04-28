# Project Anota (개발중)
소코반 형식의 퍼즐게임입니다.  
플레이어는 상하좌우로 끝까지 이동합니다. 플레이어의 체력보다 적거나 같은 적을 잡아먹어 체력을 늘릴 수 있습니다.  
스테이지에 존재하는 모든 적을 물리치면 승리합니다.  
처형 시스템이 존재하여 특정 턴이 지나면 스테이지에서 가장 체력이 높은 플레이어 혹은 적을 처치시킵니다.    
  
![image](https://github.com/user-attachments/assets/40d1e56e-4f97-4cd8-b860-5e3fd46050b9)

## 🛠️Tech Stack
- Unity 2021.3.16f1
- C#
- Newtonsoft.Json
- InputSystem
    
## ✨내가 구현한 핵심 기능 요약
- [다이얼로그 시스템](#다이얼로그-시스템-개발)
- [퍼즐 데이터(.csv) json 파서](#퍼즐-데이터-json-파서)
- [퍼즐 자료구조 리팩토링 및 맵 에디터 수정](#퍼즐-자료구조-리팩토링-및-맵-에디터-수정)
- [인풋 시스템 마이그레이션](#인풋-시스템-마이그레이션)

## 프로젝트를 통해 배운 점
 PM, 디자이너, 기획자, 프로그래머의 역할 구분이 명확한 게임 프로젝트를 처음 경험하게 되었습니다. 프로그래머는 기획자와 디자이너가 요구하는 스펙을 충분히 만족시킬 수 있도록 노력하는 입장임을 배웠습니다.  
 이미 대부분의 기능이 짜여있는 코드를 전반적으로 분석하고 기존의 시스템에 문제되지 않도록 코드를 짜는 것은 생각보다 어려운 일이었습니다. 본인의 주제를 알고 최대한 조심히, 하지만 창의적으로 일을 하는 경험을 했습니다.  

# Feature
## 다이얼로그 시스템 개발

![image](https://github.com/user-attachments/assets/61cae8e8-5337-4150-9545-e76ba58a3c03)  

Gcode라는 다이얼로그 시스템 언어를 개발했습니다. Gcode 언어와 Gcode 언어를 해독하는 파서, 그리고 Gcode 명령어에 맞게 다이얼로그를 조작하는 실행기를 만들었습니다.  
(Gcode라는 이름은 기계 공학 산업에서 기계 제어 명령어인 Gcode에서 따왔습니다. 취미가 3D printing이라 집에 3D 프린터가 있는데, 3D 프린터를 조작하는 명령어 체계가 Gcode였습니다)  
  
게임의 Localization을 위해 i2L 에셋을 사용합니다. i2L 라이브러리는 텍스트를 key-value 쌍으로 관리하며 에디터에서 key값으로 텍스트를 표현하면 i2L 모듈을 통해 현재 언어 설정에 대응되는 텍스트를 받아올 수 있습니다.  
예를 들어 다이얼로그 대사를 번역하기 위해 i2L 테이블에 key값은 "diatest/101e_001"로, korean value값은 "문 너머에 자리잡고 있던 것은 단 하나의 집 나간 영혼이었습니다."로 표현하면 "diatest/101e_001" key값으로 해당 한국어 텍스트를 화면에 쓸 수 있습니다.  

따라서 하나의 다이얼로그 장면을 구현하기 위해선 i2L 번역 테이블과, 하나의 Gcode가 필요합니다.  
[Gcode 문법(노션)](https://grand-timimus-dc9.notion.site/Gcode-1e3b21ed213f81bf97d0cede4abb8ef3?pvs=4)  
  
Gcode예시는 다음과 같습니다.  
```
; 메인
S_Create I"BG1" N"Cutscene_04"; "Cutscene_04" 스프라이트를 ID : "BG1"로 유니티에 인스턴스화합니다.
S_Create I"BG2" N"Cutscene_02"

BG_set I"BG2"; "BG2"를 배경화면으로 설정합니다

Text M"diatest/101e_001" N"diatest/N_Narration"; i2L에서 "diatest/101e_001"key값에 대응되는 텍스트를 출력합니다. 이름을 "diatest/N_Narration"으로 설정합니다.
Text M"diatest/101e_002"
Text M"diatest/101e_003" F; 이전 텍스트를 Flush하지 않고 이어서 출력합니다.

BG_SET I"BG1"
Text M"diatest/101e_004"
Text M"diatest/101e_005"
```

Gcode 시스템은 원하는 기능이 있으면 해당 기능을 c#으로 추가하기만 하고 기존의 gcode들은 수정할 필요가 없기 때문에 독립성이 좋습니다. 
다만 가독성이 별로 좋지 못하다는 단점이 있습니다. 추후엔 이 시스템을 베이스로 하여 GUI툴을 만들어 볼 계획이 있습니다.  

[Gcode 파서](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Utils/Gcode.cs#L36)  
[Gcode 실행기](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Manager/DialogueManager.cs#L1376)  

아래는 실행기의 일부분입니다.  
```
public IEnumerator Executor()
    {
        while (!IsEnd())
        {
            Debug.Log($"Gcode parser EXECUTE ({GetCommand()})");
            switch (GetCommandType())
            {
                case "TEXT": // 메세지 출력은 바로 return하여 다음 마우스 클릭을 기다림
                    DialogueManager.Instance.TEXT(GetArguments(new Dictionary<string, Type>()
                    {
                        { "M", typeof(string) },
                        { "S", typeof(int) },
                        { "O", typeof(bool) },
                        { "F", typeof(bool) },
                        { "N", typeof(string) },
                        { "I", typeof(string) },
                    }));
                    Next();
                    coroutine = null;
                    yield break;

                case "TEXT_SPD":
                    DialogueManager.Instance.TEXT_SPD(GetArguments(new Dictionary<string, Type>()
                    {
                        { "S", typeof(int) },
                    }));
                    break;

```

- Gcode의 각 줄은 하나의 명령어이고, flag를 이용해 인자를 전달합니다.
- 각 명령어별로 필요한 인자를 Dictionary형태로 정의하여 코드의 가독성을 높였습니다.  
- 다이얼로그 매니저는 각 명령어에 대응되는 함수가 정의되어 있어 실제 다이얼로그를 조작합니다.  
- 다이얼로그 시스템의 기능들을 각 함수로 매핑하여 높은 자유도로 다이얼로그를 gcode명령어로 조작할 수 있습니다.  
- 주요 기능으론 텍스트 출력, 텍스트 출력 속도 조절, 폰트 설정, 이름 설정, 배경화면 설정, 스프라이트 생성, 스프라이트 이동/회전/크기/색상 조절, 스프라이트 애니메이션 생성/실행 이 있습니다.  
- 사실상 스프라이트 애니메이션 생성의 높은 자유도를 위해 만든 시스템입니다. Transform기반의 모든 애니메이션을 이 Gcode로 구현할 수 있습니다.  

## 퍼즐 데이터 json 파서
기획자가 퍼즐을 구상하여 구글 스프레드시트에 입력하면, 해당 csv파일을 긁어와 게임에서 사용할 수 있는 json구조로 파싱하는 모듈을 만들었습니다.  
Newtonsoft.Json 라이브러리를 사용했습니다.  
[Json converter1 코드](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Data/StageDataConverter.cs#L6)  
[Json converter2 코드](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Data/TileObjectConveter.cs#L6)  
[구글 스프레드 시트 연동 코드](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/MapEditor/Editor/StageDataSheetManager.cs#L111)  
  
csv파일 다운로드시 여러 링크에 다운로드 리퀘스트를 보내는 경우 멀티 스레드 방식으로 리퀘스트를 보내는 것이 성능이 좋았습니다.  
싱글 스레드로 순차적으로 다운로드를 기다리는 경우 첫번째 다운로드가 완료되어야 두번째 다운로드가 시작되어 시간 손해가 생깁니다.  
```
// 1. 퍼즐 정보 테이블과 퍼즐 구조 테이블을 다운로드
        Debug.Log($"퍼즐 정보 테이블 다운로드... \n(Path :{puzzleDataSheetLoader.PuzzleDataTableURL})");
        Debug.Log($"퍼즐 구조 테이블 다운로드... \n(Path :{puzzleDataSheetLoader.PuzzleStructureTableURL})");

        List<Task<string>> downloadTasks = new List<Task<string>>()
        {
            GoogleSheetDownloader.DownloadCSV(puzzleDataSheetLoader.PuzzleDataTableURL),
            GoogleSheetDownloader.DownloadCSV(puzzleDataSheetLoader.PuzzleStructureTableURL)
        };

        // 2. CSV 데이터를 파싱하여 Dictionary<string, string>[] 형태로 변환
        string[] downloadResults = await Task.WhenAll(downloadTasks);
        string puzzleDataCSV = downloadResults[0];
        string puzzleStructureCSV = downloadResults[1];
        
        Debug.Log(" 다운로드 완료 ");
```
이 툴 덕분에 기획자가 업데이트한 퍼즐 데이터를 쉽게 게임에 적용할 수 있게 되었습니다.  

## 퍼즐 자료구조 리팩토링 및 맵 에디터 수정

이 프로젝트는 옛날에 진행하다 무산된 프로젝트를 발굴하여 진행하는 프로젝트입니다. 때문에 기획자가 변경되어 퍼즐의 자료구조와 게임 로직이 변경되었습니다.  
현재 기획자의 요구사항에 맞게 퍼즐 자료구조를 변경하는 과정에서 대대적인 게임 로직의 변화가 생겼습니다. 이 작업을 진행하며 게임의 전반적인 로직을 검토하게 되었습니다.  
또 해당 과정에서 코드의 일부 리팩토링을 거쳤습니다.  
(커밋 로그를 첨부하고싶지만 프로젝트가 Private이기 때문에 첨부 못하는 점 양해 바랍니다)  

대표적으로 추상클래스를 활용해 함수 호출을 간략화하여 코드 가독성을 높인 사례가 있습니다. (원본 프로젝트가 Private이기 때문에 코드 첨부를 못하는 점 양해 바랍니다)  

해당 맵 자료구조에 맞게 기존의 맵 에디터도 수정하였습니다.  
![image](https://github.com/user-attachments/assets/7828fe2e-1c4e-46da-b20e-e9144f4f384c)  
그리고 팀원이 만들어준 퍼즐 풀이 알고리즘을 맵 에디터에 추가하여 기획자가 퍼즐의 해답을 쉽게 파악하여 의도된 대로 퍼즐이 풀리는 지 검토할 수 있게 하였습니다.  

[퍼즐 자료구조1](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Data/StageData.cs#L11)  
[퍼즐 자료구조2](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Data/TileData.cs#L10)  
[맵에디터 폴더](https://github.com/tpwls6630/Anota-Portfolio-/tree/main/MapEditor)  

## 인풋 시스템 마이그레이션

이 프로젝트는 키보드만으로 게임을 조작하는 것을 목표로 잡았기 때문에 기존의 Input.Keycode를 이용한 조작에서 벗어나 다양한 키 매핑이 가능하도록(패드도 활용 가능하도록) 인풋시스템을 마이그레이션 하는 작업이 배정되었습니다.  
InputSystem 패키지는 유니티6버전에선 기본적으로 제공하는 기능이지만 2021.3.16f1버전에선 따로 PackageManager로 받아 사용해야 하기 때문에 마이그레이션 작업이 필요하게 되었습니다.  
해당 기능을 구현하며 조작감에 대한 개선 의견이 제시되어, 퍼즐 조작 시 선입력 기능을 추가하는 작업이 배정되었습니다.  
저는 격투게임을 옛날에 즐겨했던 기억이 있기 때문에 경직과 선입력 시스템을 잘 이해하고 있었고, 캐릭터의 경직이 풀리기 6프레임 전까지 입력된 인풋 큐를 처리하도록 하는 시스템을 개발하였습니다.  

[인풋매니저](https://github.com/tpwls6630/Anota-Portfolio-/blob/a54f4160c5f2f7889aa4f69464c52231f77c7391/Manager/InputManager.cs#L5)
```
#region 퍼즐 - 플레이어 인풋 큐
    private struct InputData
    {
        public Vector2 direction;
        public float timeStamp;

        public InputData(Vector2 direction, float timeStamp)
        {
            this.direction = direction;
            this.timeStamp = timeStamp;
        }
    }

    private Queue<InputData> _playerMoveQueue = new();
    [SerializeField] private float _inputBufferTime = 0.096f; // 약 6프레임

    private void OnMoveInput(Vector2 direction)
    {
        if (_isUIActive || _isDialogueActive)
            return;

        if (direction == Vector2.zero)
            return;

        _playerMoveQueue.Enqueue(new InputData(direction, Time.time));
    }

    #endregion
    ...
    // 주기적으로 inputBufferTime을 초과한 입력은 삭제시켜준다
    void Update()
    {
        while (_playerMoveQueue.Count > 0 && Time.time - _playerMoveQueue.Peek().timeStamp > _inputBufferTime)
        {
            _playerMoveQueue.Dequeue();
        }
    }

    // 캐릭터 이동 입력을 받는 함수
    public Vector2 GetPlayerMoveInput()
    {
        // UI가 활성화되어 있으면 이동 입력을 무시
        if (_isUIActive || _isDialogueActive)
            return Vector2.zero;
        return _playerMoveQueue.Count > 0 ? _playerMoveQueue.Dequeue().direction : Vector2.zero;
    }
```

- 인풋매니저는 방향키 입력을 받을 때마다 입력이 발생한 시간과 벡터값을 큐에 저장합니다.  
- 매 update호출마다 _inputBufferTime이 지난 입력은 큐에서 제거합니다.
- 캐릭터의 경직이 풀리면 캐릭터의 update함수에서 이동 입력이 큐에 있는지 확인하여 이동할 지 결정합니다.

이 기능을 리팩토링 한다면 매 update호출마다 오래된 인풋을 제거하는 것이 아니라 이동 입력을 받을 때만 일괄적으로 오래된 인풋을 제거하도록 하면 더 적은 함수 호출을 가지고 같은 기능을 구현할 수 있을 것 같습니다.
