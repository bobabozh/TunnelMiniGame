using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;


public abstract class MiniGameBase : SceneBase
{
    public enum GameCompleteReason
    {
        ABORTED_IN_TEN_SECONDS = -3,
        ABORTED_AFTER_TEN_SECONDS = -1,
        RESTARTED_AFTER_TEN_SECONDS = -2,
        FAILURE = 0,
        SUCCESS = 1
    }

    /// <summary> will be needed for managing mini game offer state </summary>
    public static DateTime lastTimeGameLaunched;

    public static double GetGameTime() // MOVED HERE FROM EXPERIENCE CLASS
    {
        return Utilities.CurrentTime;
    }

    // Time

    private double _gameStartTime;

    protected float GetTimeSpend()
    {
        return (float) (GetGameTime() - _gameStartTime - _pausedTimer);
    }

    [Header("Scene management")] public int _returnStepsBack = 1;

    [SerializeField] [Header("Time")] protected float _timeLimit = -1;
    [SerializeField] private bool _showTimer;

    // Score And Errors

    protected int _score;
    protected int _nErrors;

    // MiniGameParameters

    protected MGParameters _mgParameters;

    private bool _gameRestarted;
    protected bool gameRestarted => _gameRestarted;

    private bool _gameComplete;
    protected bool gameComplete => _gameComplete;

    private bool _gameStarted;
    protected bool gameStarted => _gameStarted;

    private bool _wasGameSuccessful;
    public bool wasGameSuccessful => _wasGameSuccessful;


    private float _expBeforeGame;
    private float _expBeforeGameTotal;
    //private float _totalExpGained;
    //public float totalExpGained => _totalExpGained;

    [Header("Game Finish")] [SerializeField]
    private bool _showCloseButton;

    [SerializeField] private float _endGameDelay;
    [SerializeField] protected bool showFireworksOnComplete = true;

    private float _pausedTimer;
    private float _pauseStartTime;
    
    protected float _lastSalute;

    protected GameObject _endGameButton;

    public virtual void SetMGParameters(MGParameters mgParameters)
    {
        _mgParameters = mgParameters;
        _timeLimit = mgParameters.timeLimit;
        _endGameOnTimeOut = mgParameters.endGameOnTimeOut;

        _timer.limit = _timeLimit;
    }

    [SerializeField] protected bool _debugMode;

    protected void DebugLog(string message)
    {
        if(_debugMode && AppManager.DebugMode)
            DebugManager.logMinor(GetType()+ " debug: " + message);
    }

    #region Some Getters For Fun

    protected Rect _blocksHouseRect => _mgParameters.sceneParams.blocksHouseRect;

    #endregion

    #region LetterGames

    protected char[] GenerateRandomKnownLetters(int num, bool withInitLetter = true, bool sameTypeLetters = false, char[] prohibitedLetters = null)
    {

        List<char> sameTypeLetterList = null;
        if (sameTypeLetters)
        {
            sameTypeLetterList = LetterUtility.GetLettersSameType(_mgParameters.sceneParams.initLetter).ToList();
            if (sameTypeLetterList.Count == 0)
            {
                sameTypeLetters = false;
            }
        }

        List<char> lettersKnown = _mgParameters.letters.known.ToList();

        Utilities.MixList(ref lettersKnown);

        if (withInitLetter)
        {
            lettersKnown = lettersKnown.Except(new char[] { _mgParameters.sceneParams.initLetter }).ToList();
        }

        if (prohibitedLetters != null)
        {
            lettersKnown = lettersKnown.Except(prohibitedLetters).ToList();
        }

        if (sameTypeLetters)
        {
            lettersKnown = lettersKnown.Intersect(sameTypeLetterList).ToList();
        }

        int deficit = withInitLetter ? num - (lettersKnown.Count + 1) : num - lettersKnown.Count;

        if (deficit > 0)
        {
            List <char> alphabet;
            if (withInitLetter)
            {
                alphabet = LetterUtility.Alphabet.Except(lettersKnown).Except(new char [] { _mgParameters.sceneParams.initLetter }).ToList();
            } else
            {
                alphabet = LetterUtility.Alphabet.Except(lettersKnown).ToList();
            }

            if (prohibitedLetters != null)
            {
                alphabet = alphabet.Except(prohibitedLetters).ToList();
            }

            if (sameTypeLetters)
            {
                List<char> alphabetSameType = alphabet.Intersect(sameTypeLetterList).ToList();
                if (alphabetSameType.Count >= deficit)
                {
                    alphabet = alphabetSameType;
                }
            }

            if (deficit > alphabet.Count)
            {
                deficit = alphabet.Count(); //при разумном использовании (не пытаемся найти слишком много букв) такого не должно происходить
            }

            int[] rndInd = Utilities.GenerateRandomUniqueIndexes(deficit, 0, alphabet.Count);

            for (int i=0; i<rndInd.Length; i++)
            {
                lettersKnown.Add(alphabet[rndInd[i]]);
            }
        }

        if (withInitLetter)
        {
            lettersKnown.Insert(0, _mgParameters.sceneParams.initLetter);
        }

        return lettersKnown.GetRange(0, num).ToArray();
    }

