using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    private PlayerInput _playerInput;
    private DialogueInput _dialogueInput;
    private bool _isUIActive = false;
    private bool _isDialogueActive = false;

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

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.Enable();
        _dialogueInput = new DialogueInput();
        _dialogueInput.Disable();

        _playerInput.Player.Move.performed += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
        _playerInput.Player.Move.canceled += ctx => OnMoveInput(Vector2.zero);
    }

    void Update()
    {
        while (_playerMoveQueue.Count > 0 && Time.time - _playerMoveQueue.Peek().timeStamp > _inputBufferTime)
        {
            _playerMoveQueue.Dequeue();
        }
    }

    public void SetUIActive(bool isActive)
    {
        _isUIActive = isActive;
        if (isActive)
        {
            // UI가 활성화되면 게임플레이 입력을 비활성화
            _playerInput.Player.Disable();
        }
        else
        {
            // UI가 비활성화되면 게임플레이 입력을 활성화
            _playerInput.Player.Enable();
        }
    }

    public void SetDialogueActive(bool isActive)
    {
        _isDialogueActive = isActive;
        if (isActive)
        {
            _playerInput.Disable();
            _dialogueInput.Enable();
        }
        else
        {
            _playerInput.Enable();
            _dialogueInput.Disable();
        }
    }

    public Vector2 GetPlayerMoveInput()
    {
        // UI가 활성화되어 있으면 이동 입력을 무시
        if (_isUIActive || _isDialogueActive)
            return Vector2.zero;
        return _playerMoveQueue.Count > 0 ? _playerMoveQueue.Dequeue().direction : Vector2.zero;
    }

    public bool GetPlayerResetInputDown()
    {
        // UI가 활성화되어 있으면 리셋 입력을 무시
        if (_isUIActive || _isDialogueActive)
            return false;
        return _playerInput.Player.Reset.WasPerformedThisFrame();
    }

    public Vector2 GetUIMoveDown()
    {
        if (_isDialogueActive)
            return Vector2.zero;
        return _playerInput.UI.Navigate.ReadValue<Vector2>();
    }

    public bool GetUISubmitDown()
    {
        if (_isDialogueActive)
            return false;
        return _playerInput.UI.Submit.WasPerformedThisFrame();
    }

    public bool GetUICancelDown()
    {
        if (_isDialogueActive)
            return false;
        return _playerInput.UI.Cancel.WasPerformedThisFrame();
    }

    public bool GetUISaveDefaultDown()
    {
        if (_isDialogueActive)
            return false;
        return _playerInput.UI.SaveDefault.WasPerformedThisFrame();
    }

    public bool GetDialogueInputDown()
    {
        return _dialogueInput.Dialogue.Next.WasPerformedThisFrame();
    }

    public bool GetDialogueEscapeDown()
    {
        return _dialogueInput.Dialogue.Escape.WasPerformedThisFrame();
    }
}
