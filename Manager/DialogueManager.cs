using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using I2.Loc;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 싱글톤 패턴이긴 하지만 DontDestroyOnLoad를 사용하지 않기 때문에 싱글톤을 직접 구현했다(고 한다)
// 프리팹으로 만들어 두었기에 인스턴스화가 생략되었다.
public class DialogueManager : MonoBehaviour
{
    // 싱글톤 패턴. 인스턴스를 하나만 만들어서 사용한다. 
    private static DialogueManager _instance;

    public static DialogueManager Instance
    {
        get => _instance;
        set => _instance = value;
    }

    #region 데이터필드

    private List<IEnumerator> activeCoroutines = new List<IEnumerator>();

    [SerializeField] private Canvas DialogueCanvas;

    [SerializeField] private Image DialogueTextBoard;
    [SerializeField] private TextMeshProUGUI Textboard_Name;
    [SerializeField] private TextMeshProUGUI Textboard_Text;

    [SerializeField] private Image SpriteBase;
    [Header("스프라이트")]
    [SerializeField] private List<Sprite> DialogueSprites;
    [SerializeField] private GameObject SpritesGroup;

    [SerializeField] private DialogueMenuManager dialogueMenuManager;
    [SerializeField] private DialogueLogManager dialogueLogManager;

    public DialogueLogManager DialogueLogManager
    {
        get => dialogueLogManager;
    }

#if UNITY_EDITOR
    [SerializeField] private bool isTestMode = false;
    [EnableIf("isTestMode")]
    [SerializeField] private string DialogueGcodePath;
#endif

    private bool _isDialogueRunning = false;
    public bool IsDialogueRunning
    {
        get => _isDialogueRunning;
        set
        {
            if (value == true)
            {
                InputManager.Instance.SetDialogueActive(true);
            }
            else
            {
                InputManager.Instance.SetDialogueActive(false);
            }
            _isDialogueRunning = value;
        }
    }
    public bool _isDialogueTextRunning = false;
    private bool IsMenuOpen { get => dialogueMenuManager.IsMenuOpen; }
    private bool IsLogOpen { get => dialogueLogManager.gameObject.activeSelf; }
    private Coroutine AutoExecuteCoroutine;
    public bool IsAutoExecuting { get => AutoExecuteCoroutine != null; }

    #region Gcode 제어 변수
    private DialogueGcodeParser dialogueGcodeParser;

    private int text_printSpeed = 100;

    private float text_fontSize = 36;
    private bool text_fontBold = false;
    private bool text_fontItalic = false;
    private bool text_fontUnderline = false;
    private bool text_fontStrike = false;

    private float text_fontColorR = 0;
    private float text_fontColorG = 0;
    private float text_fontColorB = 0;
    private float text_fontColorA = 255;

    private float name_fontColorR = 0;
    private float name_fontColorG = 0;
    private float name_fontColorB = 0;
    private float name_fontColorA = 255;

    private GameObject DialogueBackGround;
    private Dictionary<string, GameObject> dialogueSpriteTable = new Dictionary<string, GameObject>();
    private AnimationCreator animationCreator = new AnimationCreator();

    #endregion // Gcode 제어 전역변수

    #endregion // 데이터필드

    #region 메소드