    #endregion

    #region Timer

    protected struct Timer
    {
        public float begin;
        public float limit;

        public float relative => begin == -1 ? 1 : begin == -2 ? 0 : Mathf.Max(1 - (Time.time - begin) / limit, 0);
        public float left => Mathf.Max(limit - Time.time + begin, 0);
        public float spend => Time.time - begin;

        public void Reset()
        {
            begin = Time.time;
        }

        public void Set(float value)
        {
            limit = value;
            Reset();
        }
    }

    protected Timer _timer;
    private bool _endGameOnTimeOut;
    private MiniGameTimer _miniGameTimer;
    private Vector2 _timerPadding = new Vector2(30, 30);

    protected void ShowTimer(bool value = true)
    {
        if (_miniGameTimer == null)
        {
            _miniGameTimer = Instantiate(CommonAssets.Instance.timerPrefab, transform);
            UpdateTimerPosition();
        }

        _miniGameTimer.gameObject.SetActive(value);
    }

    protected void UpdateTimerPosition()
    {
        _miniGameTimer.transform.position =
            _cameraController.Rect.max - Vector2.one * _miniGameTimer.radius - _timerPadding;
    }

    protected void SetTimerPosition()
    {
    }

    protected void SetTimerPadding(Vector2 padding)
    {
        ShowTimer(false);
        _timerPadding = padding;
        UpdateTimerPosition();
    }

    protected void SetTimerScale(float value)
    {
        ShowTimer(false);
        _miniGameTimer.transform.localScale = Vector3.one * value;
        UpdateTimerPosition();
    }

    protected void SetTimerRadius(float value = 50)
    {
        ShowTimer(false);
        _miniGameTimer.SetRadius(value);
        UpdateTimerPosition();
    }

    #endregion

    #region Utilities

    private Transform __container;

    protected Transform _container
    {
        get
        {
            if (__container == null)
            {
                __container = CreateContainer();
            }

            return __container;
        }
    }

    protected Transform CreateContainer(string name = "container")
    {
        GameObject cont = new GameObject();
        cont.name = name;
        cont.transform.SetParent(transform);

        SortingGroup sg = cont.AddComponent(typeof(SortingGroup)) as SortingGroup;
        sg.sortingLayerName = "MiniGameBase";
        return cont.transform;
    }

    public void SetBackground(Sprite sprite, Color spriteColor)
    {
        if (sprite == null)
            return;

        GameObject bg = new GameObject();
        bg.name = "background";
        bg.transform.SetParent(transform);
        bg.transform.SetAsFirstSibling();


        _background = bg.AddComponent(typeof(SpriteRenderer)) as SpriteRenderer;
        _background.sprite = sprite;
        _background.sortingOrder = -10;
        _background.color = spriteColor;
        _background.sortingLayerName = "spriteLayer0";
        _background.drawMode = SpriteDrawMode.Simple;
        //_background.transform.localScale = Vector2.one * 1.5f;
        _background.transform.localScale = Vector2.one * Math.Max(camProps.viewBounds.extents.x / sprite.bounds.extents.x, camProps.viewBounds.extents.y / sprite.bounds.extents.y);
    }

