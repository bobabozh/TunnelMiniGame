using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TunnelWord : MonoBehaviour
{
    [SerializeField] private DynamicWordObject _slovo;
    [SerializeField] private SpriteRenderer _image;
    [SerializeField] private SpriteRenderer _pointAndText;
    private TweenableObject _imageTweenable;
    private TweenableObject _pointAndTextTweenable;
    
    private CircleCollider2D _collider;
    private AudioClip _sound;
    private TweenableObject _tweenable;
    

    private bool _eatable;
    public bool eatable => _eatable;
    
    private Vector2 _maxImageSize = new Vector2(250, 250);
    private Transform _container;

    public delegate void TunnelWordDelegate(TunnelWord word);

    public TunnelWordDelegate onWordAte;

    private string _word;

    public string word => _word;

    public void Init(string word, Sprite sprite, AudioClip sound, bool eatable, TMP_FontAsset font, bool capitalize = true, bool colorized = false)
    {
        _word = word;
        _eatable = eatable;

        _sound = sound;
        
        _collider = GetComponent<CircleCollider2D>();
        _tweenable = GetComponent<TweenableObject>();

        _pointAndTextTweenable = _pointAndText.GetComponent<TweenableObject>();
        
        _imageTweenable = _image.GetComponent<TweenableObject>();
        _imageTweenable.SetHidden();

        _image.sprite = sprite;
        _imageTweenable.SetRootScale(Mathf.Min(_maxImageSize.x / sprite.bounds.size.x, _maxImageSize.y/sprite.bounds.size.y));

        _container = transform.parent;
        
        _slovo.Init(word);
        _slovo.SetFont(font);
        _slovo.SetCase(capitalize);
        _slovo.SetColorize(colorized, false, true);
        
        if(!capitalize)
            _slovo.transform.localScale = Vector3.one*1.5f;
    }

    public void SetRadius(float value)
    {
        Vector2 tmpPosition = _slovo.transform.localPosition;
        tmpPosition.x = value - 5;
        tmpPosition.y = -value + 5;
        _slovo.transform.localPosition = tmpPosition;

        float scale = value / _pointAndText.size.y;
        transform.localScale = Vector3.one*scale;
    }

    public bool Eat()
    {
        _collider.enabled = false;

        StartCoroutine(EatCoroutine());
        
        return _eatable;
    }

    private IEnumerator EatCoroutine()
    {
        float imagePadding = 25;
        
        _tweenable.MoveToLocal(Vector2.zero, 0.5f);
        _pointAndTextTweenable.SetHidden(true, 0.5f);
        
        _imageTweenable.Spawn(0.1f);
        _imageTweenable.MoveToLocal(new Vector2(_maxImageSize.x/2 + imagePadding, 0), 0.1f);
        
        yield return new WaitForSeconds(0.2f);
        
        AudioManager.Instance.Play(SoundType.Voice, _sound);
        
        yield return new WaitForSeconds(0.3f);
        
        _imageTweenable.SetHidden(true, 0.5f);
        _imageTweenable.MoveToLocal(Vector2.zero, 0.5f);
        
        yield return new WaitForSeconds(0.5f);

        if (_eatable)
        {
            onWordAte?.Invoke(this);
            _tweenable.FadeOutAndDestroy();
        }
        else
        {
            onWordAte?.Invoke(this);
            
            _tweenable.MoveToLocal(new Vector2(50, 0), 0.5f);
            
            _pointAndTextTweenable.SetHidden(false, 0.5f);
            
            _imageTweenable.SetHidden(false, 0.5f);
            _imageTweenable.MoveToLocal(new Vector2(_maxImageSize.x/2+ imagePadding + _slovo.width, 0), 0.5f);
            
            yield return new WaitForSeconds(0.6f);
            
            _imageTweenable.SetHidden(true ,0.5f);

            _slovo.Fade(0.4f, 0.2f);
            _pointAndText.DOFade(0.4f, 0.2f);
            
            transform.SetParent(_container);
        }
    }
}
