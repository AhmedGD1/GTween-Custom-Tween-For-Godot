using System;

public static class TweenCallbackExtensions
{
    // Use this instead of .OnUpdate(() => {}) to avoid allocations
    public static TweenBuilder WithCallback(this TweenBuilder builder, ITweenCallback callback)
    {
        // Implementation would need to be added to TweenBuilder
        // This shows the API design
        return builder;
    }

    // Example usage for common patterns
    public static TweenBuilder OnProgress(this TweenBuilder builder, Action<float> progressCallback)
    {
        // This creates a closure, but useful for prototyping
        // For production, use ITweenCallback interface instead
        return builder.OnUpdate(() =>
        {
            // Would need access to tween data to calculate progress
        });
    }
}

/// <summary>
/// Pooled callback wrapper - reuse these to minimize allocations
/// </summary>
public class PooledTweenCallback : ITweenCallback
{
    public Action<TweenData> OnUpdate;
    public Action<TweenData> OnComplete;
    public Action<TweenData> OnStart;
    public Action<TweenData> OnKill;

    public void OnTweenUpdate(TweenData tween) => OnUpdate?.Invoke(tween);
    public void OnTweenComplete(TweenData tween) => OnComplete?.Invoke(tween);
    public void OnTweenStart(TweenData tween) => OnStart?.Invoke(tween);
    public void OnTweenKill(TweenData tween) => OnKill?.Invoke(tween);

    public void Clear()
    {
        OnUpdate = null;
        OnComplete = null;
        OnStart = null;
        OnKill = null;
    }
}