    protected void BuildSceneBoundsColliders()
    {
        BoxCollider2D coll;

        float w = 500;
        Vector2 paddings = Vector2.zero;

        coll = gameObject.AddComponent<BoxCollider2D>();
        coll.size = new Vector2(camProps.viewBounds.size.x + w * 2, w);
        coll.offset = new Vector2(0, camProps.viewBounds.size.y / 2 + w / 2 - paddings.y);

        coll = gameObject.AddComponent<BoxCollider2D>();
        coll.size = new Vector2(camProps.viewBounds.size.x + w * 2, w);
        coll.offset = new Vector2(0, -camProps.viewBounds.size.y / 2 - w / 2 + paddings.y);


        coll = gameObject.AddComponent<BoxCollider2D>();
        coll.size = new Vector2(w, camProps.viewBounds.size.y + w * 2);
        coll.offset = new Vector2(camProps.viewBounds.size.x / 2 + w / 2 - paddings.x, 0);

        coll = gameObject.AddComponent<BoxCollider2D>();
        coll.size = new Vector2(w, camProps.viewBounds.size.y + w * 2);
        coll.offset = new Vector2(-camProps.viewBounds.size.x / 2 - w / 2 + paddings.x, 0);
        
    }

    protected void StopIdleMovingsAndTweenables()
    {
        foreach (var t in FindObjectsOfType<TweenableObject>())
            t.Stop();
        foreach (var t in FindObjectsOfType<IdleMovingObject>())
        {
            //t.SetUpdateType(IdleMovingObject.UpdateType.Update);
            t.savePosition = false;
        }
    }

    #endregion

    #region Effects

    protected void Mistake(MonoBehaviour target, bool useTargetTransform = false)
    {
        Mistake(target.transform.position, useTargetTransform ? target.transform : transform);
    }

    protected void Mistake(Vector3 pos, Transform parent = null)
    {
        if (parent == null)
            parent = transform;

        pos.z -= 1;

        GameObject effect = Instantiate(CommonAssets.Instance.effectCirclesRed, parent);
        effect.transform.position = pos;
    }

    protected void Success(MonoBehaviour target, bool useTargetTransform = false)
    {
        Success(target.transform.position, useTargetTransform ? target.transform : transform);
    }

    protected void Success(Vector3 pos, Transform parent = null)
    {
        if (parent == null)
            parent = transform;

        pos.z -= 1;

        GameObject effect = Instantiate(CommonAssets.Instance.effectCirclesGreen, parent);
        effect.transform.position = pos;
        effect.GetComponentInChildren<ParticleSystem>().GetComponent<ParticleSystemRenderer>().sortingOrder += 100;
    }

    #endregion

    #region Sound

    protected float PlayRightSound()
    {
        return AppManager.AudioManager.PlayRightSound();
    }

    protected void PlayWrongLetterSound()
    {
        AppManager.AudioManager.PlayWrongLetterSound();
    }

    protected void PlaySuccessSound()
    {
        AppManager.AudioManager.PlayPingSound();
    }

    protected void PlayFailureSound()
    {
        AppManager.AudioManager.PlayFailureSound();
    }


    protected virtual void PlayMusic()
    {
        if (!AudioManager.Instance.IsPLaying(SoundType.Music))
            AudioManager.Instance.PlayMiniGameMusic();
    }

    protected virtual void StopMusic()
    {
        AudioManager.Instance.StopMiniGameMusic();
    }

    #endregion

    #region Game State

