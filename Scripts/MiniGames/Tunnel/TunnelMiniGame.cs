using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class TunnelMiniGame : MiniGameBase
{
    [Header("Tunnel Mini Game")] [SerializeField]
    private TunnelPackMan _packMan;

    [SerializeField] private TunnelWord _wordPrefab;

    [SerializeField] private Tilemap _tunnelTileMap;
    [SerializeField] private TileBase _tunnelTile;
    [SerializeField] private Transform _tileMapContainer;
    [Header("Stars")]
    [SerializeField] private TweenableObject _starPrefab;

    [SerializeField] private Transform[] _starsGray;
    [SerializeField] private Transform _starsContainer;
    private List<TweenableObject> _stars = new List<TweenableObject>();

    [Space] [SerializeField] private ParticleSystem _deathEffect;

    [Header("Sound")] 
    [SerializeField] private AudioClipWithVolume _introSound;
    [SerializeField] private AudioClipWithVolume _manualSound;
    [SerializeField] private AudioClipWithVolume[] _eatableSound;
    [SerializeField] private AudioClipWithVolume[] _notEatableSound;
    [SerializeField] private AudioClipWithVolume _endGameSound;

    private float _tunnelScale = 1;
    private int _centerTileX;
    private int _centerTileTunnelCenter;
    private float _tileWidth;

    private Vector2Int _tunnelSize;
    private Vector2 _tunnelOffset;

    [Header("Background")] [SerializeField]
    private Renderer _gameBackground;

    private float _packmanMaxSpeed = 200;
    private float _packmanSpeed;
    private float _backgoundMaxSpeed;
    private Vector2 _backgroundOffset;
    private float _backgroundSpeed;
    private Tween _backgroundSpeedTween;
    private float _backgroundAccelerationTime = 0.5f;

    [SerializeField] private float _packmanSize = 100;

    [SerializeField][Range(0, 1)] private float _eatableChance;
    [SerializeField] private float _wordSpawnDistance = 700;
    [SerializeField] private float _wordSpawnDistanceRange = 0.3f;
    [SerializeField] private int _maxEatableInARaw = 1;
    [SerializeField] private int _maxNotEatableInARaw = 3;
    
    private float _nextWordSpawnX;
    private List<WordData_Thin> _wordsEatable = new List<WordData_Thin>();
    private List<WordData_Thin> _wordsNotEatable = new List<WordData_Thin>();

    private string _prevEatableWord;
    private string _prevWord;

    private int _eatableWordsSpawned;
    private int _notEatableWordsSpawned;

    private int _successSpree;

    private List<TunnelWord> _words = new List<TunnelWord>();
    private List<string> _wordsEaten = new List<string>();

    private int _wordComplexity;
    private bool _colorizedSklads;

    private new TunnelMGParameters _mgParameters => (TunnelMGParameters)base._mgParameters;
   
    
    public override void SetMGParameters(MGParameters mgParameters)
    {
        base.SetMGParameters(mgParameters);

        TunnelMGParameters parameters = (TunnelMGParameters) mgParameters;

        _packmanMaxSpeed = parameters.tunnelSpeed;
        _wordComplexity = parameters.wordComplexity;
        _colorizedSklads = parameters.colorizedSklads;
    }
    public override void LaunchScene()
    {
        base.LaunchScene();
        
        _packMan.onCrash += OnPackmanCrash;
        _packMan.onDragBegin += OnPackmanDragBegin;
        _packMan.onDragEnd += OnPackmanDragEnd;

        _packMan.Init();
    }

    public override void CreateGameContent()
    {
        base.CreateGameContent();

        _gameBackground.transform.localScale = _cameraController.Size*1.1f;
        _gameBackground.material.mainTextureScale = _gameBackground.transform.localScale / 100;

        _gameBackground.sortingLayerName = "Default";
        _gameBackground.sortingOrder = -1;
        
        _background.gameObject.SetActive(false);

        _tunnelSize.x = Mathf.CeilToInt(_cameraController.Size.x / _tunnelTileMap.cellSize.x / _tunnelScale / 2) + 2;
        _tunnelSize.y = Mathf.CeilToInt(_cameraController.Size.y / _tunnelTileMap.cellSize.y / _tunnelScale / 2) + 2;
        
        _starsContainer.position =
            new Vector2(_cameraController.Rect.xMin, _cameraController.Rect.yMax) + new Vector2(30, -30);

        _tileWidth = _tunnelTileMap.cellSize.x * _tunnelScale;
        
        _packmanSpeed = _packmanMaxSpeed;
        _backgoundMaxSpeed = _packmanSpeed / _tileWidth;

        GenerateTunnel();
        GenerateWords();

        _packMan.SetSize(_packmanSize);
        _packMan.transform.position = GetTunnelCenter();
        _packMan.Remove(-1);
    }

    private void GenerateWords()
    {
        List<WordData_Thin> allWords = null;

        if (_wordComplexity == 0)
        {
            allWords = CommonAssets.Texts.GetAllWordDatasBelowLevel(level: 1, andBelow: true, onlyActive: false, blockCount: 2, letterCount: 3);
        }
        else
        {
            allWords = CommonAssets.Texts.GetAllWordDatasBelowLevel(level: _wordComplexity, andBelow: true, onlyActive: false, blockCount: -1, letterCount: -1);
        }

        _wordsEatable.Clear();
        _wordsNotEatable.Clear();

        for (int i = 0; i < allWords.Count; i++)
        {
            if (allWords[i].eatable == 1)
                _wordsEatable.Add(allWords[i]);
            else if(allWords[i].eatable == 0)
                _wordsNotEatable.Add(allWords[i]);
        }

        //!TEST
        /*
        foreach (WordData_Thin w in _wordsEatable)
        {
            string w1 = w.word.Replace("^", "");
            //if (Random.Range(0,1.0f) > 0.42f)
            //    Experience.GetExperience.AddReadExperienceForChtenieSlovo(w1);
            if (Random.Range(0, 1.0f) > 0.98f)
                Experience.GetExperience.AddExperienceForUnhintedChtenie(w1,1);
        }
        foreach (WordData_Thin w in _wordsNotEatable)
        {
            string w1 = w.word.Replace("^", "");
            //if (Random.Range(0, 1.0f) > 0.42f)
            //    Experience.GetExperience.AddReadExperienceForChtenieSlovo(w1);
            if (Random.Range(0, 1.0f) > 0.98f)
                Experience.GetExperience.AddExperienceForUnhintedChtenie(w1, 1);
        }
        */

    }

    public override void RemoveGameContent()
    {
        base.RemoveGameContent();
        
        _tunnelTileMap.ClearAllTiles();

        for (int i = 0; i < _words.Count; i++)
        {
            Destroy(_words[i].gameObject);
        }
        
        _words.Clear();

        for (int i = 0; i < _stars.Count; i++)
        {
            Destroy(_stars[i].gameObject);
        }
        
        _stars.Clear();
        
        _wordsEaten.Clear();
        
        _nextWordSpawnX = 0;
        _successSpree = 0;
        
        _packMan.Remove(-1);
    }

    public override void StartGame()
    {
        base.StartGame();

        _packMan.Spawn(0.3f);
        
        step.Delay(0.5f);
        step.Add(StartTime, true);
        step.Add(PlayIntroSound);
        step.Add(PlayManualSound);
        step.Play();
    }

    private void PlayIntroSound()
    {
        step.Next(AudioManager.Instance.Play(SoundType.Voice, _introSound));
    }

    private void PlayManualSound()
    {
        step.Next(AudioManager.Instance.Play(SoundType.Voice, _manualSound));
    }

    public override void StartTime()
    {
        base.StartTime();
        
        PlayMusic();
    }

    private void OnPackmanDragBegin(InteractiveObject io)
    {
        if(_backgroundSpeedTween!=null && _backgroundSpeedTween.active)
            _backgroundSpeedTween.Kill();
        
        _backgroundSpeedTween = DOTween.To(() => _backgroundSpeed, x => _backgroundSpeed = x, _backgoundMaxSpeed,
            _backgroundAccelerationTime);
    }

    private void OnPackmanDragEnd(InteractiveObject io)
    {
        if(_backgroundSpeedTween!=null && _backgroundSpeedTween.active)
            _backgroundSpeedTween.Kill();
        
        _backgroundSpeedTween =
            DOTween.To(() => _backgroundSpeed, x => _backgroundSpeed = x, 0, _backgroundAccelerationTime);
    }

    private int _tunnelCenter;
    private int _tunnelWidth = 2;

    private void ClearTunnelColumn(int x)
    {
        Vector3Int[] positions = new Vector3Int[_tunnelSize.y*2 + 1];
        TileBase[] tiles = new TileBase[_tunnelSize.y*2 + 1];
        int tunnelWidth = 2;

        int n = 0;
        
        for (int y = -_tunnelSize.y; y <= _tunnelSize.y; y++)
        {
            positions[n] = new Vector3Int(x, y, 0);
            n++;
        }

        _tunnelTileMap.SetTiles(positions, tiles);
    }
    
    private void GenerateTunnelColumn(int x){
        
        Vector3Int[] positions = new Vector3Int[_tunnelSize.y*2 + 1];
        TileBase[] tiles = new TileBase[_tunnelSize.y*2 + 1];

        int n = 0;
        
        for (int y = -_tunnelSize.y; y <= _tunnelSize.y; y++)
        {
            positions[n] = new Vector3Int(x, y, 0);
            
            if (y < _tunnelCenter - _tunnelWidth || y > _tunnelCenter + _tunnelWidth)
            {
                tiles[n] = _tunnelTile;
            }

            n++;
        }

        _tunnelCenter += Random.Range(-1, 2);

        if (_tunnelCenter > _tunnelSize.y -_tunnelWidth - 2)
            _tunnelCenter = _tunnelSize.y -_tunnelWidth - 2;
        if (_tunnelCenter < -_tunnelSize.y +_tunnelWidth + 2)
            _tunnelCenter = -_tunnelSize.y +_tunnelWidth + 2;
        
        _tunnelTileMap.SetTiles(positions, tiles);
    }

    private void GenerateTunnel()
    {
        for (int x = _centerTileX - _tunnelSize.x; x <= _centerTileX + _tunnelSize.x; x++)
        {
            GenerateTunnelColumn(x);
        }
    }
    
    public override void UpdateLoop()
    {
        _backgroundOffset.x += _backgroundSpeed * Time.deltaTime*0.5f;
        _gameBackground.material.mainTextureOffset = _backgroundOffset;
        
        _tunnelOffset.x += _backgroundSpeed * Time.deltaTime;
        _tileMapContainer.position = -_tunnelOffset*_tileWidth;
        
        _container.transform.position = -_tunnelOffset*_tileWidth;
        
        if (_tunnelOffset.x - _centerTileX >= 1)
        {
            _centerTileX = Mathf.FloorToInt(_tunnelOffset.x);
            UpdateTunnel();
        }

        if (_container.transform.position.x < _nextWordSpawnX)
        {
            _nextWordSpawnX = _container.transform.position.x - _wordSpawnDistance -
                              _wordSpawnDistance * Random.Range(-_wordSpawnDistanceRange, _wordSpawnDistanceRange);
            SpawnWord();
        }
    }

    private void UpdateTunnel()
    {
        _tunnelTileMap.CompressBounds();
        _tunnelTileMap.ResizeBounds();
        ClearTunnelColumn(_centerTileX - _tunnelSize.x - 1);
        GenerateTunnelColumn(_centerTileX + _tunnelSize.x);
    }

    private Vector2 GetTunnelCenter()
    {
        return _tunnelTileMap.CellToWorld(GetColumnCenter(_centerTileX));
    }

    private Vector3Int GetColumnCenter(int x)
    {
        Vector3Int tile = new Vector3Int(x, 0, 0);
        
        for (int y = -_tunnelSize.y; y <= _tunnelSize.y; y++)
        {
            tile.y = y;
            if (!_tunnelTileMap.HasTile(tile))
            {
                tile.y += _tunnelWidth;
                break;
            }
        }

        return tile;
    }
    
    private Vector2 GetFreePositionInColumn(int x)
    {
        Vector3Int tileBot = new Vector3Int(x, 0, 0);
        
        for (int y = -_tunnelSize.y; y <= _tunnelSize.y; y++)
        {
            tileBot.y = y;
            if (!_tunnelTileMap.HasTile(tileBot))
            {
                tileBot.y++;
                break;
            }
        }

        Vector3Int tileTop = tileBot + new Vector3Int(0, _tunnelWidth * 2 -1, 0);
        
        Vector2 position = new Vector2(_tunnelTileMap.CellToWorld(tileBot).x, Random.Range(_tunnelTileMap.CellToWorld(tileBot).y + 50, _tunnelTileMap.CellToWorld(tileTop).y - 50));

        return position;
    }

    private void OnPackmanCrash()
    {
        step.StartCoroutine(OnPackmanCrashCoroutine());
        
        OnPackmanDragEnd(null);
    }

    private IEnumerator OnPackmanCrashCoroutine()
    {
        Pause();
        
        Crash();
        
        DropStar();
        
        yield return new WaitForSeconds(1);

        Play();
        
        _packMan.transform.position = GetTunnelCenter();
        _packMan.Spawn();
        
        _successSpree = 0;
        SpeedDown();
        
        yield return new WaitForSeconds(1);
    }

    private void Crash()
    {
        Vector2 pos = _packMan.transform.position;

        ParticleSystem effect = Instantiate(_deathEffect, transform);
        effect.transform.position = pos;
        }

    private void OnPackmanEatEnd(TunnelWord word)
    {

        if (word.eatable)
        {
            _words.Remove(word);
            AudioManager.Instance.PlayOneShot(SoundType.Voice, _eatableSound);
            _successSpree++;
            if (_successSpree >= 3)
            {
                SpeedUp();
                _successSpree = 0;
            }
            AddStar();
            _score++;
            if (!_wordsEaten.Contains(word.word))
                _wordsEaten.Add(word.word);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(SoundType.Voice, _notEatableSound);
            _nErrors++;
            _successSpree = 0;
            SpeedDown();
            DropStar();
        }
    }

    private void SpeedUp()
    {
        _packmanSpeed = _packmanSpeed*1.1f;
        _backgoundMaxSpeed = _packmanSpeed / _tileWidth;
    }

    private void SpeedDown()
    {
        _packmanSpeed = _packmanSpeed/1.1f;
        _backgoundMaxSpeed = _packmanSpeed / _tileWidth;
    }

    private void AddStar()
    {
        TweenableObject star = Instantiate(_starPrefab, _starsContainer);
        star.transform.position = _packMan.transform.position;
        star.Spawn();
        _stars.Add(star);
        
        int n = _stars.Count - 1;
        
        star.MoveTo(_starsGray[n].transform.position, 1);

        if (_stars.Count == _starsGray.Length)
        {
            GameComplete();
        }
    }

    private void DropStar()
    {
        if (_stars.Count == 0)
            return;
        
        _stars[_stars.Count - 1].Drop(20, _cameraController.Rect.yMin - 100, 2);
        _stars.RemoveAt(_stars.Count - 1);
    }

    private void SpawnWord()
    {
        TunnelWord word = Instantiate(_wordPrefab, _container);
        word.transform.position = GetFreePositionInColumn(_centerTileX + _tunnelSize.x);

        bool eatable = Random.value < _eatableChance;
        
        if (_eatableWordsSpawned >= _maxEatableInARaw)
        {
            eatable = false;
        }else if (_notEatableWordsSpawned >= _maxNotEatableInARaw)
        {
            eatable = true;
        }
        

        WordData_Thin wd;

        if (eatable)
        {
            do
            {
                wd = _wordsEatable[Random.Range(0, _wordsEatable.Count)];
            } while (wd.GetCleanWord() == _prevEatableWord);

            _prevEatableWord = wd.GetCleanWord();
            _eatableWordsSpawned++;
            _notEatableWordsSpawned = 0;
        }
        else
        {
            do
            {
                wd = _wordsNotEatable[Random.Range(0, _wordsNotEatable.Count)];
            } while (wd.GetCleanWord() == _prevWord);

            _prevWord = wd.GetCleanWord();
            _eatableWordsSpawned = 0;
            _notEatableWordsSpawned++;
        }

        WordData_Fat wdf = wd.GetFatData();
        
        word.Init(wd.word, wdf.GetRandomSprite(), wdf.sound, eatable, CommonAssets.Instance.ceraRoundMediumFont, _mgParameters.letters.capitalized, _colorizedSklads);
        
        _words.Add(word);
        word.onWordAte += OnPackmanEatEnd;
        
        RemoveOldWords();
    }

    private void RemoveOldWords()
    {
        for (int i = _words.Count - 1; i >=0 ; i--)
        {
            if (_words[i].transform.position.x < _cameraController.Rect.xMin - 200)
            {
                Destroy(_words[i].gameObject);
                _words.RemoveAt(i);
            }
        }
    }

    public override void GameComplete()
    {
        base.GameComplete();
        
        _packMan.Remove();
        OnPackmanDragEnd(null);
        AudioManager.Instance.Play(SoundType.Voice, _endGameSound);

        SaveManager.Session.SetParameter("tunnelFirstCompletionTime", Time.time);
        
        FinishGame(false, 5);
    }

    protected override void TrackGamePlayed(GameCompleteReason reason)
    {
        AppManager.EventTracker.track_roadGame_tunnelWithComplexity(_wordComplexity, (int)reason, (long)GetTimeSpend());
    }

    protected override void ReturnToPrevScene()
    {
        if (gameComplete)
        {
            _navigationController.OpenScene(leftRightSceneFolder, rightSceneName, 1,
            useScreenshotSlideTransitions ? SceneTransitionType.SlideToRightScreenshot : SceneTransitionType.SlideToRight, Vector3.zero, .5f, false,asyncLoad: true);
        }else
            _navigationController.OpenScene(leftRightSceneFolder, leftSceneName, 1,
                useScreenshotSlideTransitions ? SceneTransitionType.SlideToLeftScreenshot : SceneTransitionType.SlideToLeft, Vector3.zero, .5f, false,asyncLoad: true);
    }

    protected override bool CheckWasGameSuccessful()
    {
        return _score >= _nErrors * 2;
    }

    protected override float CalculateAndSendExperience()
    {
        //float totalExp = 0;

        float pointsToAdd = 1.0f;

        if (_score < _nErrors)
        {
            pointsToAdd = 0.1f;
        }
        else if (_score < _nErrors * 2)
        {
            pointsToAdd = 0.3f;
        }

        //float totalExpBefore = Experience.GetExperience.Sum.total;

        for (int i = 0; i < _wordsEaten.Count; i++)
        {
            string word = _wordsEaten[i];
            word = word.Replace("^", string.Empty);
            Experience.GetExperience.AddReadExperienceForChtenieSlovo(word, pointsToAdd);
            Experience.GetExperience.AddExperienceForUnhintedChtenie(word, (25 + _mgParameters.complexity)/25.0f*pointsToAdd);
            //totalExp += (25 + _mgParameters.complexity) / 25;
        }

        //return Experience.GetExperience.Sum.total - totalExpBefore;
        return 0.0f;    //не используется
    }
}