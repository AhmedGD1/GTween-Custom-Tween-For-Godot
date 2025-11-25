using Godot;
using System;
using System.Collections.Generic;

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
        if (isValid) data.IsPaused = true;
        return this;
    }

    public TweenController Play()
    {
        if (isValid) data.IsPaused = false;
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

    public TweenController OnKill(Action callback)
    {
        if (isValid)
            data.OnKillCallback = callback;
        return this;
    }

    public bool IsRunning() => isValid && !data.IsPaused;
    public bool IsPaused() => isValid && data.IsPaused;
    public bool IsComplete() => !isValid;
    public float GetProgress() => isValid ? data.GetTotalProgress() : 1f;



}