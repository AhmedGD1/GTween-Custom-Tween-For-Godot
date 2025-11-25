using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class TweenManager
{
    private const int INITIAL_CAPACITY = 32;
    private const int POOL_SIZE = 20;

    public TweenData[] activeTweens;
    public int activeTweenCount = 0;
    
    private TweenData[] toRemove;
    private int toRemoveCount = 0;
    
    private Queue<TweenData> tweenPool = new();

    private struct PropertyUpdate
    {
        public GodotObject Target;
        public StringName Property;
        public Variant Value;
    }
    
    private PropertyUpdate[] pendingUpdates;
    private int pendingUpdateCount = 0;

    private float[] segmentProgressCache;
    private Variant[] interpolatedValuesCache;

    public TweenManager()
    {
        activeTweens = new TweenData[INITIAL_CAPACITY];
        toRemove = new TweenData[16];
        pendingUpdates = new PropertyUpdate[INITIAL_CAPACITY];
        segmentProgressCache = new float[INITIAL_CAPACITY];
        interpolatedValuesCache = new Variant[INITIAL_CAPACITY];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            tweenPool.Enqueue(new TweenData());
        }
    }

    public TweenController RegisterTween(TweenData tween)
    {
        if (activeTweenCount >= activeTweens.Length)
        {
            Array.Resize(ref activeTweens, activeTweens.Length * 2);
            Array.Resize(ref pendingUpdates, pendingUpdates.Length * 2);
            Array.Resize(ref segmentProgressCache, segmentProgressCache.Length * 2);
            Array.Resize(ref interpolatedValuesCache, interpolatedValuesCache.Length * 2);
        }

        activeTweens[activeTweenCount++] = tween;
        
        if (GTween.Instance != null && tween.Target != null && !string.IsNullOrEmpty(tween.Property))
        {
            var key = (tween.Target, tween.Property);
            if (!GTween.propertyTweenRegistry.ContainsKey(key))
            {
                GTween.propertyTweenRegistry[key] = new List<TweenData>();
            }
            GTween.propertyTweenRegistry[key].Add(tween);
        }
        
        return new TweenController(tween, this);
    }

    /// <summary>
    /// Get all active tweens on a specific property
    /// </summary>
    public List<TweenData> GetPropertyTweens(GodotObject target, string property)
    {
        var result = new List<TweenData>();
        
        for (int i = 0; i < activeTweenCount; i++)
        {
            var tween = activeTweens[i];
            if (tween != null && tween.Target == target && tween.Property == property)
            {
                result.Add(tween);
            }
        }
        
        return result;
    }

    public void KillTween(TweenData tween)
    {
        tween.OnKillCallback?.Invoke();
        
        if (toRemoveCount >= toRemove.Length)
            Array.Resize(ref toRemove, toRemove.Length * 2);
            
        toRemove[toRemoveCount++] = tween;
    }

    public void KillAll()
    {
        for (int i = 0; i < activeTweenCount; i++)
        {
            activeTweens[i].OnKillCallback?.Invoke();
        }
        activeTweenCount = 0;
        toRemoveCount = 0;
    }

    public int KillTweensOf(GodotObject target)
    {
        int killed = 0;
        for (int i = activeTweenCount - 1; i >= 0; i--)
        {
            if (activeTweens[i].Target == target)
            {
                activeTweens[i].OnKillCallback?.Invoke();
                
                if (toRemoveCount >= toRemove.Length)
                    Array.Resize(ref toRemove, toRemove.Length * 2);
                    
                toRemove[toRemoveCount++] = activeTweens[i];
                killed++;
            }
        }
        return killed;
    }

    public void TogglePause(bool toggle)
    {
        for (int i = 0; i < activeTweenCount; i++)
        {
            activeTweens[i].IsPaused = toggle;
        }
    }

    // OPTIMIZED UPDATE LOOP - Main performance hotspot
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(double delta)
    {
        toRemoveCount = 0;
        pendingUpdateCount = 0;
        float deltaF = (float)delta;

        for (int i = 0; i < activeTweenCount; i++)
        {
            var tween = activeTweens[i];

            if (!GodotObject.IsInstanceValid(tween.Target))
            {
                if (toRemoveCount >= toRemove.Length)
                    Array.Resize(ref toRemove, toRemove.Length * 2);
                toRemove[toRemoveCount++] = tween;
                continue;
            }

            if (tween.IsPaused)
                continue;

            if (tween.Delay > 0)
            {
                tween.Delay -= deltaF;
                continue;
            }

            if (!tween.HasStarted)
            {
                tween.OnStartCallback?.Invoke();
                tween.HasStarted = true;
            }

            var seg = tween.Segments[tween.CurrentSegmentIndex];
            tween.SegmentElapsed += deltaF * tween.SpeedScale;

            float progress = tween.SegmentElapsed / seg.Duration;
            if (progress > 1f) progress = 1f;
            
            segmentProgressCache[i] = progress;

            float easedProgress = ApplyEasingFast(progress, seg.TransitionType, seg.Ease);
            Variant currentValue = InterpolateFast(seg.Start, seg.End, easedProgress);

            if (tween.SnapToInt)
            {
                currentValue = SnapValueFast(currentValue);
            }

            interpolatedValuesCache[i] = currentValue;

            if (pendingUpdateCount >= pendingUpdates.Length)
                Array.Resize(ref pendingUpdates, pendingUpdates.Length * 2);

            pendingUpdates[pendingUpdateCount++] = new PropertyUpdate
            {
                Target = tween.Target,
                Property = tween.PropertyName,
                Value = currentValue
            };

            tween.OnUpdateCallback?.Invoke();

            if (progress >= 1f)
            {
                tween.CurrentSegmentIndex++;

                if (tween.CurrentSegmentIndex >= tween.Segments.Count)
                {
                    if (tween.CanLoop())
                    {
                        ApplyTweenLoop(tween);
                        continue;
                    }

                    tween.Callback?.Invoke();
                    
                    if (toRemoveCount >= toRemove.Length)
                        Array.Resize(ref toRemove, toRemove.Length * 2);
                    toRemove[toRemoveCount++] = tween;
                }
                else
                {
                    tween.SegmentElapsed = 0f;
                }
            }
        }

        ApplyBatchedUpdates();
        CleanupRemovedTweens();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyBatchedUpdates()
    {
        for (int i = 0; i < pendingUpdateCount; i++)
        {
            var update = pendingUpdates[i];
            update.Target.Set(update.Property, update.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CleanupRemovedTweens()
    {
        for (int i = 0; i < toRemoveCount; i++)
        {
            var tweenToRemove = toRemove[i];
            
            // FIX: Remove from property registry
            if (GTween.Instance != null && tweenToRemove.Target != null && !string.IsNullOrEmpty(tweenToRemove.Property))
            {
                var key = (tweenToRemove.Target, tweenToRemove.Property);
                if (GTween.propertyTweenRegistry.TryGetValue(key, out var list))
                {
                    list.Remove(tweenToRemove);
                    if (list.Count == 0)
                    {
                        GTween.propertyTweenRegistry.Remove(key);
                    }
                }
            }
            
            // Remove from active tweens array
            for (int j = 0; j < activeTweenCount; j++)
            {
                if (activeTweens[j] == tweenToRemove)
                {
                    activeTweens[j] = activeTweens[activeTweenCount - 1];
                    activeTweens[activeTweenCount - 1] = null;
                    activeTweenCount--;
                    
                    ReturnToPool(tweenToRemove);
                    break;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ApplyEasingFast(float t, GTween.TransitionType trans, GTween.EaseDirection ease)
    {
        if (trans == GTween.TransitionType.Linear)
            return t;

        if (trans == GTween.TransitionType.Sine)
        {
            switch (ease)
            {
                case GTween.EaseDirection.In:
                    return 1f - Mathf.Cos((t * Mathf.Pi) / 2f);
                case GTween.EaseDirection.Out:
                    return Mathf.Sin((t * Mathf.Pi) / 2f);
                case GTween.EaseDirection.InOut:
                    return -(Mathf.Cos(Mathf.Pi * t) - 1f) / 2f;
            }
        }

        return ApplyEasing(t, trans, ease);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Variant InterpolateFast(Variant start, Variant end, float t)
    {
        var type = start.VariantType;
        
        if (type == Variant.Type.Float)
        {
            float s = start.AsSingle();
            float e = end.AsSingle();
            return s + (e - s) * t;
        }
        
        if (type == Variant.Type.Vector2)
        {
            return start.AsVector2().Lerp(end.AsVector2(), t);
        }
        
        if (type == Variant.Type.Vector3)
        {
            return start.AsVector3().Lerp(end.AsVector3(), t);
        }

        // Fallback
        return Interpolate(start, end, t);
    }

    private Variant Interpolate(Variant start, Variant end, float t)
    {
        switch (start.VariantType)
        {
            case Variant.Type.Float:
                return Mathf.Lerp(start.AsSingle(), end.AsSingle(), t);
            case Variant.Type.Vector2:
                return start.AsVector2().Lerp(end.AsVector2(), t);
            case Variant.Type.Vector3:
                return start.AsVector3().Lerp(end.AsVector3(), t);
            case Variant.Type.Color:
                return start.AsColor().Lerp(end.AsColor(), t);
            default:
                return end;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Variant SnapValueFast(Variant value)
    {
        var type = value.VariantType;
        
        if (type == Variant.Type.Float)
            return Mathf.Round(value.AsSingle());
        
        if (type == Variant.Type.Vector2)
        {
            var v2 = value.AsVector2();
            return new Vector2(Mathf.Round(v2.X), Mathf.Round(v2.Y));
        }
        
        if (type == Variant.Type.Vector3)
        {
            var v3 = value.AsVector3();
            return new Vector3(Mathf.Round(v3.X), Mathf.Round(v3.Y), Mathf.Round(v3.Z));
        }
        
        return value;
    }

    // Full easing implementation (unchanged, but called less often now)
    private float ApplyEasing(float t, GTween.TransitionType trans, GTween.EaseDirection ease)
    {
        switch (trans)
        {
            case GTween.TransitionType.Quad:
                return Ease(t, ease,
                    x => x * x,
                    x => 1f - (1f - x) * (1f - x),
                    x => x < 0.5f ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2
                );

            case GTween.TransitionType.Cubic:
                return Ease(t, ease,
                    x => x * x * x,
                    x => 1f - Mathf.Pow(1f - x, 3),
                    x => x < 0.5f ? 4f * x * x * x : 
                                    1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f
                );

            case GTween.TransitionType.Quart:
                return Ease(t, ease,
                    x => x * x * x * x,
                    x => 1f - Mathf.Pow(1f - x, 4f),
                    x => x < 0.5f ? 8f * x * x * x * x :
                                    1 - Mathf.Pow(-2f * x + 2f, 4f) / 2f
                );

            case GTween.TransitionType.Quint:
                return Ease(t, ease,
                    x => x * x * x * x * x,
                    x => 1 - Mathf.Pow(1 - x, 5),
                    x => x < 0.5f ? 16 * x * x * x * x * x :
                                    1 - Mathf.Pow(-2 * x + 2, 5) / 2
                );

            case GTween.TransitionType.Expo:
                return Ease(t, ease,
                    x => x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10),
                    x => x == 1 ? 1 : 1 - Mathf.Pow(2, -10 * x),
                    x => x == 0 ? 0 : x == 1 ? 1 :
                        x < 0.5f ? Mathf.Pow(2, 20 * x - 10) / 2 :
                        (2 - Mathf.Pow(2, -20 * x + 10)) / 2
                );

            case GTween.TransitionType.Circ:
                return Ease(t, ease,
                    x => 1 - Mathf.Sqrt(1 - x * x),
                    x => Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2)),
                    x => x < 0.5f ? 
                        (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2 :
                        (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2
                );

            case GTween.TransitionType.Back:
                const float c1 = 1.70158f;
                const float c2 = c1 * 1.525f;
                return Ease(t, ease,
                    x => (c1 + 1) * x * x * x - c1 * x * x,
                    x => 1 + (c1 + 1) * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2),
                    x => x < 0.5f ? 
                        (Mathf.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2 :
                        (Mathf.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2
                );

            case GTween.TransitionType.Bounce:
                return BounceEase(t, ease);

            case GTween.TransitionType.Elastic:
                return ElasticEase(t, ease);
                
            default:
                return t;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Ease(float t, GTween.EaseDirection ease,
        Func<float, float> easeIn,
        Func<float, float> easeOut,
        Func<float, float> easeInOut)
    {
        return ease switch
        {
            GTween.EaseDirection.In => easeIn(t),
            GTween.EaseDirection.Out => easeOut(t),
            GTween.EaseDirection.InOut => easeInOut(t),
            GTween.EaseDirection.OutIn => t < 0.5f ? easeOut(t * 2) / 2 : easeIn((t - 0.5f) * 2) / 2 + 0.5f,
            _ => t
        };
    }

    private float BounceEase(float t, GTween.EaseDirection ease)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        float bounceOut(float x)
        {
            if (x < 1 / d1) return n1 * x * x;
            if (x < 2 / d1) return n1 * (x -= 1.5f / d1) * x + 0.75f;
            if (x < 2.5 / d1) return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }

        return ease switch
        {
            GTween.EaseDirection.In => 1f - bounceOut(1f - t),
            GTween.EaseDirection.Out => bounceOut(t),
            GTween.EaseDirection.InOut => t < 0.5f 
                ? (1 - bounceOut(1 - 2 * t)) / 2 
                : (1 + bounceOut(2 * t - 1)) / 2,
            GTween.EaseDirection.OutIn => t < 0.5f 
                ? bounceOut(t * 2) / 2 
                : (1 - bounceOut((1 - t) * 2)) / 2 + 0.5f,
            _ => t
        };
    }

    private float ElasticEase(float t, GTween.EaseDirection ease)
    {
        const float c4 = (2 * Mathf.Pi) / 3;
        const float c5 = (2 * Mathf.Pi) / 4.5f;

        float easeIn(float x) =>
            x == 0 ? 0 :
            x == 1 ? 1 :
            -Mathf.Pow(2, 10 * x - 10) * Mathf.Sin((x * 10 - 10.75f) * c4);

        float easeOut(float x) =>
            x == 0 ? 0 :
            x == 1 ? 1 :
            Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;

        float easeInOut(float x) =>
            x == 0 ? 0 :
            x == 1 ? 1 :
            x < 0.5f ?
                -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2 :
                Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * c5) / 2 + 1;

        return ease switch
        {
            GTween.EaseDirection.In => easeIn(t),
            GTween.EaseDirection.Out => easeOut(t),
            GTween.EaseDirection.InOut => easeInOut(t),
            GTween.EaseDirection.OutIn => t < 0.5f 
                ? easeOut(t * 2) / 2 
                : easeIn((t - 0.5f) * 2) / 2 + 0.5f,
            _ => t
        };
    }

    private void ApplyTweenLoop(TweenData tween)
    {
        tween.Loop();
        tween.CurrentSegmentIndex = 0;
        tween.SegmentElapsed = 0f;

        if (tween.LoopMode == GTween.LoopMode.PingPong && !tween.IsReversed)
        {
            tween.Segments.Reverse();
            for (int s = 0; s < tween.Segments.Count; s++)
            {
                var segment = tween.Segments[s];
                (segment.Start, segment.End) = (segment.End, segment.Start);
                tween.Segments[s] = segment;
            }
            tween.IsReversed = true;
        }
    }

    private void ReturnToPool(TweenData tween)
    {
        if (tweenPool.Count < POOL_SIZE * 2)
        {
            tween.Reset();
            tweenPool.Enqueue(tween);
        }
    }

    public int GetActiveTweenCount() => activeTweenCount;
    public int GetPoolSize() => tweenPool.Count;
    public int GetCapacity() => activeTweens.Length;
}