    public void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        Debug.Log("DialogueManager Awake");
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (isTestMode)
        {
            Init(DialogueGcodePath);
        }
#endif
    }

    public void Init(DialogueEvent dialogueEvent, int world, int stage)
    {
        string gcodePath = string.Empty;
        gcodePath = (world * 100 + stage).ToString();
        gcodePath += "_" + dialogueEvent.ToString();
        gcodePath += ".gcode";

        Init(gcodePath);
    }

    // 데이터를 가져오고, 만약 현재 스테이지에 다이얼로그가 존재하는 경우 다이얼로그를 출력한다.
    public void Init(string gcodePath)
    {
        if (dialogueGcodeParser != null)
        {
            Destroy(dialogueGcodeParser);
        }
        dialogueGcodeParser = gameObject.AddComponent<DialogueGcodeParser>();

        dialogueSpriteTable.Clear();
        animationCreator = new AnimationCreator();

        SetActiveDialogue(true);

        try
        {
            dialogueGcodeParser.LoadGcode(gcodePath);
            dialogueGcodeParser.Execute();
            IsDialogueRunning = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DialogueManager Init Error: 다이얼로그 파일경로가 없습니다. {e.Message}");
            ExitDialogue(true);
        }
    }

    #region 코루틴 매니지먼트

    private Coroutine StartTrackedCoroutine(IEnumerator coroutine)
    {
        activeCoroutines.Add(coroutine);
        return StartCoroutine(WrapCoroutine(coroutine));
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine)
    {
        while (coroutine.MoveNext())
        {
            yield return coroutine.Current;
        }
        activeCoroutines.Remove(coroutine);
    }

    private void SkipAllCoroutines()
    {
        foreach (var coroutine in activeCoroutines)
        {
            SkipCoroutineToEnd(coroutine);
        }
        activeCoroutines.Clear();
    }

    private void SkipCoroutineToEnd(IEnumerator coroutine)
    {
        while (coroutine.MoveNext()) { }
    }

    public bool IsCoroutineRunning()
    {
        return activeCoroutines.Count > 0;
    }

    #endregion


    // 1. 다이얼로그 시작 (스프라이트 변경, 텍스트 출력 시작) :: isDialogueRunning = true, isDialogueTextRunning = true
    // 2. 텍스트 한글자씩 출력 <- 마우스 클릭 시 텍스트 전부 출력 :: isDialogueTextRunning = true
    // 3. 텍스트 출력 완료 :: isDialogueTextRunning = false
    // 마우스 클릭 시 -> count + 1 &...
    // 3-1. 다음 대화가 있다면 -> 다음 대화 출력
    // 3-2. 다음 대화가 없다면 -> 다이얼로그 종료 :: isDialogueRunning = false, isDialogueTextRunning = false
    //
    // #언제라도 : Skip을 누르면 -> 다이얼로그 종료
    // #언제라도 : Back을 누르면 -> 
    // #언제라도 : 대화 내역 버튼을 누르면 -> 대화 내역 표시(TimeScale = 0 이 가장 무난한듯?)
    private void Update()
    {
        if (!IsMenuOpen && !IsLogOpen)
        {
            if (IsDialogueRunning)
            {
                if (InputManager.Instance.GetDialogueInputDown())
                {
                    InputInteraction();
                }
            }
        }
    }


    /// <summary>
    /// 마우스 클릭, 키 입력에 대한 상호작용을 처리하는 메소드.
    /// </summary>
    public void InputInteraction()
    {
        AutoDialogue(false);
        animationCreator.CompleteAllSequences();

        if (_isDialogueTextRunning)
        {
            SkipAllCoroutines();
        }
        else
        {
            dialogueGcodeParser.Execute();
        }
    }


    /// <summary>
    /// 다이얼로그 매니저 프리팹의 ScreenClick 오브젝트에서 호출됨
    /// 메뉴가 열려있었다면 메뉴를 닫고, 아니라면 InputInteraction을 호출한다.
    /// </summary>
    public void OnScreenClick()
    {
        if (dialogueMenuManager.IsMenuOpendOnClickAnyware())
        {
            return;
        }
        InputInteraction();
    }

    /// <summary>
    /// 2. 텍스트 한글자씩 출력 <- 마우스 클릭 시 텍스트 전부 출력 :: isDialogueTextRunning = true
    /// 다이얼로그 텍스트를 출력하는 코루틴.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PrintDialogueText(string message, int? printSpeed)
    {
        
        _isDialogueTextRunning = true;

        for (int i = 0; i < message.Length; i++)
        {
            Textboard_Text.text += message[i]; // 한글자씩 출력
            Textboard_Text.text = Textboard_Text.text.Replace("\\n", "\n"); // 개행문자 처리
            yield return new WaitForSeconds(1f / (printSpeed ?? text_printSpeed));
        }

        _isDialogueTextRunning = false;
    }

    /// <summary>
    /// 다이얼로그 전체 대화가 끝났을 때 호출되는 함수
    /// </summary>
    public void ExitDialogue(bool instantly = false)
    {
        AutoDialogue(false);
        IsDialogueRunning = false;
        _isDialogueTextRunning = false;
        animationCreator.KillAllSequences();
        SkipAllCoroutines();

        if (instantly)
        {
            DialogueCanvas.enabled = false;
        }
        else
        {
            SetActiveDialogue(false);
        }

    }

    public void AutoDialogue(bool isAutoMode)
    {
        IEnumerator AutoExecute()
        {
            while (true)
            {
                yield return new WaitUntil(() => !_isDialogueTextRunning);
                yield return new WaitUntil(() => animationCreator.IsAllSequencesCompleted());
                yield return new WaitForSeconds(1f);
                dialogueGcodeParser.Execute();
            }
        }

        if (isAutoMode)
        {
            AutoExecuteCoroutine = StartCoroutine(AutoExecute());
        }
        else
        {
            if (AutoExecuteCoroutine != null)
            {
                StopCoroutine(AutoExecuteCoroutine);
            }
            AutoExecuteCoroutine = null;
        }
    }

    public void SetActiveDialogue(bool isActive)
    {
        if (isActive)
        {
            DialogueCanvas.enabled = true;
            DialogueCanvas.GetComponent<CanvasGroup>().DOFade(1, 0.5f);
        }
        else
        {
            DialogueCanvas.GetComponent<CanvasGroup>().DOFade(0, 0.5f).OnComplete(() => DialogueCanvas.enabled = false);
        }

    }

    public void SetTextboardActive(bool isActive)
    {
        if (isActive)
        {
            DialogueTextBoard.enabled = true;
            DialogueTextBoard.GetComponent<CanvasGroup>().DOFade(1, 0.5f);
        }
        else
        {
            DialogueTextBoard.GetComponent<CanvasGroup>().DOFade(0, 0.5f).OnComplete(() => DialogueTextBoard.enabled = false);
        }
    }

    private string GetText(string key)
    {
        return LocalizationManager.GetTranslation(key) ?? key;
    }


    // ### 텍스트 출력 (TEXT)
    // - M{string} : 현재 로딩된 번역테이블에서 {string}값에 해당되는 텍스트를 불러와 다이얼로그 박스에 출력하고 대기상태로 진입
    // - [S{int}] : 텍스트 출력속도를 이 커맨드에 한해 변경함.
    // - [O] : 텍스트 출력을 한글자씩 하는 것이 아니라 한번에 함. S플래그는 무시함
    // - [F] : 이전에 출력된 텍스트를 Flush하지 않고 이어서 출력함.
    // - [N{string : key}] : 화자의 이름을 Name박스에 출력 (=T2)
    // - [I{string}] : 해당 스프라이트를 하이라이트하고 이외의 스프라이트를 어둡게 변경함
    public void TEXT(Dictionary<string, object> arguments)
    {
        string key = arguments["M"] as string;
        int? printSpeed = arguments["S"] as int?;
        bool? printInOnce = arguments["O"] as bool?;
        bool? notFlush = arguments["F"] as bool?;
        string nameKey = arguments["N"] as string;
        string spriteKey = arguments["I"] as string;

        if (key == null)
        {
            Debug.LogError("GcodeParser : TEXT - M flag is not found");
        }

        string message = GetText(key);

        if (printInOnce == true)
        {
            _isDialogueTextRunning = true;

            if (notFlush == true)
            {
                Textboard_Text.text += message;
            }
            else
            {
                Textboard_Text.text = message;
            }
            Textboard_Text.text = Textboard_Text.text.Replace("\\n", "\n"); // 개행문자 처리
            _isDialogueTextRunning = false;
        }
        else
        {
            if (notFlush != true)
            {
                Textboard_Text.text = "";
            }

            StartTrackedCoroutine(PrintDialogueText(message, printSpeed));
        }

        string name = "";
        Sprite sprite = null;

        if (nameKey != null)
        {
            name = GetText(nameKey);
            NAME(new Dictionary<string, object> { { "M", nameKey } });
        }

        if (spriteKey != null)
        {
            if (dialogueSpriteTable.ContainsKey(spriteKey))
            {
                sprite = dialogueSpriteTable[spriteKey].GetComponent<Image>().sprite;
            }
        }

        // 대화 로그에 추가
        dialogueLogManager.AddDialogueLog(message, name, sprite, false);

    }

    //     ### 텍스트 출력 제어 (TEXT_SPD)
    // - [S{int}] : 텍스트 출력속도를 설정. 초당 {int}글자
    public void TEXT_SPD(Dictionary<string, object> arguments)
    {
        int? printSpeed = arguments["S"] as int?;

        if (printSpeed <= 0)
        {
            Debug.LogError("GcodeParser : TEXT_SPD - printSpeed has invalid value. printSpeed must be greater than 0");
        }

        if (printSpeed != null)
        {
            text_printSpeed = printSpeed.Value;
        }
    }

    //     ### 텍스트 폰트 제어 (TEXT_FONT)
    // - [F{float}] : 폰트 사이즈를 전역변수로 저장
    // - [B{0/1}] : 폰트 굵게(Bold) 설정. 1 : On, 0 : Off
    // - [I{0/1}] : 폰트 이탤릭체 설정
    // - [U{0/1}] : 폰트 밑줄 설정
    // - [S{0/1}] : 폰트 취소선 설정
    public void TEXT_FONT(Dictionary<string, object> arguments)
    {
        float? fontSize = arguments["F"] as float?;
        int? fontBold = arguments["B"] as int?;
        int? fontItalic = arguments["I"] as int?;
        int? fontUnderline = arguments["U"] as int?;
        int? fontStrike = arguments["S"] as int?;

        if (fontSize != null)
        {
            text_fontSize = fontSize.Value;
        }

        if (fontBold != null)
        {
            text_fontBold = fontBold.Value == 1;
        }

        if (fontItalic != null)
        {
            text_fontItalic = fontItalic.Value == 1;
        }

        if (fontUnderline != null)
        {
            text_fontUnderline = fontUnderline.Value == 1;
        }

        if (fontStrike != null)
        {
            text_fontStrike = fontStrike.Value == 1;
        }

        Textboard_Text.fontSize = text_fontSize;
        Textboard_Text.fontStyle = FontStyles.Normal;

        if (text_fontBold)
        {
            Textboard_Text.fontStyle |= FontStyles.Bold;
        }

        if (text_fontItalic)
        {
            Textboard_Text.fontStyle |= FontStyles.Italic;
        }

        if (text_fontUnderline)
        {
            Textboard_Text.fontStyle |= FontStyles.Underline;
        }

        if (text_fontStrike)
        {
            Textboard_Text.fontStyle |= FontStyles.Strikethrough;
        }
    }

    //     ### 텍스트 폰트 컬러 설정 (TEXT_COLOR)
    // - [R{float}] : 폰트 컬러 Red값 전역변수로 저장
    // - [G{float}] : 폰트 컬러 Green값 전역변수로 저장
    // - [B{float}] : 포트 컬러 Blue값 전역변수로 저장
    // - [H{float}] : 폰트 컬러 alpHa값 전역변수로 저장
    public void TEXT_COLOR(Dictionary<string, object> arguments)
    {
        float? R = arguments["R"] as float?;
        float? G = arguments["G"] as float?;
        float? B = arguments["B"] as float?;
        float? H = arguments["H"] as float?;

        if (R != null)
        {
            text_fontColorR = R.Value;
        }

        if (G != null)
        {
            text_fontColorG = G.Value;
        }

        if (B != null)
        {
            text_fontColorB = B.Value;
        }

        if (H != null)
        {
            text_fontColorA = H.Value;
        }

        Textboard_Text.color = new Color(text_fontColorR / 255f, text_fontColorG / 255f, text_fontColorB / 255f, text_fontColorA / 255f);

    }

    //     ### 이름 출력 (NAME)
    // - M{string} : 다이얼로그 창에 이름 출력
    public void NAME(Dictionary<string, object> arguments)
    {
        string key = arguments["M"] as string;

        if (key == null)
        {
            Debug.LogError("GcodeParser : NAME - M flag is not found");
        }

        string name = GetText(key);

        Textboard_Name.text = name;
    }

    //     ### 이름 폰트 컬러 설정 (NAME_COLOR)
    // - [R{float}] : 폰트 컬러 Red값 전역변수로 저장
    // - [G{float}] : 폰트 컬러 Green값 전역변수로 저장
    // - [B{float}] : 포트 컬러 Blue값 전역변수로 저장
    // - [H{float}] : 폰트 커러 alpHa값 전역변수로 저장
    public void NAME_COLOR(Dictionary<string, object> arguments)
    {
        float? R = arguments["R"] as float?;
        float? G = arguments["G"] as float?;
        float? B = arguments["B"] as float?;
        float? H = arguments["H"] as float?;

        if (R != null)
        {
            name_fontColorR = R.Value;
        }

        if (G != null)
        {
            name_fontColorG = G.Value;
        }

        if (B != null)
        {
            name_fontColorB = B.Value;
        }

        if (H != null)
        {
            name_fontColorA = H.Value;
        }

        Textboard_Name.color = new Color(name_fontColorR / 255f, name_fontColorG / 255f, name_fontColorB / 255f, name_fontColorA / 255f);
    }
    #endregion // 메소드

    //     ### 텍스트박스 On/Off (TEXT_E)
    // - [T{0/1}] : 텍스트박스 On/Off.
    // - ~~[N{0/1}] : 이름 On/Off.~~
    public void TEXT_E(Dictionary<string, object> arguments)
    {
        int? textBoard = arguments["T"] as int?;
        // int? nameBoard = arguments["N"] as int?;

        if (textBoard != null)
        {
            SetTextboardActive(textBoard == 1);
        }

        // if (nameBoard != null)
        // {
        //     Textboard_Name.enabled = nameBoard == 1;
        // }   
    }


    //     ### 스프라이트 등록 (S_CREATE)
    // - I{string} : 스프라이트 제어 Gcode에 사용될 Id
    // - N{string} : 스프라이트 파일명. 다이얼로그 매니저 설정창에 등록되어 있는 스프라이트 이름과 동일해야 함.
    // ID는 덮어쓰기가 가능합니다.
    // 하나의 스프라이트를 여러 ID에 등록할 수 있습니다. 이 때 각 ID에 해당되는 스프라이트는 개별로 관리가 가능합니다. (ID당 오브젝트가 독립적으로 존재함)
    public void S_CREATE(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        string name = arguments["N"] as string;
        bool? enable = arguments["E"] as bool?;

        if (id == null || name == null)
        {
            Debug.LogError("GcodeParser : S_CREATE - I or N flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            Destroy(dialogueSpriteTable[id]);
        }

        GameObject sprite = Instantiate(SpriteBase.gameObject, SpritesGroup.transform);
        sprite.GetComponent<Image>().sprite = DialogueSprites.Find(x => x.name == name);
        dialogueSpriteTable[id] = sprite;

        // 스프라이트 크기에 맞게 Width, Height 조절
        sprite.GetComponent<RectTransform>().sizeDelta = sprite.GetComponent<Image>().sprite.rect.size;

        // 스프라이트 활성화 여부
        sprite.SetActive(enable == true);

    }


    // ### 스프라이트 이동 - 즉시 (S_MOVE)
    // 스프라이트의 position을 즉시 변경합니다.
    // - I{string} : 스프라이트 제어 ID
    // - [X{float}] : 스프라이트 위치 X값
    // - [Y{float}] : 스프라이트 위치 Y값
    // - [Z{float}] : 스프라이트 위치 Z값
    public void S_MOVE(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_MOVE - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            Vector3 position = sprite.transform.position;

            if (x != null)
            {
                position.x = x.Value;
            }

            if (y != null)
            {
                position.y = y.Value;
            }

            if (z != null)
            {
                position.z = z.Value;
            }

            sprite.GetComponent<RectTransform>().anchoredPosition = position;
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_MOVE - Sprite {id} is not found");
        }
    }


    // ### 스프라이트 회전 - 즉시 (S_ROTATE)
    // 스프라이트의 rotation을 즉시 변경합니다.
    // - I{string} : 스프라이트 제어 ID
    // - [X{float}] : 스프라이트 회전 X값
    // - [Y{float}] : 스프라이트 회전 Y값
    // - [Z{float}] : 스프라이트 회전 Z값
    public void S_ROTATE(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_ROTATE - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            Vector3 rotation = sprite.transform.rotation.eulerAngles;

            if (x != null)
            {
                rotation.x = x.Value;
            }

            if (y != null)
            {
                rotation.y = y.Value;
            }

            if (z != null)
            {
                rotation.z = z.Value;
            }

            sprite.transform.rotation = Quaternion.Euler(rotation);
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_ROTATE - Sprite {id} is not found");
        }
    }


    // ### 스프라이트 크기 - 즉시 (S_SCALE)
    // 스프라이트의 크기를 즉시 변경합니다.
    // - I{string} : 스프라이트 제어 ID
    // - [X{float}] : 스프라이트 스케일 X값
    // - [Y{float}] : 스프라이트 스케일 Y값
    // - [Z{float}] : 스프라이트 스케일 Z값
    // - [W{float}] : 스프라이트 폭(Width)
    // - [H{ float}] : 스프라이트 높이(Height)
    public void S_SCALE(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;
        float? w = arguments["W"] as float?;
        float? h = arguments["H"] as float?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_SCALE - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            Vector3 scale = sprite.transform.localScale;

            if (x != null)
            {
                scale.x = x.Value;
            }

            if (y != null)
            {
                scale.y = y.Value;
            }

            if (z != null)
            {
                scale.z = z.Value;
            }

            if (w != null)
            {
                scale.x = w.Value;
            }

            if (h != null)
            {
                scale.y = h.Value;
            }

            sprite.transform.localScale = scale;
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_SCALE - Sprite {id} is not found");
        }
    }


    // ### 스프라이트 색상 (S_COLOR)
    //     스프라이트의 색상을 즉시 변경합니다.
    // - I{string} : 스프라이트 제어 ID
    // - [R{float}] : Red
    // - [G{float}] : Green
    // - [B{float}] : Blue
    // - [H{float}] : alpHa(투명도)
    public void S_COLOR(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        float? r = arguments["R"] as float?;
        float? g = arguments["G"] as float?;
        float? b = arguments["B"] as float?;
        float? h = arguments["H"] as float?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_COLOR - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            Color color = sprite.GetComponent<Image>().color;

            if (r != null)
            {
                color = new Color(r.Value / 255f, color.g, color.b, color.a);
            }

            if (g != null)
            {
                color = new Color(color.r, g.Value / 255f, color.b, color.a);
            }

            if (b != null)
            {
                color = new Color(color.r, color.g, b.Value / 255f, color.a);
            }

            if (h != null)
            {
                color = new Color(color.r, color.g, color.b, h.Value / 255f);
            }

            sprite.GetComponent<Image>().color = color;
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_COLOR - Sprite {id} is not found");
        }
    }


    // ### 스프라이트 On/Off (S_E)
    //     스프라이트를 On/Off합니다.
    // - I{string} : 스프라이트 제어 ID
    // - E{int : 0 / 1} : On / Off
    public void S_E(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        int? enable = arguments["E"] as int?;

        if (id == null || enable == null)
        {
            Debug.LogError("GcodeParser : S_E - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            sprite.SetActive(enable.Value == 1);
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_E - Sprite {id} is not found");
        }
    }

    // ### 스프라이트 하이라이트 (S_HL)
    //     해당 스프라이트를 하이라이트(On/Off)합니다(= S_COLOR … R255 G255 B255 / R100 G100 B100)
    // - I{string} : 스프라이트 제어 ID
    // - [D] : 디하이라이트(= S4 R100 G100 B100)
    // - [O] : 이 스프라이트를 제외한 모든 스프라이트를 디하이라이트합니다
    public void S_HL(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        bool? d = arguments["D"] as bool?;
        bool? o = arguments["O"] as bool?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_HL - I flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            Color color = sprite.GetComponent<Image>().color;

            if (d == true)
            {
                color = new Color(100 / 255f, 100 / 255f, 100 / 255f, color.a);
            }
            else
            {
                color = new Color(255 / 255f, 255 / 255f, 255 / 255f, color.a);
            }

            sprite.GetComponent<Image>().color = color;

            if (o == true)
            {
                foreach (var sp in dialogueSpriteTable.Values)
                {
                    if (sp != dialogueSpriteTable[id] && sp != DialogueBackGround)
                    {
                        sp.GetComponent<Image>().color = new Color(100 / 255f, 100 / 255f, 100 / 255f, sp.GetComponent<Image>().color.a);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_HL - Sprite {id} is not found");
        }
    }

    // ### 스프라이트 애니메이션 등록 (ANIM_CREATE)
    //     애니메이션을 프리셋으로 저장해놓고 나중에 스프라이트에 적용하여 애니메이션을 실행합니다.
    //     유니티의 DOTween 라이브러리를 기반으로 구현했습니다
    //     DOTween의 seqence를 사용합니다
    // - A{string} : 애니메이션을 제어할 이름
    public void ANIM_CREATE(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;

        if (id == null)
        {
            Debug.LogError("GcodeParser : ANIM_CREATE - A flag is not found");
        }

        animationCreator.CreateAnimation(id);
    }


    //     ### 스프라이트 애니메이션 - 이동 (ANIM_MOVE)
    // - A{string} : 애니메이션 이름
    // - [X{float}] : 스프라이트 위치 X값
    // - [Y{float}] : 스프라이트 위치 Y값
    // - [Z{float}] : 스프라이트 위치 Z값
    // - T{float} : 애니메이션 길이
    // - P | I{float} | J(셋 중에 하나) : aPpend 앞의 애니메이션 뒤에 붙힘 | Insert {float}초 뒤에 실행 | Join 앞의 애니메이션과 같이 실행 (중복 시 P > I > J 순으로 하나만 적용)
    public void ANIM_MOVE(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;
        float? t = arguments["T"] as float?;
        bool? p = arguments["P"] as bool?;
        float? i = arguments["I"] as float?;
        bool? j = arguments["J"] as bool?;
        bool? l = arguments["L"] as bool?;

        if (id == null || t == null)
        {
            Debug.LogError("GcodeParser : ANIM_MOVE - A or T flag is not found");
        }
        if (p == null && i == null && j == null)
        {
            Debug.LogError("GcodeParser : ANIM_MOVE - must contain P or I or J flag");
        }

        if (p == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {

                if (l == true)
                {
                    seq.Append(target.transform.DOBlendableMoveBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value));
                }
                else
                {
                    RectTransform rectTransform = target.GetComponent<RectTransform>();
                    seq.Append(target.GetComponent<RectTransform>().DOAnchorPos(new Vector3(
                        x ?? rectTransform.anchoredPosition.x,
                        y ?? rectTransform.anchoredPosition.y,
                        z ?? target.transform.position.z
                    ), t.Value));
                }
            });
        }
        else if (i != null)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {

                if (l == true)
                {
                    seq.Insert(i.Value, target.transform.DOBlendableMoveBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value));
                }
                else
                {
                    RectTransform rectTransform = target.GetComponent<RectTransform>();
                    seq.Insert(i.Value, target.GetComponent<RectTransform>().DOAnchorPos(new Vector3(
                        x ?? rectTransform.anchoredPosition.x,
                        y ?? rectTransform.anchoredPosition.y,
                        z ?? target.transform.position.z
                    ), t.Value));
                }

            });
        }
        else if (j == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                if (l == true)
                {
                    seq.Join(target.transform.DOBlendableMoveBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value));
                }
                else
                {
                    RectTransform rectTransform = target.GetComponent<RectTransform>();
                    seq.Join(target.GetComponent<RectTransform>().DOAnchorPos(new Vector3(
                        x ?? rectTransform.anchoredPosition.x,
                        y ?? rectTransform.anchoredPosition.y,
                        z ?? target.transform.position.z
                    ), t.Value));
                }

            });
        }
    }


    //     ### 스프라이트 애니메이션 - 회전 (ANIM_ROTATE)
    // - A{string} : 애니메이션 이름
    // - [X{float}] : 스프라이트 회전 X값
    // - [Y{float}] : 스프라이트 회전 Y값
    // - [Z{float}] : 스프라이트 회전 Z값
    // - T{float} : 애니메이션 길이
    // - P | I{float} | J(셋 중에 하나) : aPpend 앞의 애니메이션 뒤에 붙힘 | Insert {float}초 뒤에 실행 | Join 앞의 애니메이션과 같이 실행 (중복 시 P > I > J 순으로 하나만 적용)
    public void ANIM_ROTATE(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;
        float? t = arguments["T"] as float?;
        bool? p = arguments["P"] as bool?;
        float? i = arguments["I"] as float?;
        bool? j = arguments["J"] as bool?;
        bool? l = arguments["L"] as bool?;

        if (id == null || t == null)
        {
            Debug.LogError("GcodeParser : ANIM_ROTATE - A or T flag is not found");
        }
        if (p == null && i == null && j == null)
        {
            Debug.LogError("GcodeParser : ANIM_ROTATE - must contain P or I or J flag");
        }

        if (p == true)
        {

            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                if (l == true)
                {
                    seq.Append(target.transform.DOBlendableRotateBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value, RotateMode.FastBeyond360));
                }
                else
                {
                    seq.Append(target.transform.DORotate(new Vector3(
                        x ?? target.transform.rotation.x,
                        y ?? target.transform.rotation.y,
                        z ?? target.transform.rotation.z
                    ), t.Value, RotateMode.FastBeyond360));
                }
            });
        }
        else if (i != null)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                if (l == true)
                {
                    seq.Insert(i.Value, target.transform.DOBlendableRotateBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value, RotateMode.FastBeyond360));
                }
                else
                {
                    seq.Insert(i.Value, target.transform.DORotate(new Vector3(
                            x ?? target.transform.rotation.x,
                            y ?? target.transform.rotation.y,
                            z ?? target.transform.rotation.z
                        ), t.Value, RotateMode.FastBeyond360));
                }
            });
        }
        else if (j == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                if (l == true)
                {
                    seq.Join(target.transform.DOBlendableRotateBy(new Vector3(x ?? 0, y ?? 0, z ?? 0), t.Value, RotateMode.FastBeyond360));
                }
                else
                {
                    seq.Join(target.transform.DORotate(new Vector3(
                            x ?? target.transform.rotation.x,
                            y ?? target.transform.rotation.y,
                            z ?? target.transform.rotation.z
                        ), t.Value, RotateMode.FastBeyond360));
                }
            });
        }
    }


    //     ### 스프라이트 애니메이션 - 크기 (ANIM_SCALE)
    // - A{int} : 애니메이션 이름
    // - [X{float}] : 스프라이트 스케일 X값
    // - [Y{float}] : 스프라이트 스케일 Y값
    // - [Z{float}] : 스프라이트 스케일 Z값
    // - [W{float}] : 스프라이트 폭(Width)
    // - [H{float}] : 스프라이트 높이(Height)
    // - T{float} : 애니메이션 길이
    // - P | I{float} | J(셋 중에 하나): aPpend 앞의 애니메이션 뒤에 붙힘 | Insert {float}초 뒤에 실행 | Join 앞의 애니메이션과 같이 실행(중복 시 P > I > J 순으로 하나만 적용)
    public void ANIM_SCALE(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;
        float? x = arguments["X"] as float?;
        float? y = arguments["Y"] as float?;
        float? z = arguments["Z"] as float?;
        float? w = arguments["W"] as float?;
        float? h = arguments["H"] as float?;
        float? t = arguments["T"] as float?;
        bool? p = arguments["P"] as bool?;
        float? i = arguments["I"] as float?;
        bool? j = arguments["J"] as bool?;

        if (id == null || t == null)
        {
            Debug.LogError("GcodeParser : ANIM_SCALE - A or T flag is not found");
        }
        if (p == null && i == null && j == null)
        {
            Debug.LogError("GcodeParser : ANIM_SCALE - must contain P or I or J flag");
        }

        if (p == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Append(target.transform.DOScale(new Vector3(x ?? target.transform.rotation.x, y ?? target.transform.rotation.y, z ?? target.transform.rotation.z), t.Value));
                seq.Join(target.GetComponent<RectTransform>().DOSizeDelta(new Vector2(w ?? target.GetComponent<RectTransform>().sizeDelta.x, h ?? target.GetComponent<RectTransform>().sizeDelta.y), t.Value));
            });
        }
        else if (i != null)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Insert(i.Value, target.transform.DORotate(new Vector3(x ?? target.transform.rotation.x, y ?? target.transform.rotation.y, z ?? target.transform.rotation.z), t.Value));
                seq.Join(target.GetComponent<RectTransform>().DOSizeDelta(new Vector2(w ?? target.GetComponent<RectTransform>().sizeDelta.x, h ?? target.GetComponent<RectTransform>().sizeDelta.y), t.Value));
            });
        }
        else if (j == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Join(target.transform.DORotate(new Vector3(x ?? target.transform.rotation.x, y ?? target.transform.rotation.y, z ?? target.transform.rotation.z), t.Value));
                seq.Join(target.GetComponent<RectTransform>().DOSizeDelta(new Vector2(w ?? target.GetComponent<RectTransform>().sizeDelta.x, h ?? target.GetComponent<RectTransform>().sizeDelta.y), t.Value));
            });
        }
    }


    //     ### 스프라이트 애니메이션 - 색상(ANIM_COLOR)
    // - A{int} : 애니메이션 이름
    // - [R{float}] : Red
    // - [G{float}] : Green
    // - [B{float}] : Blue
    // - [H{float}] : alpHa
    // - T{float} : 애니메이션 길이
    // - P | I{float} | J(셋 중에 하나): aPpend 앞의 애니메이션 뒤에 붙힘 | Insert {float}초 뒤에 실행 | Join 앞의 애니메이션과 같이 실행(중복 시 P > I > J 순으로 하나만 적용)
    public void ANIM_COLOR(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;
        float? r = arguments["R"] as float?;
        float? g = arguments["G"] as float?;
        float? b = arguments["B"] as float?;
        float? a = arguments["H"] as float?;
        float? t = arguments["T"] as float?;
        bool? p = arguments["P"] as bool?;
        float? i = arguments["I"] as float?;
        bool? j = arguments["J"] as bool?;

        if (id == null || t == null)
        {
            Debug.LogError("GcodeParser : ANIM_COLOR - A or T flag is not found");
        }
        if (p == null && i == null && j == null)
        {
            Debug.LogError("GcodeParser : ANIM_COLOR - must contain P or I or J flag");
        }

        if (p == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Append(target.GetComponent<Image>().DOColor(new Color(
                    r / 255f ?? target.GetComponent<Image>().color.r,
                    g / 255f ?? target.GetComponent<Image>().color.g,
                    b / 255f ?? target.GetComponent<Image>().color.b,
                    a / 255f ?? target.GetComponent<Image>().color.a)
                    , t.Value));
            });
        }
        else if (i != null)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Insert(i.Value, target.GetComponent<Image>().DOColor(new Color(
                    r / 255f ?? target.GetComponent<Image>().color.r,
                    g / 255f ?? target.GetComponent<Image>().color.g,
                    b / 255f ?? target.GetComponent<Image>().color.b,
                    a / 255f ?? target.GetComponent<Image>().color.a)
                    , t.Value));
            });
        }
        else if (j == true)
        {
            animationCreator.AddTweenToAnimation(id, (seq, target) =>
            {
                seq.Join(target.GetComponent<Image>().DOColor(new Color(
                    r / 255f ?? target.GetComponent<Image>().color.r,
                    g / 255f ?? target.GetComponent<Image>().color.g,
                    b / 255f ?? target.GetComponent<Image>().color.b,
                    a / 255f ?? target.GetComponent<Image>().color.a)
                    , t.Value));
            });
        }
    }


    //     ### 스프라이트 애니메이션 - 딜레이 (ANIM_DELAY)
    // - A{string} : 애니메이션 이름
    // - T{float} : 딜레이 초
    public void ANIM_DELAY(Dictionary<string, object> arguments)
    {
        string id = arguments["A"] as string;
        float? t = arguments["T"] as float?;

        if (id == null || t == null)
        {
            Debug.LogError("GcodeParser : ANIM_DELAY - A or T flag is not found");
        }

        animationCreator.AddTweenToAnimation(id, (seq, target) =>
        {
            seq.AppendInterval(t.Value);
        });
    }


    //     ### 스프라이트 애니메이션 실행 (ANIM_PLAY)
    // - I{string} : 스프라이트 ID
    // - A{string} : 애니메이션 이름
    public void ANIM_PLAY(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        string animationName = arguments["A"] as string;

        if (id == null || animationName == null)
        {
            Debug.LogError("GcodeParser : ANIM_PLAY - I or A flag is not found");
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            GameObject sprite = dialogueSpriteTable[id];
            animationCreator.PlayAnimation(sprite, animationName);
        }
        else
        {
            Debug.LogWarning($"GcodeParser : ANIM_PLAY - Sprite {id} is not found");
        }
    }

    //    ### 스프라이트 배경이미지 설정 (BG_SET)
    // - I{string} : 배경이미지로 사용할 스프라이트 ID
    // - [T{float}] : 배경이미지 전환 Fade in 시간 (초). 생략 시 1
    // S0로 등록된 스프라이트를 배경 이미지로 복사합니다
    public void BG_SET(Dictionary<string, object> arguments)
    {
        string id = arguments["I"] as string;
        float? t = arguments["T"] as float?;

        if (id == null)
        {
            Debug.LogError("GcodeParser : S_CREATE - I or N flag is not found");
        }

        if (DialogueBackGround == null)
        {
            DialogueBackGround = Instantiate(SpriteBase.gameObject, DialogueCanvas.transform);
            DialogueBackGround.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            DialogueBackGround.SetActive(true);
            DialogueBackGround.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            DialogueBackGround.transform.SetAsFirstSibling();
        }

        if (dialogueSpriteTable.ContainsKey(id))
        {
            // Sprite sprite = dialogueSpriteTable[id].GetComponent<Image>().sprite;

            GameObject from = DialogueBackGround;
            GameObject to = dialogueSpriteTable[id];

            animationCreator.CreateAnimation("__BG_FADE__");
            animationCreator.AddTweenToAnimation("__BG_FADE__", (seq, _) =>
            {
                seq.Append(from.GetComponent<Image>().DOColor(new Color(0, 0, 0, 1), (t ?? 1) / 2f))
                .AppendCallback(() =>
                {
                    from.SetActive(false);
                    from.transform.SetParent(SpritesGroup.transform);
                    from.transform.SetAsLastSibling();

                    DialogueBackGround = to;

                    to.transform.SetParent(DialogueCanvas.transform);
                    to.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
                    to.GetComponent<Image>().color = new Color(0, 0, 0, 1);
                    to.transform.SetAsFirstSibling();
                    to.SetActive(true);
                })
                .Append(to.GetComponent<Image>().DOColor(new Color(1f, 1f, 1f, 1f), (t ?? 1) / 2f));

            });

            animationCreator.PlayAnimation(to, "__BG_FADE__");
        }
        else
        {
            Debug.LogWarning($"GcodeParser : S_CREATE - Sprite {id} is not found");
        }
    }


    public bool WAIT()
    {
        return animationCreator.IsAllSequencesCompleted();
    }
}

