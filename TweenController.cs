using Godot;
using System;

public class TweenController
{
    internal TweenData data;
    internal TweenManager manager;
    internal bool isValid = true;

    public TweenController(TweenData data, TweenManager manager)
    {
        this.data = data;
        this.manager = manager;
    }

    public TweenController Pause()
    {
        if (isValid)
        {
            data.IsPaused = true;
            data.InvokeOnPauseToggle();
        }
        return this;
    }

    public TweenController Play()
    {
        if (isValid) 
        {
            data.IsPaused = false;
            
            // Ensure the tween is registered with the manager
            if (!manager.IsTweenActive(data))
            {
                manager.RegisterTween(data);
            }
        }
        return this;
    }

    public TweenController TogglePause(bool toggle)
    {
        if (isValid)
        {
            data.IsPaused = toggle;
            data.InvokeOnPauseToggle();
        }
        return this;
    }

    public TweenController Kill()
    {
        if (isValid)
        {
            manager.KillTween(data);
            isValid = false;
        }
        return this;
    }

    public TweenController Restart()
    {
        if (isValid)
        {
            data.SegmentElapsed = 0f;
            data.CurrentSegmentIndex = 0;
            data.ResetLoopCount();
        }
        return this;
    }

    public TweenController Rewind()
    {
        if (isValid)
        {
            data.SegmentElapsed = 0f;
            data.CurrentSegmentIndex = 0;
            
            if (data.Segments.Count > 0)
                data.Target.Set(data.Property, data.Segments[0].Start);
        }
        return this;
    }

    public TweenController OnStart(Action callback)
    {
        if (isValid)
            data.OnStartCallback = callback;
        return this;
    }

    public TweenController OnUpdate(Action callback)
    {
        if (isValid)
            data.OnUpdateCallback = callback;
        return this;
    }

    public TweenController OnComplete(Action callback)
    {
        if (isValid)
            data.Callback = callback;
        return this;
    }

    public TweenController OnPause(Action<bool> callback)
    {
        if (isValid)
            data.OnPauseToggleCallback = callback;
        return this;
    }

    public TweenController OnKill(Action callback)
    {
        if (isValid)
            data.OnKillCallback = callback;
        return this;
    }

    public TweenController SetId(object id)
    {
        if (isValid)
            data.Id = id;
        return this;
    }

    public TweenController SetDelay(float duration)
    {
        data.Delay = duration;
        return this;
    }

    public bool IsRunning() => isValid && !data.IsPaused;
    public bool IsPaused() => isValid && data.IsPaused;
    public bool IsComplete() => !isValid;
    public float GetProgress() => isValid ? data.GetTotalProgress() : 1f;
    public object GetId() => data.Id;

    /// <summary>Get the current interpolated value</summary>
    public Variant GetCurrentValue()
    {
        if (!isValid || data.Segments.Count == 0) 
            return default;
            
        var segment = data.Segments[data.CurrentSegmentIndex];
        float progress = data.SegmentElapsed / segment.Duration;
        float eased = manager.ApplyEasingFast(progress, segment.TransitionType, segment.Ease);
        return manager.InterpolateFast(segment.Start, segment.End, eased);
    }

    /// <summary>Seek to specific time in the tween</summary>
    public TweenController Seek(float time)
    {
        if (!isValid) return this;
        
        // Calculate which segment and how far in
        float accumulated = 0f;
        for (int i = 0; i < data.Segments.Count; i++)
        {
            float segDuration = data.Segments[i].Duration;
            if (time <= accumulated + segDuration)
            {
                data.CurrentSegmentIndex = i;
                data.SegmentElapsed = time - accumulated;
                data.InvokeOnUpdate();
                return this;
            }
            accumulated += segDuration;
        }
        
        // If beyond duration, complete it
        data.CurrentSegmentIndex = data.Segments.Count - 1;
        data.SegmentElapsed = data.Segments[^1].Duration;
        return this;
    }

    /// <summary>Set progress (0-1) through entire tween</summary>
    public TweenController SetProgress(float progress)
    {
        if (!isValid) return this;
        
        float totalDuration = GetTotalDuration();
        Seek(progress * totalDuration);
        return this;
    }

