using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// High-performance virtual tweens without Godot object overhead
/// </summary>
public partial class VirtualTween
{
    private struct VirtualTweenData
    {
        public float From;
        public float To;
        public float Duration;
        public float Elapsed;
        public Action<float> OnUpdate;
        public Action OnComplete;
        public bool IsAlive;
        public GTween.EaseDirection Ease;
        public GTween.TransitionType Transition;
    }

    private VirtualTweenData[] _activeTweens;
    private int _activeCount;
    private Queue<int> _freeIndices;
    private const int INITIAL_CAPACITY = 64;

    public VirtualTween()
    {
        _activeTweens = new VirtualTweenData[INITIAL_CAPACITY];
        _freeIndices = new Queue<int>();
        
        // Pre-fill with free indices
        for (int i = 0; i < INITIAL_CAPACITY; i++)
            _freeIndices.Enqueue(i);
    }

    /// <summary>
    /// Ultra-fast float tween - no Godot objects, no Variants
    /// </summary>
    public int Float(float from, float to, float duration, Action<float> onUpdate, 
                    Action onComplete = null,
                    GTween.EaseDirection ease = GTween.EaseDirection.In,
                    GTween.TransitionType transition = GTween.TransitionType.Linear)
    {
        if (_freeIndices.Count == 0)
            ExpandCapacity();

        int index = _freeIndices.Dequeue();
        
        _activeTweens[index] = new VirtualTweenData
        {
            From = from,
            To = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdate = onUpdate,
            OnComplete = onComplete,
            IsAlive = true,
            Ease = ease,
            Transition = transition
        };

        _activeCount++;
        
        // Immediate first update
        onUpdate?.Invoke(from);
        
        return index;
    }

    /// <summary>
    /// Must be called manually each frame (or integrate with your update system)
    /// </summary>
    public void Update(float delta)
    {
        for (int i = 0; i < _activeTweens.Length; i++)
        {
            ref var tween = ref _activeTweens[i];
            
            if (!tween.IsAlive) continue;
            
            tween.Elapsed += delta;
            float progress = Mathf.Clamp(tween.Elapsed / tween.Duration, 0f, 1f);
            
            // Apply easing directly
            float easedProgress = ApplyEasingDirect(progress, tween.Transition, tween.Ease);
            float currentValue = Mathf.Lerp(tween.From, tween.To, easedProgress);
            
            tween.OnUpdate?.Invoke(currentValue);
            
            if (progress >= 1f)
            {
                // Ensure exact final value
                tween.OnUpdate?.Invoke(tween.To);
                tween.OnComplete?.Invoke();
                
                tween.IsAlive = false;
                _freeIndices.Enqueue(i);
                _activeCount--;
            }
        }
    }

    /// <summary>
    /// Direct easing application without Variant overhead
    /// </summary>
    private float ApplyEasingDirect(float t, GTween.TransitionType trans, GTween.EaseDirection ease)
    {
        // Use your existing TweenManager easing logic here
        // This would call the same easing functions but without object overhead
        return GTween.Instance.tweenManager.ApplyEasingFast(t, trans, ease);
    }

    private void ExpandCapacity()
    {
        int newCapacity = _activeTweens.Length * 2;
        var newArray = new VirtualTweenData[newCapacity];
        _activeTweens.CopyTo(newArray, 0);
        _activeTweens = newArray;
        
        // Add new indices to free list
        for (int i = _activeTweens.Length / 2; i < newCapacity; i++)
            _freeIndices.Enqueue(i);
    }

    public void Kill(int tweenId)
    {
        if (tweenId >= 0 && tweenId < _activeTweens.Length && _activeTweens[tweenId].IsAlive)
        {
            _activeTweens[tweenId].IsAlive = false;
            _freeIndices.Enqueue(tweenId);
            _activeCount--;
        }
    }

    public int GetActiveCount() => _activeCount;
    public int GetCapacity() => _activeTweens.Length;
}

