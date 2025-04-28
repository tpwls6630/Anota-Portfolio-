using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DialogueLogManager : MonoBehaviour
{

    #region 다이얼로그 로그 자료구조
    public abstract class DialogueLogBase
    {
        protected static DialogueLogManager LogManager;
        protected static GameObject LogViewPort => LogManager.LogViewPort;
        protected GameObject DialogueLogObject;

        public static void SetLogManager(DialogueLogManager logManager)
        {
            if (LogManager == null)
            {
                LogManager = logManager;
            }
        }
    }

    public class DialogueLogSpacer : DialogueLogBase
    {
        private static GameObject Spacer => LogManager.SpacerObject;

        public DialogueLogSpacer()
        {
            GameObject gameObject = Instantiate(Spacer, LogViewPort.transform);
            DialogueLogObject = gameObject;

            gameObject.SetActive(true);
        }
    }



    public class DialogueLog : DialogueLogBase
    {
        // Instantiate할 때 사용할 기본 오브젝트
        private static GameObject LeftNormal => LogManager.LogObjectBase_LeftNormal;
        private static GameObject LeftTail => LogManager.LogObjectBase_LeftTail;
        private static GameObject RightNormal => LogManager.LogObjectBase_RightNormal;
        private static GameObject RightTail => LogManager.LogObjectBase_RightTail;

        private Image CharacterSprite;
        private TextMeshProUGUI Name;
        private TextMeshProUGUI Message;

        public DialogueLog(string message, Sprite characterSprite, string characterName, bool isPlayer, bool isNeedTail)
        {
            GameObject gameObject;
            if (isPlayer)
            {
                gameObject = Instantiate(isNeedTail ? RightTail : RightNormal, LogViewPort.transform);
            }
            else
            {
                gameObject = Instantiate(isNeedTail ? LeftTail : LeftNormal, LogViewPort.transform);
            }

            DialogueLogObject = gameObject;

            CharacterSprite = gameObject.transform.Find("Character/Sprite")?.GetComponent<Image>();
            Name = gameObject.transform.Find("Name/Name_Text")?.GetComponent<TextMeshProUGUI>();
            Message = gameObject.transform.Find("Textbox/Textbox_Text").GetComponent<TextMeshProUGUI>();

            Message.text = message;

            if (CharacterSprite != null && characterSprite != null)
            {
                CharacterSprite.sprite = characterSprite;
            }

            if (Name != null && string.IsNullOrEmpty(characterName))
            {
                Name.gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
        }


    }
    #endregion

    [SerializeField] private GameObject LogViewPort;
    [SerializeField] private GameObject LogObjectBase_LeftNormal;
    [SerializeField] private GameObject LogObjectBase_LeftTail;
    [SerializeField] private GameObject LogObjectBase_RightNormal;
    [SerializeField] private GameObject LogObjectBase_RightTail;
    [SerializeField] private GameObject SpacerObject;
    
    

    private List<DialogueLogBase> DialogueLogs = new List<DialogueLogBase>();
    private CanvasGroup canvasGroup;
    private static string _lastCharacterName = "";

    public void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (InputManager.Instance.GetDialogueEscapeDown())
        {
            Show(false);
        }
    }

    public void AddDialogueLog(string message, string characterName, Sprite characterSprite, bool isPlayer)
    {
        Debug.Log($"AddDialogueLog : {characterName} / {message}");
        DialogueLogBase.SetLogManager(this);

        if (IsNeedSpacer(characterName))
        {
            DialogueLogs.Add(new DialogueLogSpacer());
            _lastCharacterName = "";
        }

        DialogueLogs.Add(new DialogueLog
        (
            message,
            characterSprite,
            characterName,
            isPlayer,
            IsNeedTail(characterName)
        ));

        _lastCharacterName = characterName;
    }

    public void Show(bool show)
    {
        if (show)
        {
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, 1f);
        }
        
        if (!show)
        {
            canvasGroup.DOFade(0, 1f).OnComplete(() =>
            {
                DialogueMenuManager.Instance.OnLog_Close();
                gameObject.SetActive(false);
            });
        }
        
    }

    public bool IsNeedSpacer(string newCharacterName)
    {
        if (DialogueLogs.Count == 0)
        {
            return false;
        }

        // 마지막 로그가 Spacer라면 false
        if (DialogueLogs[DialogueLogs.Count - 1] is DialogueLogSpacer)
        {
            return false;
        }
        else
        {
            Debug.Log($"IsNeedSpacer : {_lastCharacterName} / {newCharacterName}");
            return _lastCharacterName.Equals(newCharacterName);
        }
    }

    private bool IsNeedTail(string newCharacterName)
    {
        if (string.IsNullOrEmpty(newCharacterName))
        {
            return false;
        }

        return _lastCharacterName.Equals(newCharacterName);
    }

    public void OnScreenClick()
    {
        Show(false);
    }
}
