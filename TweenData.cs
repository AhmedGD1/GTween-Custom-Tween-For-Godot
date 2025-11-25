using Godot;
using System;
using System.Collections.Generic;


public interface ITweenCallback
{
    void OnTweenUpdate(TweenData tween);
    void OnTweenComplete(TweenData tween);
    void OnTweenStart(TweenData tween);
    void OnTweenKill(TweenData tween);
    void OnTweenPauseToggle(TweenData tween, bool toggle);
}

public class TweenData
{
    public GodotObject Target { get; set; }
    public object Id { get; set; }

    private string property;
    private StringName cachedPropertyName;
    
    public string Property
    {
        get => property;
        set
        {
            if (property != value)
            {
                property = value;
                cachedPropertyName = new StringName(value);
            }
        }
    }
    
    public StringName PropertyName => cachedPropertyName;

    public Action OnStartCallback { get; set; }
    public Action OnUpdateCallback { get; set; }
    public Action OnKillCallback { get; set; }
    public Action Callback { get; set; }
    public Action<bool> OnPauseToggleCallback { get; set; }

    public ITweenCallback CallbackInterface { get; set; }

    public bool UseInterfaceCallbacks { get; set; }

    public List<TweenSegment> Segments { get; set; } = new();
    public int CurrentSegmentIndex { get; set; }
    public float SegmentElapsed { get; set; }

    public GTween.TransitionType? OverrideTransition { get; set; }
    public GTween.EaseDirection? OverrideEaseDirection { get; set; }

    public bool IsPaused { get; set; }
    public float Delay { get; set; }
    public float SpeedScale { get; set; } = 1f;
    public bool HasStarted { get; set; }

    public GTween.LoopMode LoopMode { get; set; }
    public int Loops { get; set; } = 1;
    public bool IsReversed { get; set; }
    public bool IsKilled { get; set; }

    public bool IsShake { get; set; }
    public float ShakeStrength { get; set; }
    public int ShakeVibrato { get; set; }
    public Variant ShakeOriginal { get; set; }

    public bool SnapToInt { get; set; }
    public bool AutoKill { get; set; } = true;

    private int loopCount = 0;

    public void InvokeOnStart()
    {
        if (UseInterfaceCallbacks)
            CallbackInterface?.OnTweenStart(this);
        else
            OnStartCallback?.Invoke();
    }

    public void InvokeOnUpdate()
    {
        if (UseInterfaceCallbacks)
            CallbackInterface?.OnTweenUpdate(this);
        else
            OnUpdateCallback?.Invoke();
    }

    public void InvokeOnComplete()
    {
        if (UseInterfaceCallbacks)
            CallbackInterface?.OnTweenComplete(this);
        else
            Callback?.Invoke();
    }

    public void InvokeOnKill()
    {
        if (UseInterfaceCallbacks)
            CallbackInterface?.OnTweenKill(this);
        else
            OnKillCallback?.Invoke();
    }

    public void InvokeOnPauseToggle()
    {
        if (UseInterfaceCallbacks)
            CallbackInterface?.OnTweenPauseToggle(this, IsPaused);
        else
            OnPauseToggleCallback?.Invoke(IsPaused);
    }

    public bool CanLoop() => Loops == 0 || (Loops > 1 && loopCount < Loops - 1);
    public void Loop() { if (Loops != 0) loopCount++; }
    public void ResetLoopCount() => loopCount = 0;

    public float GetTotalProgress()
    {
        if (Segments.Count == 0) return 1f;
        
        float totalDuration = 0f;
        foreach (var seg in Segments)
            totalDuration += seg.Duration;

        float elapsed = 0f;
        for (int i = 0; i < CurrentSegmentIndex; i++)
            elapsed += Segments[i].Duration;
        elapsed += SegmentElapsed;

        return Mathf.Clamp(elapsed / totalDuration, 0f, 1f);
    }

    public void Reset()
    {
        Target = null;
        property = null;
        cachedPropertyName = default;
        Callback = null;
        OnStartCallback = null;
        OnUpdateCallback = null;
        OnKillCallback = null;
        CallbackInterface = null;
        UseInterfaceCallbacks = false;
        CurrentSegmentIndex = 0;
        SegmentElapsed = 0f;
        LoopMode = GTween.LoopMode.Linear;
        Loops = 1;
        loopCount = 0;
        IsReversed = false;
        IsPaused = false;
        Delay = 0f;
        SpeedScale = 1f;
        HasStarted = false;
        SnapToInt = false;
        AutoKill = true;
        IsKilled = false;
    }
}

public struct TweenSegment
{
    public Variant Start { get; set; }
    public Variant End { get; set; }
    public float Duration { get; set; }
    public GTween.EaseDirection Ease { get; set; }
    public GTween.TransitionType TransitionType { get; set; }

    public bool UseCustomCurve { get; set; }
    public Curve CustomCurve { get; set; }
    public Func<float, float> CustomEaseFunction { get; set; }
}