using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TunnelPackMan : MonoBehaviour
{
    [SerializeField] private DragAndDropObject _dragAndDrop;
    [SerializeField] private TweenableObject _tweenable;
    private InteractiveObject _interactive;

    private Collider2D _collider2D;

    public DragAndDropObject.OnDragDelegate onDragBegin;
    public DragAndDropObject.OnDragDelegate onDragEnd;

    public delegate void PackmanDelegate();
    public delegate void PackmanWordDelegate(TunnelWord word);

    public PackmanDelegate onCrash;
    public PackmanDelegate onWordEatBegin;
    public PackmanWordDelegate onWordEatEnd;

    private bool _active;
    
    public void Init()
    {
        _dragAndDrop = GetComponent<DragAndDropObject>();
        _interactive = GetComponent<InteractiveObject>();
        
        _dragAndDrop.Init(255 - (byte)MiniGameBase.GameState.PAUSED);

        _dragAndDrop.onDrag += OnDrag;
        _dragAndDrop.onDragEnd += OnDragEnd;
        _dragAndDrop.onDragBegin += OnDragBegin;

        _collider2D = GetComponent<Collider2D>();
    }

    private void OnDragBegin(InteractiveObject io)
    {
        onDragBegin?.Invoke(io);
    }
    
    private void OnDragEnd(InteractiveObject io)
    {
        onDragEnd?.Invoke(io);
    }

    private void OnDrag(InteractiveObject io)
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active)
            return;

        TunnelWord word = other.GetComponent<TunnelWord>();

        if (word != null)
        {
            EatWord(word);
        }
        else
        {
            Crash();
        }
    }

    private void Crash()
    {
        _tweenable.SetHidden(true, 0.2f);
        onCrash?.Invoke();
        _active = false;
        
        _dragAndDrop.Release();
    }

    private void EatWord(TunnelWord word)
    {
        bool eatable = word.Eat();
        word.transform.SetParent(transform);
        onWordEatEnd?.Invoke(word);
    }

    public void SetSize(float size)
    {
        float scale = size / GetComponent<SpriteRenderer>().size.x;
        _tweenable.SetRootScale(scale);
    }

    public void Spawn(float duration = 1)
    {
        _tweenable.Spawn(duration);
        _active = true;
    }

    public void Remove(float dur = 0.5f)
    {
        if(dur > 0 )
            _tweenable.SetHidden(true, dur);
        else
            _tweenable.SetHidden();
    }
}
