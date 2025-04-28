using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueMenuManager : MonoBehaviour
{
    private static DialogueMenuManager _instance;

    public static DialogueMenuManager Instance
    {
        get
        {
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    [SerializeField] private GameObject DialogueMenu;
    [SerializeField] private GameObject Btn_Esc;
    [SerializeField] private GameObject Btn_Skip;
    [SerializeField] private GameObject Btn_Log;
    [SerializeField] private GameObject Btn_Auto;
    private CanvasGroup _canvasGroup;

    private bool _isMenuOpen;
    public bool IsMenuOpen
    {
        get { return _isMenuOpen; }
        set
        {
            _isMenuOpen = value;
            MenuAnimation(IsMenuOpen);
            if (IsMenuOpen)
            {
                EventSystem.current.SetSelectedGameObject(Btn_Skip);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private Sequence DropdownOpenSequence;
    private Sequence DropdownCloseSequence;
    private readonly float DropdownFadeDuration = 0.5f;

    private bool IsLogOpen { get => DialogueManager.Instance.DialogueLogManager.gameObject.activeSelf; }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        _canvasGroup = GetComponent<CanvasGroup>();

        IsMenuOpen = false;

        SetMenusInteractable(false);

        DropdownOpenSequence = DOTween.Sequence();
        DropdownOpenSequence.AppendCallback(() =>
        {
            SetMenusInteractable(true);
        })
                        .Append(Btn_Esc.GetComponent<CanvasGroup>().DOFade(0, DropdownFadeDuration))
                        .Join(Btn_Skip.GetComponent<CanvasGroup>().DOFade(1, DropdownFadeDuration))
                        .Join(Btn_Log.GetComponent<CanvasGroup>().DOFade(1, DropdownFadeDuration))
                        .Join(Btn_Auto.GetComponent<CanvasGroup>().DOFade(1, DropdownFadeDuration))
                        .Join(Btn_Log.GetComponent<RectTransform>().DOAnchorPosY(-80 + 4, DropdownFadeDuration).SetRelative())
                        .Join(Btn_Auto.GetComponent<RectTransform>().DOAnchorPosY(-160 + 8, DropdownFadeDuration).SetRelative())
                        .SetAutoKill(false)
                        .Pause();

        DropdownCloseSequence = DOTween.Sequence();
        DropdownCloseSequence.AppendCallback(() =>
        {
            SetMenusInteractable(false);
        })
                        .Append(Btn_Esc.GetComponent<CanvasGroup>().DOFade(1, DropdownFadeDuration))
                        .Join(Btn_Skip.GetComponent<CanvasGroup>().DOFade(0, DropdownFadeDuration))
                        .Join(Btn_Log.GetComponent<CanvasGroup>().DOFade(0, DropdownFadeDuration))
                        .Join(Btn_Auto.GetComponent<CanvasGroup>().DOFade(0, DropdownFadeDuration))
                        .Join(Btn_Log.GetComponent<RectTransform>().DOAnchorPosY(80 - 4, DropdownFadeDuration).SetRelative())
                        .Join(Btn_Auto.GetComponent<RectTransform>().DOAnchorPosY(160 - 8, DropdownFadeDuration).SetRelative())
                        .SetAutoKill(false)
                        .Pause();

    }

    private void Update()
    {
        if (!IsLogOpen)
        {
            if (InputManager.Instance.GetDialogueEscapeDown())
            {
                IsMenuOpen = !IsMenuOpen;
            }
        }
    }

    public void OnClickBtn_Esc()
    {
        Debug.Log("OnClickBtn_Esc");
        IsMenuOpen = true;
    }

    public void OnClickBtn_Skip()
    {
        Debug.Log("OnClickBtn_Skip");
        DialogueManager.Instance.ExitDialogue();
    }

    public void OnClickBtn_Log()
    {
        Debug.Log("OnClickBtn_Log");
        _canvasGroup.interactable = false;
        DialogueManager.Instance.DialogueLogManager.Show(true);
    }

    public void OnLog_Close()
    {
        Debug.Log("OnClickBtn_Log_Close");
        _canvasGroup.interactable = true;
    }

    public void OnClickBtn_Auto()
    {
        Debug.Log("OnClickBtn_Auto");
        if (DialogueManager.Instance.IsAutoExecuting)
        {
            DialogueManager.Instance.AutoDialogue(false);
        }
        else
        {
            DialogueManager.Instance.AutoDialogue(true);
        }
    }

    /// <summary>
    /// 메뉴가 열려있을 때, 아무 곳이나 클릭하면 메뉴를 닫는다.
    /// </summary>
    /// <returns>메뉴가 열려있었는가</returns>
    public bool IsMenuOpendOnClickAnyware()
    {
        if (IsMenuOpen)
        {
            IsMenuOpen = false;
            return true;
        }
        return false;
    }

    private void MenuAnimation(bool isActive)
    {
        if (isActive)
        {
            Debug.Log("OpenMenu true");
            DropdownCloseSequence.Pause();
            DropdownOpenSequence.Restart();
        }
        else
        {
            Debug.Log("OpenMenu false");
            DropdownOpenSequence.Pause();
            DropdownCloseSequence.Restart();
        }
    }

    private void SetMenusInteractable(bool isMenuOpen)
    {
        Btn_Esc.GetComponent<CanvasGroup>().interactable = !isMenuOpen;
        Btn_Skip.GetComponent<CanvasGroup>().interactable = isMenuOpen;
        Btn_Log.GetComponent<CanvasGroup>().interactable = isMenuOpen;
        Btn_Auto.GetComponent<CanvasGroup>().interactable = isMenuOpen;

        Btn_Esc.GetComponent<CanvasGroup>().blocksRaycasts = !isMenuOpen;
        Btn_Skip.GetComponent<CanvasGroup>().blocksRaycasts = isMenuOpen;
        Btn_Log.GetComponent<CanvasGroup>().blocksRaycasts = isMenuOpen;
        Btn_Auto.GetComponent<CanvasGroup>().blocksRaycasts = isMenuOpen;
    }

}
