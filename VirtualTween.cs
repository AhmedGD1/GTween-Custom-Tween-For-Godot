using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// High-performance virtual tweens without Godot object overhead
/// </summary>
public partial class VirtualTween
{
    private enum ValueType
    {
        Float,
        Int,
        Vector2,
        Vector3,
        Color
    }

    private struct VirtualTweenData
    {
        public ValueType Type;
        public bool IsAlive;
        
        public float FromFloat;
        public float ToFloat;
        
        public int FromInt;
        public int ToInt;
        
        public Vector2 FromVector2;
        public Vector2 ToVector2;
        
        public Vector3 FromVector3;
        public Vector3 ToVector3;
        
        public Color FromColor;
        public Color ToColor;
        
        public float Duration;
        public float Elapsed;
        public Action<float> OnUpdateFloat;
        public Action<int> OnUpdateInt;
        public Action<Vector2> OnUpdateVector2;
        public Action<Vector3> OnUpdateVector3;
        public Action<Color> OnUpdateColor;
        public Action OnComplete;
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
            Type = ValueType.Float,
            FromFloat = from,
            ToFloat = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdateFloat = onUpdate,
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
    /// Ultra-fast int tween - interpolates as float then rounds to int
    /// </summary>
    public int Int(int from, int to, float duration, Action<int> onUpdate,
                  Action onComplete = null,
                  GTween.EaseDirection ease = GTween.EaseDirection.In,
                  GTween.TransitionType transition = GTween.TransitionType.Linear)
    {
        if (_freeIndices.Count == 0)
            ExpandCapacity();

        int index = _freeIndices.Dequeue();
        
        _activeTweens[index] = new VirtualTweenData
        {
            Type = ValueType.Int,
            FromInt = from,
            ToInt = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdateInt = onUpdate,
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
    /// Ultra-fast Vector2 tween - no Godot objects, no Variants
    /// </summary>
    public int Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> onUpdate,
                      Action onComplete = null,
                      GTween.EaseDirection ease = GTween.EaseDirection.In,
                      GTween.TransitionType transition = GTween.TransitionType.Linear)
    {
        if (_freeIndices.Count == 0)
            ExpandCapacity();

        int index = _freeIndices.Dequeue();
        
        _activeTweens[index] = new VirtualTweenData
        {
            Type = ValueType.Vector2,
            FromVector2 = from,
            ToVector2 = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdateVector2 = onUpdate,
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
    /// Ultra-fast Vector3 tween - no Godot objects, no Variants
    /// </summary>
    public int Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
                      Action onComplete = null,
                      GTween.EaseDirection ease = GTween.EaseDirection.In,
                      GTween.TransitionType transition = GTween.TransitionType.Linear)
    {
        if (_freeIndices.Count == 0)
            ExpandCapacity();

        int index = _freeIndices.Dequeue();
        
        _activeTweens[index] = new VirtualTweenData
        {
            Type = ValueType.Vector3,
            FromVector3 = from,
            ToVector3 = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdateVector3 = onUpdate,
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
    /// Ultra-fast Color tween - no Godot objects, no Variants
    /// </summary>
    public int Color(Color from, Color to, float duration, Action<Color> onUpdate,
                   Action onComplete = null,
                   GTween.EaseDirection ease = GTween.EaseDirection.In,
                   GTween.TransitionType transition = GTween.TransitionType.Linear)
    {
        if (_freeIndices.Count == 0)
            ExpandCapacity();

        int index = _freeIndices.Dequeue();
        
        _activeTweens[index] = new VirtualTweenData
        {
            Type = ValueType.Color,
            FromColor = from,
            ToColor = to,
            Duration = duration,
            Elapsed = 0f,
            OnUpdateColor = onUpdate,
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
            
            // Update based on value type
            switch (tween.Type)
            {
                case ValueType.Float:
                    float currentFloat = Mathf.Lerp(tween.FromFloat, tween.ToFloat, easedProgress);
                    tween.OnUpdateFloat?.Invoke(currentFloat);
                    break;
                    
                case ValueType.Int:
                    // Interpolate as float then round to int for smooth animation
                    float intProgress = Mathf.Lerp(tween.FromInt, tween.ToInt, easedProgress);
                    int currentInt = (int)Mathf.Round(intProgress);
                    tween.OnUpdateInt?.Invoke(currentInt);
                    break;
                    
                case ValueType.Vector2:
                    Vector2 currentVector2 = tween.FromVector2.Lerp(tween.ToVector2, easedProgress);
                    tween.OnUpdateVector2?.Invoke(currentVector2);
                    break;
                    
                case ValueType.Vector3:
                    Vector3 currentVector3 = tween.FromVector3.Lerp(tween.ToVector3, easedProgress);
                    tween.OnUpdateVector3?.Invoke(currentVector3);
                    break;
                    
                case ValueType.Color:
                    Color currentColor = tween.FromColor.Lerp(tween.ToColor, easedProgress);
                    tween.OnUpdateColor?.Invoke(currentColor);
                    break;
            }
            
            if (progress >= 1f)
            {
                // Ensure exact final value
                switch (tween.Type)
                {
                    case ValueType.Float:
                        tween.OnUpdateFloat?.Invoke(tween.ToFloat);
                        break;
                    case ValueType.Int:
                        tween.OnUpdateInt?.Invoke(tween.ToInt);
                        break;
                    case ValueType.Vector2:
                        tween.OnUpdateVector2?.Invoke(tween.ToVector2);
                        break;
                    case ValueType.Vector3:
                        tween.OnUpdateVector3?.Invoke(tween.ToVector3);
                        break;
                    case ValueType.Color:
                        tween.OnUpdateColor?.Invoke(tween.ToColor);
                        break;
                }
                
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
        return GTween.Instance?.tweenManager.ApplyEasingFast(t, trans, ease) ?? t;
    }

    /// <summary>
    /// Wait for a specified duration (coroutine-style)
    /// </summary>
    public async System.Threading.Tasks.Task Wait(float duration)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        
        Float(0f, 1f, duration, 
            onUpdate: null,
            onComplete: () => tcs.SetResult(true),
            GTween.EaseDirection.In
        );
        
        await tcs.Task;
    }

    private void ExpandCapacity()
    {
        int newCapacity = _activeTweens.Length * 2;
        var newArray = new VirtualTweenData[newCapacity];
        _activeTweens.CopyTo(newArray, 0);
        _activeTweens = newArray;
       
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

    public void KillAll()
    {
        for (int i = 0; i < _activeTweens.Length; i++)
        {
            if (_activeTweens[i].IsAlive)
            {
                _activeTweens[i].IsAlive = false;
                _freeIndices.Enqueue(i);
            }
        }
        _activeCount = 0;
    }

    /// <summary>
    /// Get current value of a running tween
    /// </summary>
    public object GetCurrentValue(int tweenId)
    {
        if (tweenId < 0 || tweenId >= _activeTweens.Length || !_activeTweens[tweenId].IsAlive)
            return null;

        ref var tween = ref _activeTweens[tweenId];
        float progress = Mathf.Clamp(tween.Elapsed / tween.Duration, 0f, 1f);
        float easedProgress = ApplyEasingDirect(progress, tween.Transition, tween.Ease);

        return tween.Type switch
        {
            ValueType.Float => Mathf.Lerp(tween.FromFloat, tween.ToFloat, easedProgress),
            ValueType.Int => (int)Mathf.Round(Mathf.Lerp(tween.FromInt, tween.ToInt, easedProgress)),
            ValueType.Vector2 => tween.FromVector2.Lerp(tween.ToVector2, easedProgress),
            ValueType.Vector3 => tween.FromVector3.Lerp(tween.ToVector3, easedProgress),
            ValueType.Color => tween.FromColor.Lerp(tween.ToColor, easedProgress),
            _ => null
        };
    }

    /// <summary>
    /// Check if a tween is currently active
    /// </summary>
    public bool IsActive(int tweenId)
    {
        return tweenId >= 0 && tweenId < _activeTweens.Length && _activeTweens[tweenId].IsAlive;
    }

    /// <summary>
    /// Get progress of a tween (0-1)
    /// </summary>
    public float GetProgress(int tweenId)
    {
        if (tweenId < 0 || tweenId >= _activeTweens.Length || !_activeTweens[tweenId].IsAlive)
            return 0f;

        ref var tween = ref _activeTweens[tweenId];
        return Mathf.Clamp(tween.Elapsed / tween.Duration, 0f, 1f);
    }

    public int GetActiveCount() => _activeCount;
    public int GetCapacity() => _activeTweens.Length;
}