/// <summary>
/// 다이얼로그 Gcode를 파싱하여 실행하는 클래스
/// </summary>
public class DialogueGcodeParser : GcodeParserBase
{
    private Coroutine coroutine;

    public override void Execute()
    {
        if (coroutine == null)
        {
            coroutine = StartCoroutine(Executor());
        }
    }

    public IEnumerator Executor()
    {
        while (!IsEnd())
        {
            Debug.Log($"Gcode parser EXECUTE ({GetCommand()})");
            switch (GetCommandType())
            {
                // case "LOAD":
                //     DialogueManager.Instance.Load(GetArguments(new Dictionary<string, Type>()
                //     {
                //         { "F", typeof(string) },
                //     }));
                //     break;

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

                case "TEXT_FONT":
                    DialogueManager.Instance.TEXT_FONT(GetArguments(new Dictionary<string, Type>()
                    {
                        { "F", typeof(float) },
                        { "B", typeof(int) },
                        { "I", typeof(int) },
                        { "U", typeof(int) },
                        { "S", typeof(int) },
                    }));
                    break;

                case "TEXT_COLOR":
                    DialogueManager.Instance.TEXT_COLOR(GetArguments(new Dictionary<string, Type>()
                    {
                        { "R", typeof(float) },
                        { "G", typeof(float) },
                        { "B", typeof(float) },
                        { "A", typeof(float) },
                    }));
                    break;
                case "TEXT_E":
                    DialogueManager.Instance.TEXT_E(GetArguments(new Dictionary<string, Type>()
                    {
                        { "E", typeof(bool) },
                    }));
                    break;

                case "NAME":
                    DialogueManager.Instance.NAME(GetArguments(new Dictionary<string, Type>()
                    {
                        { "M", typeof(string) },
                    }));
                    break;

                case "NAME_COLOR":
                    DialogueManager.Instance.NAME_COLOR(GetArguments(new Dictionary<string, Type>()
                    {
                        { "R", typeof(float) },
                        { "G", typeof(float) },
                        { "B", typeof(float) },
                        { "A", typeof(float) },
                    }));
                    break;
                case "S_CREATE":
                    DialogueManager.Instance.S_CREATE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "N", typeof(string) },
                        { "E", typeof(bool) },
                    }));
                    break;
                case "S_MOVE":
                    DialogueManager.Instance.S_MOVE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                    }));
                    break;
                case "S_ROTATE":
                    DialogueManager.Instance.S_ROTATE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                    }));
                    break;
                case "S_SCALE":
                    DialogueManager.Instance.S_SCALE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                        { "W", typeof(float) },
                        { "H", typeof(float) },
                    }));
                    break;
                case "S_COLOR":
                    DialogueManager.Instance.S_COLOR(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "R", typeof(float) },
                        { "G", typeof(float) },
                        { "B", typeof(float) },
                        { "A", typeof(float) },
                    }));
                    break;
                case "S_E":
                    DialogueManager.Instance.S_E(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "E", typeof(int) },
                    }));
                    break;
                case "S_HL":
                    DialogueManager.Instance.S_HL(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "D", typeof(bool) },
                        { "O", typeof(bool) },
                    }));
                    break;
                case "ANIM_CREATE":
                    DialogueManager.Instance.ANIM_CREATE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                    }));
                    break;
                case "ANIM_MOVE":
                    DialogueManager.Instance.ANIM_MOVE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                        { "T", typeof(float) },
                        { "P", typeof(bool) },
                        { "I", typeof(float) },
                        { "J", typeof(bool) },
                        { "L", typeof(bool) },
                    }));
                    break;
                case "ANIM_ROTATE":
                    DialogueManager.Instance.ANIM_ROTATE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                        { "T", typeof(float) },
                        { "P", typeof(bool) },
                        { "I", typeof(float) },
                        { "J", typeof(bool) },
                        { "L", typeof(bool) },
                    }));
                    break;
                case "ANIM_SCALE":
                    DialogueManager.Instance.ANIM_SCALE(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                        { "X", typeof(float) },
                        { "Y", typeof(float) },
                        { "Z", typeof(float) },
                        { "W", typeof(float) },
                        { "H", typeof(float) },
                        { "T", typeof(float) },
                        { "P", typeof(bool) },
                        { "I", typeof(float) },
                        { "J", typeof(bool) },
                    }));
                    break;
                case "ANIM_COLOR":
                    DialogueManager.Instance.ANIM_COLOR(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                        { "R", typeof(float) },
                        { "G", typeof(float) },
                        { "B", typeof(float) },
                        { "H", typeof(float) },
                        { "T", typeof(float) },
                        { "P", typeof(bool) },
                        { "I", typeof(float) },
                        { "J", typeof(bool) },
                    }));
                    break;
                case "ANIM_DELAY":
                    DialogueManager.Instance.ANIM_DELAY(GetArguments(new Dictionary<string, Type>()
                    {
                        { "A", typeof(string) },
                        { "T", typeof(float) },
                    }));
                    break;
                case "ANIM_PLAY":
                    DialogueManager.Instance.ANIM_PLAY(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "A", typeof(string) },
                    }));
                    break;
                case "BG_SET":
                    DialogueManager.Instance.BG_SET(GetArguments(new Dictionary<string, Type>()
                    {
                        { "I", typeof(string) },
                        { "T", typeof(float) },
                    }));
                    break;
                case "WAIT":
                    float t = GetArguments(new Dictionary<string, Type>()
                    {
                        { "T", typeof(float) },
                    })["T"] as float? ?? 0;
                    yield return new WaitUntil(DialogueManager.Instance.WAIT);
                    yield return new WaitForSeconds(t);
                    break;
            }

            Next();

        }

        if (IsEnd())
        {
            DialogueManager.Instance.ExitDialogue();
        }

        coroutine = null;
    }
}