    /// <summary>Get total duration of all segments</summary>
    public float GetTotalDuration()
    {
        if (!isValid) return 0f;
        
        float total = 0f;
        foreach (var seg in data.Segments)
            total += seg.Duration;
        return total;
    }

    /// <summary>Get remaining time</summary>
    public float GetRemainingTime()
    {
        if (!isValid) return 0f;
        
        float elapsed = 0f;
        for (int i = 0; i < data.CurrentSegmentIndex; i++)
            elapsed += data.Segments[i].Duration;
        elapsed += data.SegmentElapsed;
        
        return GetTotalDuration() - elapsed;
    }

    /// <summary>Reverse the tween direction</summary>
    public TweenController Reverse()
    {
        if (!isValid) return this;
        
        data.Segments.Reverse();
        for (int i = 0; i < data.Segments.Count; i++)
        {
            var segment = data.Segments[i];
            (segment.Start, segment.End) = (segment.End, segment.Start);
            data.Segments[i] = segment;
        }
        
        // Reset to beginning of reversed tween
        data.CurrentSegmentIndex = 0;
        data.SegmentElapsed = 0f;
        
        return this;
    }

    /// <summary>Dynamically change loop count</summary>
    public TweenController SetLoops(int loops, GTween.LoopMode mode = GTween.LoopMode.Linear)
    {
        if (isValid)
        {
            data.Loops = loops;
            data.LoopMode = mode;
        }
        return this;
    }

    /// <summary>Change playback speed dynamically</summary>
    public TweenController SetSpeedScale(float speedScale)
    {
        if (isValid) data.SpeedScale = speedScale;
        return this;
    }

    /// <summary>Get current segment index</summary>
    public int GetCurrentSegmentIndex() => isValid ? data.CurrentSegmentIndex : -1;

    /// <summary>Get progress through current segment (0-1)</summary>
    public float GetSegmentProgress()
    {
        if (!isValid || data.Segments.Count == 0) return 0f;
        var seg = data.Segments[data.CurrentSegmentIndex];
        return data.SegmentElapsed / seg.Duration;
    }

    // 🆕 EVENT ENHANCEMENTS

    /// <summary>Add completion callback without replacing existing</summary>
    public TweenController AddOnComplete(Action callback)
    {
        if (!isValid) return this;
        
        var existing = data.Callback;
        data.Callback = () => { existing?.Invoke(); callback?.Invoke(); };
        return this;
    }

    /// <summary>Chain another tween after this one completes</summary>
    public TweenController Then(Action<TweenBuilder> setup)
    {
        if (!isValid) return this;
        
        AddOnComplete(() => 
        {
            var builder = new TweenBuilder(manager, data.Target, data.Property);
            setup?.Invoke(builder);
        });
        return this;
    }

    /// <summary>Change transition type for all remaining segments</summary>
    public TweenController SetTransition(GTween.TransitionType transition)
    {
        if (!isValid || data.Segments.Count == 0) return this;
        
        data.OverrideTransition = transition;
        return this;
    }

    /// <summary>Change ease direction for all remaining segments</summary>
    public TweenController SetEase(GTween.EaseDirection easing)
    {
        if (!isValid || data.Segments.Count == 0) return this;
        
        data.OverrideEaseDirection = easing;
        return this;
    }

    /// <summary>Change transition and ease for specific segment</summary>
    public TweenController SetSegmentStyle(int segmentIndex, GTween.TransitionType transition, GTween.EaseDirection ease)
    {
        if (!isValid || segmentIndex < 0 || segmentIndex >= data.Segments.Count) 
            return this;
        
        var segment = data.Segments[segmentIndex];
        segment.TransitionType = transition;
        segment.Ease = ease;
        data.Segments[segmentIndex] = segment;
        
        return this;
    }

    /// <summary>Apply cubic transition to remaining segments</summary>
    public TweenController Cubic()
    {
        return SetTransition(GTween.TransitionType.Cubic);
    }

    /// <summary>Apply elastic transition to remaining segments</summary>
    public TweenController Elastic()
    {
        return SetTransition(GTween.TransitionType.Elastic);
    }

    /// <summary>Apply bounce transition to remaining segments</summary>
    public TweenController Bounce()
    {
        return SetTransition(GTween.TransitionType.Bounce);
    }