    public enum GameState
    {
        INITED = 1,
        INTRO = 2,
        READY = 4,
        INPROGRESS = 8,
        PAUSED = 16,
        TIMEOUT = 32,
        COMPLETION = 64,
        FINISHED = 128
    }

    public static byte GetStateMask(GameState state)
    {
        return (byte) state;
    }

    public static byte GetStateMask(GameState[] states)
    {
        byte mask = 0;

        for (int i = 0; i < states.Length; i++)
        {
            mask += (byte) states[i];
        }

        return mask;
    }

    private GameState _currentState;

    protected void SetGameState(GameState value)
    {
        _currentState = value;
        InteractiveObject.state = (byte) _currentState;
    }

    public bool CheckGameState(GameState value)
    {
        return _currentState == value;
    }

    public bool CheckGameState(GameState stateA, GameState stateB)
    {
        return stateA <= _currentState && stateB >= _currentState;
    }

    protected void Pause()
    {
        SetGameState(GameState.PAUSED);

        _pauseStartTime = Time.time;
    }

    protected void Play()
    {
        if (_currentState == GameState.PAUSED)
        {
            _pausedTimer += Time.time - _pauseStartTime;
        }

        SetGameState(GameState.INPROGRESS);
    }

    protected void Ready()
    {
        SetGameState(GameState.READY);
    }

    #endregion

    #region Mini Game Methods 

    #region Init and Content
    
    public override void CreateSpecificContent(NavigationController navController, CameraController cameraController)
    {
        base.CreateSpecificContent(navController, cameraController);

        if (_mgParameters != null)
            if (_mgParameters.sceneParams != null)
                SetBackground(_mgParameters.sceneParams.backgroundSprite, _mgParameters.sceneParams.backgroundColor);

        CreateGameContent();

        lastTimeGameLaunched = DateTime.Now;
    }

    public override void LaunchScene()
    {
        base.LaunchScene();

        _expBeforeGame = Experience.GetExperience.Sum.unique;
        _expBeforeGameTotal = Experience.GetExperience.Sum.total;

        StartGame();
    }
    
    public virtual void CreateGameContent()
    {
    }

    public virtual void RemoveGameContent()
    {
        step.Stop();

        _timer.begin = -1;
        RemoveCloseButton();

        StopMusic();

        _gameComplete = false;
        _gameStarted = false;

        _nErrors = 0;
        _score = 0;

        if (_showTimer)
            ShowTimer(false);
    }

    #endregion

    #region Start

    public virtual void StartGame()
    {
        SetGameState(GameState.INTRO);
    }
    
    #endregion

    #region Restart
    public override void OnRestartClicked()
    {
        base.OnRestartClicked();
        
        RemoveCloseButton();

        _expBeforeGame = Experience.GetExperience.Sum.unique;
        _expBeforeGameTotal = Experience.GetExperience.Sum.total;

        RestartGame();
    }
    public virtual void RestartGame()
    {
        _gameRestarted = true;

        // TODO - учесть (может быть сохранить результаты)
        // + перегенерить параметры

        if (!_gameComplete)
            if (gameStarted && GetTimeSpend() > 10)
            {
                CalculateAndSendExperience();
                RegisterGamePlayed(GameCompleteReason.RESTARTED_AFTER_TEN_SECONDS);
            }

        RemoveGameContent();

        SetGameState(GameState.INITED);

        CreateGameContent();

        StartGame();
    }
    
    #endregion

    #region Time
    public virtual void StartTime()
    {
        SetGameState(GameState.INPROGRESS);
        PlayMusic();

        _gameStartTime = GetGameTime();
        _gameStarted = true;

        _pausedTimer = 0;
        
        WakeUpReset();

        StartTimer();
    }

    protected void StartTimer()
    {
        if (_timer.limit >= 0)
        {
            _timer.begin = Time.time;

            if (_showTimer)
                ShowTimer();
        }
    }
    
    private void TimeOut()
    {
        _timer.begin = -2;
        if (_showTimer)
            _miniGameTimer.UpdateTimer(0, 0);

        OnTimeOut();
    }