    /// <summary>Apply back transition to remaining segments</summary>
    public TweenController Back()
    {
        return SetTransition(GTween.TransitionType.Back);
    }

    /// <summary>Apply ease-in to remaining segments</summary>
    public TweenController EaseIn()
    {
        return SetEase(GTween.EaseDirection.In);
    }

    /// <summary>Apply ease-out to remaining segments</summary>
    public TweenController EaseOut()
    {
        return SetEase(GTween.EaseDirection.Out);
    }

    /// <summary>Apply ease-in-out to remaining segments</summary>
    public TweenController EaseInOut()
    {
        return SetEase(GTween.EaseDirection.InOut);
    }

    // 🆕 DYNAMIC SEGMENT MODIFICATION

    /// <summary>Add a new segment to the end of the tween</summary>
    public TweenController AppendSegment(Variant endValue, float duration, 
        GTween.TransitionType transition = GTween.TransitionType.Linear,
        GTween.EaseDirection ease = GTween.EaseDirection.In)
    {
        if (!isValid) return this;

        var lastValue = data.Segments.Count > 0 ? data.Segments[^1].End : data.Target.Get(data.Property);
        
        data.Segments.Add(new TweenSegment
        {
            Start = lastValue,
            End = endValue,
            Duration = duration,
            TransitionType = transition,
            Ease = ease
        });

        return this;
    }

    /// <summary>Insert a segment after the current one</summary>
    public TweenController InsertSegment(Variant endValue, float duration,
        GTween.TransitionType transition = GTween.TransitionType.Linear,
        GTween.EaseDirection ease = GTween.EaseDirection.In)
    {
        if (!isValid) return this;

        var currentSeg = data.Segments[data.CurrentSegmentIndex];
        var newSegment = new TweenSegment
        {
            Start = currentSeg.End,
            End = endValue,
            Duration = duration,
            TransitionType = transition,
            Ease = ease
        };

        // Insert after current segment
        data.Segments.Insert(data.CurrentSegmentIndex + 1, newSegment);
        
        return this;
    }

    /// <summary>Get transition type of current segment</summary>
    public GTween.TransitionType GetCurrentTransition()
    {
        if (!isValid || data.Segments.Count == 0) 
            return GTween.TransitionType.Linear;
        return data.Segments[data.CurrentSegmentIndex].TransitionType;
    }

    /// <summary>Get ease direction of current segment</summary>
    public GTween.EaseDirection GetCurrentEase()
    {
        if (!isValid || data.Segments.Count == 0) 
            return GTween.EaseDirection.In;
        return data.Segments[data.CurrentSegmentIndex].Ease;
    }

    /// <summary>Set Godot Curve for remaining segments</summary>
    public TweenController SetEase(Curve curve)
    {
        if (!isValid || data.Segments.Count == 0) return this;
        
        for (int i = data.CurrentSegmentIndex; i < data.Segments.Count; i++)
        {
            var segment = data.Segments[i];
            segment.UseCustomCurve = true;
            segment.CustomCurve = curve;
            segment.CustomEaseFunction = null;
            data.Segments[i] = segment;
        }
        
        return this;
    }

    /// <summary>Set custom easing function for remaining segments</summary>
    public TweenController SetEase(Func<float, float> easeFunction)
    {
        if (!isValid || data.Segments.Count == 0) return this;
        
        for (int i = data.CurrentSegmentIndex; i < data.Segments.Count; i++)
        {
            var segment = data.Segments[i];
            segment.UseCustomCurve = true;
            segment.CustomEaseFunction = easeFunction;
            segment.CustomCurve = null;
            data.Segments[i] = segment;
        }
        
        return this;
    }

    /// <summary>Use built-in easing for remaining segments</summary>
    public TweenController SetEase(GTween.TransitionType transition, GTween.EaseDirection ease = GTween.EaseDirection.In)
    {
        if (!isValid || data.Segments.Count == 0) return this;
        
        for (int i = data.CurrentSegmentIndex; i < data.Segments.Count; i++)
        {
            var segment = data.Segments[i];
            segment.UseCustomCurve = false;
            segment.TransitionType = transition;
            segment.Ease = ease;
            data.Segments[i] = segment;
        }
        
        return this;
    }
}