    protected virtual void OnTimeOut()
    {
        if (_endGameOnTimeOut)
            SetGameState(GameState.TIMEOUT);
    }

    #endregion

    #region Completion
    protected bool CheckGameComplete(bool autoComplete = false, float delay = 0)
    {
        _gameComplete = IsGameComplete();
        
        if(_gameComplete)
            SetGameState(GameState.COMPLETION);
        
        if(_gameComplete && autoComplete)
            if (delay <= 0)
                GameComplete();
            else
                step.StartCoroutine(GameCompleteCoroutine(delay));
        
        return _gameComplete;
    }

    private IEnumerator GameCompleteCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameComplete();
    }

    protected virtual bool IsGameComplete()
    {
        return false;
    }
    
    public virtual void GameComplete()
    {
        //чтобы обновить таймер
        Play();
        
        SetGameState(GameState.COMPLETION);
        StopMusic();

        _gameComplete = true;

        _wasGameSuccessful = CheckWasGameSuccessful();

        //_totalExpGained = CalculateAndSendExperience();
        CalculateAndSendExperience();

        RegisterGamePlayed(_wasGameSuccessful ? GameCompleteReason.SUCCESS : GameCompleteReason.FAILURE);

        if (showFireworksOnComplete)
            MakeSalut();

        Experience.Save();

        if (_showTimer)
            ShowTimer(false);
    }

    protected void MakeSalut(bool checkTime = false)
    {
        if (checkTime && Time.time - _lastSalute < 5)
            return;
        
        _viewController.MakeSalut();
        _lastSalute = Time.time;
    }

    protected abstract bool CheckWasGameSuccessful();
    protected abstract float CalculateAndSendExperience();


    public virtual void FinishGame()
    {
        SetGameState(GameState.FINISHED);

        ShowExperienceGain();

        ShowCloseButton();
    }

    protected virtual void FinishGame(bool showCloseButton = true, float delay = 0)
    {
        SetGameState(GameState.FINISHED);

        ShowExperienceGain();

        if (showCloseButton)
            ShowCloseButton(delay);
        else
            QuitGame(delay);
    }

    public virtual void QuitGame(float delay = -1)
    {
        if (delay > 0)
        {
            step.StartCoroutine(QuitGameCoroutine(delay));
            return;
        }

        if (!_gameComplete)
        {
            if (_gameStarted)
            {
                if (GetTimeSpend() < 10)
                    RegisterGamePlayed(GameCompleteReason.ABORTED_IN_TEN_SECONDS);
                else
                    RegisterGamePlayed(GameCompleteReason.ABORTED_AFTER_TEN_SECONDS);

                CalculateAndSendExperience();
            }
        }

        ReturnToPrevScene();
    }

    protected virtual void ReturnToPrevScene()
    {
        _navigationController.ReturnBackByNScenes(_returnStepsBack);
    }

    private IEnumerator QuitGameCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        QuitGame();
    }
    

    protected virtual void RegisterGamePlayed(GameCompleteReason reason)
    {
        // статический вызов окончания обработки игры !!!
        if (_mgParameters.sceneParams != null)
            if (_mgParameters.sceneParams.onGameEnded != null)
                _mgParameters.sceneParams.onGameEnded.Invoke((int) reason);

        DebugManager.Log($"Game Complete: " + $"{GetType()} (ID: {_mgParameters.miniGameData.gameID})" +
              $"\nResult: {reason}" +
              $"\nComplexity: {_mgParameters.miniGameData.complexityLevel}\n\n" +
              GetGameDebugResult());

        _viewController.MiniGameController.RegisterGamePlayed(_mgParameters.miniGameData,
            _mgParameters.miniGameData.complexityLevel, (int) reason, GetTimeSpend(),
            _mgParameters.miniGameData.complexityLevel);

        TrackGamePlayed(reason);
    }

    protected virtual string GetGameDebugResult()
    {
        string res =
            $"Time: {GetTimeSpend()} / {_timeLimit}" +
            $"\nErrors: {_nErrors}";

        return res;
    }

    protected virtual void TrackGamePlayed(GameCompleteReason reason)
    {
        AppManager.EventTracker.track_miniGame(_mgParameters.miniGameData.gameID,
            (int) _mgParameters.miniGameData.gameCategory, _mgParameters.complexityShort, (int) reason,
            (long) GetTimeSpend());
    }

    protected virtual void ShowExperienceGainWithDelay(float delay)
    {
        step.StartCoroutine(ShowExperienceGainCoroutine(delay));
    }

    private IEnumerator ShowExperienceGainCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        ShowExperienceGain();
    }

    protected virtual void ShowExperienceGain()
    {
        float gain = Experience.GetExperience.Sum.unique - _expBeforeGame;
        float totalGain = Experience.GetExperience.Sum.total - _expBeforeGameTotal;

        uint totalCapacity = (uint) Experience.GetExperience.TotalCapacity; //What is that?..

        // а еще летят шарики
        // самое большое количество - при добавлении уникального опыта. Но и тае полетят в зависимости от объема упражнения. Пять шариков соотвесттвуют примерно 5 единицам тотального опыта        
        int nBalloons = (int) Mathf.Max(5, Mathf.Min((gain + totalGain) * totalCapacity * 2, 30));

        //Debug.Log("n balloons = " + nBalloons);

        MiniGameExperienceShow expShow = Instantiate(CommonAssets.Instance.experienceShowPrefab, _viewController.MainCanvas.transform);
        expShow.transform.SetAsLastSibling();
        expShow.ShowExperience(nBalloons, _cameraController.Rect);
    }

    
    #endregion

    #region Close Button

    private Vector2 _customCloseButtonPosition;
    private bool _useCustomCloseButtonPosition;

    private Coroutine _closeButtonCoroutine;

    protected void SetCloseButtonPosition(Vector2 position)
    {
        _useCustomCloseButtonPosition = true;
        _customCloseButtonPosition = position;
    }

    protected void ShowCloseButton(float delay = 0)
    {
        _closeButtonCoroutine = step.StartCoroutine(ShowCloseButtonCoroutine(delay));
    }

    protected virtual void RemoveCloseButton()
    {
        if (_endGameButton != null)
            Destroy(_endGameButton.gameObject);
        
        if(_closeButtonCoroutine!=null)
            step.StopCoroutine(_closeButtonCoroutine);
    }


    private IEnumerator ShowCloseButtonCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        _endGameButton = Instantiate(CommonAssets.Instance.okButtonPrefab, transform);

        if (_useCustomCloseButtonPosition)
            _endGameButton.transform.position = _customCloseButtonPosition;
        else
            _endGameButton.transform.position =
                new Vector2(_cameraController.Rect.xMax - 100, _cameraController.Rect.yMin + 100);

        _endGameButton.GetComponent<InteractiveObject>().AddNewAction(InputEventType.Click, OnCloseButtonClick);
    }

    private void OnCloseButtonClick(InteractiveObject io, TouchInfo touch)
    {
        AudioManager.Instance.PlayClickSound();
        QuitGame();
    }
    
    #endregion

    public override void OnBackButtonClick()
    {
        QuitGame();
    }

    #endregion

    #region Update

    public override void UpdateLoop()
    {
        base.UpdateLoop();

        if (_timer.limit > 0 && _timer.left == 0 && _timer.begin > 0)
            TimeOut();

        if (_showTimer && _timer.limit > 0 && _miniGameTimer != null)
            _miniGameTimer.UpdateTimer(_timer.left, _timer.relative);

#if UNITY_EDITOR

        if (Input.GetKeyDown(KeyCode.R))
        {
            //АХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХАХ!!!!!
            //RestartGame();
        }

#endif

        MiniGameUpdateLoop();
    }

    public virtual void MiniGameUpdateLoop()
    {
    }

    #endregion
}