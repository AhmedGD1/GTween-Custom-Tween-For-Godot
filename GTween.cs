using Godot;
using System;
using System.Collections.Generic;

public partial class GTween : Node
{
    // Conflict handling modes
    public enum ConflictMode
    {
        Ignore,      // Don't start new tween if one exists on same property
        Kill,        // Kill existing tween and start new one (default)
        Complete,    // Complete existing tween instantly, then start new one
        Parallel     // Allow multiple tweens on same property
    }

    public enum LoopMode { Linear, PingPong }
    public enum EaseDirection { In, Out, InOut, OutIn }
    public enum TransitionType
    {
        Linear,
        Sine,
        Quad,
        Cubic,
        Quart,
        Quint,
        Expo,
        Circ,
        Back,
        Bounce,
        Elastic
    }


    public static GTween Instance { get; private set; }
    public static VirtualTween Virtual { get; private set; }

    public TweenManager tweenManager = new();

    // Default conflict mode for all tweens
    public static ConflictMode DefaultConflictMode = ConflictMode.Kill;

    // Registry to track which properties are being tweened
    internal static Dictionary<(GodotObject, string), List<TweenData>> propertyTweenRegistry = new();

    public override void _Ready()
    {
        Instance = this;
        Virtual ??= new VirtualTween();
    }

    public override void _Process(double delta)
    {
        tweenManager.Update(delta);
        Virtual.Update((float)delta);
    }

    public static TweenSequence CreateSequence(GodotObject target)
    {
        return new TweenSequence(target, default);
    }

    public static TweenSequence CreateSequence(GodotObject target, object id)
    {
        return new TweenSequence(target, id);
    }

    public static TweenController To(GodotObject target, string property, Variant endValue, float duration = 1f)
    {
        return target.TweenProperty(property).To(endValue).Durations(duration).Start();
    }

    public static TweenController FromTo(GodotObject target, string property, Variant startValue, Variant endValue, float duration = 1f)
    {
        return target.TweenProperty(property).From(startValue).To(endValue).Durations(duration).Start();
    }

    public static TweenController Fade(GodotObject target, float endValue, float duration)
    {
        Color color;

        if (target is Node2D node2D)
        {
            color = node2D.Modulate;
        }
        else if (target is Control control)
        {
            color = control.Modulate;
        }
        else
        {
            return null;
        }

        color.A = endValue;
        return target.TweenProperty("modulate").To(color).Durations(duration).Start();
    }

    public static TweenController FadeIn(GodotObject target, float duration)
    {
        return Fade(target, 1, duration);
    }

    public static TweenController FadeOut(GodotObject target, float duration)
    {
        return Fade(target, 0, duration);
    }

    public static TweenController Scale(GodotObject target, Vector2 endValue, float duration)
    {
        return target.TweenProperty("scale").To(endValue).Durations(duration).Start();
    }

    public static TweenController Move(GodotObject target, Vector2 endValue, float duration)
    {
        return target.TweenProperty("position").To(endValue).Durations(duration).Start();
    }

    public static TweenController Move(GodotObject target, Vector3 endValue, float duration)
    {
        return target.TweenProperty("position").To(endValue).Durations(duration).Start();
    }

    public static TweenController RotateDeg(GodotObject target, float angle, float duration)
    {
        return target.TweenProperty("rotation_degrees").To(angle).Durations(duration).Start();
    }

    public static TweenController RotateRad(GodotObject target, float angle, float duration)
    {
        return target.TweenProperty("rotation").To(angle).Durations(duration).Start();
    }

    public static TweenController RotateDeg(GodotObject target, float angle, Vector3 axis, float duration)
    {
        return target.TweenProperty("rotation_degrees").To(angle * axis).Durations(duration).Start();
    }

    public static TweenController RotateRad(GodotObject target, float angle, Vector3 axis, float duration)
    {
        return target.TweenProperty("rotation").To(angle * axis).Durations(duration).Start();
    }

    public static TweenController Color(GodotObject target, Color color, float duration, bool includeChildren = false)
    {
        return target.TweenProperty(includeChildren ? "modulate" : "self_modulate").To(color).Durations(duration).Start();
    }

    public static TweenController Color(ColorRect colorRect, Color color, float duration)
    {
        return colorRect.TweenProperty("color").To(color).Durations(duration).Start();
    }

    //---------------Text Tweening--------------------------------------;
    public static TweenController TypeText(Label label, string text, float value, float duration, bool clearFirst = true, Action onCharacterRevealed = null)
    {
        if (clearFirst)
            label.Text = "";
        
        label.Text = text;
        label.VisibleRatio = 0;

        var tween = label.TweenProperty("visible_ratio").To(1f).Durations(duration).TransitionWith(TransitionType.Linear);

        if (onCharacterRevealed != null)
            tween.OnUpdate(() => onCharacterRevealed?.Invoke());
        return tween.Start();
    }

    public static TweenController TypeText(RichTextLabel label, string text, float duration, bool clearFirst = true, Action onCharacterRevealed = null)
    {
        if (clearFirst)
            label.Text = "";
        
        label.Text = text;
        label.VisibleRatio = 0;

        var tween = label.TweenProperty("visible_ratio").To(1f).Durations(duration).TransitionWith(TransitionType.Linear);

        if (onCharacterRevealed != null)
            tween.OnUpdate(() => onCharacterRevealed?.Invoke());
        return tween.Start();
    }

    public static void SkipText(Label label)
    {
        Instance.tweenManager.KillTweensOf(label);
        label.VisibleRatio = 1f;
    }

    public static void SkipText(RichTextLabel label)
    {
        Instance.tweenManager.KillTweensOf(label);
        label.VisibleRatio = 1f;
    }

    public static TweenController TypeSoundText(Label label, string text, float duration, AudioStreamPlayer typewriterSound)
    {
        label.Text = text;
        label.VisibleRatio = 0f;
        
        int lastVisibleChars = 0;
        
        return label.TweenProperty("visible_ratio")
            .To(1f)
            .Durations(duration)
            .OnUpdate(() =>
            {
                int currentVisibleChars = Mathf.FloorToInt(label.VisibleRatio * label.Text.Length);
                
                if (currentVisibleChars > lastVisibleChars)
                {
                    typewriterSound?.Play();
                    lastVisibleChars = currentVisibleChars;
                }
            }).Start();
    }
    

    //------------------------------Shake-------------------------------;

    public static TweenController Shake(GodotObject target, string property, float duration, float strength, int vibrato = 10)
    {
        var data = new TweenData
        {
            Target = target,
            Property = property,
            Segments = new(),
            IsShake = true,
            ShakeStrength = strength,
            ShakeVibrato = vibrato
        };

        data.ShakeOriginal = target.Get(property);
        
        float segmentDuration = duration / vibrato;
        for (int i = 0; i < vibrato; i++)
        {
            float t = i / (float)vibrato;
            float currentStrength = strength * (1f - t);
            
            Variant randomOffset = GenerateRandomOffset(data.ShakeOriginal, currentStrength);
            
            data.Segments.Add(new TweenSegment
            {
                Start = i == 0 ? data.ShakeOriginal : data.Segments[^1].End,
                End = randomOffset,
                Duration = segmentDuration,
                Ease = EaseDirection.InOut,
                TransitionType = TransitionType.Sine
            });
        }

        data.Segments.Add(new TweenSegment
        {
            Start = data.Segments[^1].End,
            End = data.ShakeOriginal,
            Duration = segmentDuration,
            Ease = EaseDirection.Out,
            TransitionType = TransitionType.Sine
        });

        return Instance.tweenManager.RegisterTween(data);
    }

    private static Variant GenerateRandomOffset(Variant original, float strength)
    {
        var random = new Random();
        
        switch (original.VariantType)
        {
            case Variant.Type.Vector2:
                Vector2 v2 = original.AsVector2();
                return v2 + new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * strength,
                    (float)(random.NextDouble() * 2 - 1) * strength
                );
            
            case Variant.Type.Vector3:
                Vector3 v3 = original.AsVector3();
                return v3 + new Vector3(
                    (float)(random.NextDouble() * 2 - 1) * strength,
                    (float)(random.NextDouble() * 2 - 1) * strength,
                    (float)(random.NextDouble() * 2 - 1) * strength
                );
            
            case Variant.Type.Float:
                float f = original.AsSingle();
                return f + (float)(random.NextDouble() * 2 - 1) * strength;
            
            default:
                return original;
        }
    }

    /// <summary>
    /// Enhanced Move with shortest path rotation support
    /// </summary>
    public static TweenController MoveSmooth(Node2D target, Vector2 endValue, float duration, 
        bool smoothRotation = true)
    {
        if (!ValidateTarget(target, "MoveSmooth"))
            return null;
            
        ValidateDuration(duration, "MoveSmooth");
        
        var tween = target.TweenProperty("position")
            .To(endValue)
            .Durations(duration);
            
        if (smoothRotation)
        {
            // Calculate direction and smoothly rotate to face it
            Vector2 direction = (endValue - target.Position).Normalized();
            float targetRotation = Mathf.Atan2(direction.Y, direction.X);
            
            // Use shortest path for rotation
            RotateRadShortestPath(target, targetRotation, duration);
        }
        
        return tween.Start();
    }

    /// <summary>
    /// Rotation with shortest path (prevents 350° rotations when -10° would work)
    /// </summary>
    public static TweenController RotateRadShortestPath(Node2D target, float targetAngle, float duration)
    {
        if (!ValidateTarget(target, "RotateRadShortestPath"))
            return null;
            
        ValidateDuration(duration, "RotateRadShortestPath");
        
        float currentAngle = target.Rotation;
        float delta = Mathf.AngleDifference(currentAngle, targetAngle);
        float finalAngle = currentAngle + delta;
        
        return target.TweenProperty("rotation")
            .To(finalAngle)
            .Durations(duration)
            .Start();
    }

    public static TweenController RotateDegShortestPath(Node2D target, float targetAngle, float duration)
    {
        return RotateRadShortestPath(target, Mathf.DegToRad(targetAngle), duration);
    }

    /// <summary>
    /// 3D Rotation with shortest path
    /// </summary>
    public static TweenController RotateRad3DShortestPath(Node3D target, Vector3 targetRotation, float duration)
    {
        if (!ValidateTarget(target, "RotateRad3DShortestPath"))
            return null;
            
        ValidateDuration(duration, "RotateRad3DShortestPath");
        
        Vector3 currentRotation = target.Rotation;
        Vector3 finalRotation = new Vector3(
            currentRotation.X + Mathf.AngleDifference(currentRotation.X, targetRotation.X),
            currentRotation.Y + Mathf.AngleDifference(currentRotation.Y, targetRotation.Y),
            currentRotation.Z + Mathf.AngleDifference(currentRotation.Z, targetRotation.Z)
        );
        
        return target.TweenProperty("rotation")
            .To(finalRotation)
            .Durations(duration)
            .Start();
    }

    /// <summary>
    /// Check if a property is currently being tweened
    /// </summary>
    public static bool IsPropertyTweening(GodotObject target, string property)
    {
        if (target == null || string.IsNullOrEmpty(property))
            return false;
            
        var key = (target, property);
        return propertyTweenRegistry.ContainsKey(key) && 
            propertyTweenRegistry[key].Count > 0;
    }

    /// <summary>
    /// Kill all tweens on a specific property
    /// </summary>
    public static int KillPropertyTweens(GodotObject target, string property)
    {
        var tweens = Instance.tweenManager.GetPropertyTweens(target, property);
        int killed = 0;
        
        foreach (var tween in tweens)
        {
            GTween.Instance.tweenManager.KillTween(tween);
            killed++;
        }
        
        return killed;
    }

    /// <summary>
    /// Complete all tweens on a property instantly
    /// </summary>
    public static void CompletePropertyTweens(GodotObject target, string property)
    {
        var tweens = Instance.tweenManager.GetPropertyTweens(target, property);
        
        foreach (var tween in tweens)
        {
            // Jump to final value
            if (tween.Segments.Count > 0)
            {
                var finalSegment = tween.Segments[^1];
                target.Set(property, finalSegment.End);
            }
            
            // Trigger completion callback
            tween.Callback?.Invoke();
            
            // Kill the tween
            Instance.tweenManager.KillTween(tween);
        }
    }

    // VALIDATION METHODS
    
    /// <summary>
    /// Validate target object
    /// </summary>
    private static bool ValidateTarget(GodotObject target, string methodName)
    {
        if (target == null)
        {
            GD.PushError($"GTween.{methodName}: Target is null");
            return false;
        }
        
        if (!IsInstanceValid(target))
        {
            GD.PushError($"GTween.{methodName}: Target is not a valid instance");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Validate duration
    /// </summary>
    private static void ValidateDuration(float duration, string methodName)
    {
        if (duration < 0)
        {
            GD.PushWarning($"GTween.{methodName}: Duration is negative ({duration}), clamping to 0");
        }
        
        if (duration == 0)
        {
            GD.PushWarning($"GTween.{methodName}: Duration is 0, tween will complete instantly");
        }
    }

    /// <summary>
    /// Validate property exists on target
    /// </summary>
    private static bool ValidateProperty(GodotObject target, string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            GD.PushError("GTween: Property name is null or empty");
            return false;
        }

        // Check if property exists (handles sub-properties like "modulate:a")
        string baseProperty = property.Contains(':') ? property.Split(':')[0] : property;
        
        if (target.Get(baseProperty).VariantType == Variant.Type.Nil)
        {
            GD.PushWarning($"GTween: Property '{property}' may not exist on {target.GetClass()}");
        }
        
        return true;
    }

    //--------------------------------;
    public static void KillAll()
    {
        Instance.tweenManager.KillAll();
    }

    public static int KillByType<TData>()
    {
        return Instance.tweenManager.KillByType<TData>();
    }

    public static int KillById(object id)
    {
        return Instance.tweenManager.KillById(id);
    }

    public static void KillTween(TweenData tween)
    {
        Instance.tweenManager.KillTween(tween);
    }

    public static int KillTweensOf(GodotObject target)
    {
        return Instance.tweenManager.KillTweensOf(target);
    }

    public static void TogglePause(bool toggle)
    {
        Instance.tweenManager.TogglePause(toggle);
    }

    public static int GetActiveTweenCount()
    {
        return Instance.tweenManager.activeTweenCount;
    }

    public static void TogglePauseOf(GodotObject target, bool toggle)
    {
        if (target == null) return;
        
        for (int i = 0; i < Instance.tweenManager.activeTweenCount; i++) // ← Use count, not array length
        {
            var tween = Instance.tweenManager.activeTweens[i];
            if (tween != null && tween.Target == target) // ← Add null check
            {
                tween.IsPaused = toggle;
            }
        }
    }
}

public static class GTweenExtensions
{
    public static TweenBuilder TweenProperty(this GodotObject target, string property)
    {
        return new TweenBuilder(GTween.Instance.tweenManager, target, property);
    }

    public static TweenController Animate(this GodotObject target, string property, Variant[] values, float[] durations,
        GTween.EaseDirection? easeType = null, GTween.TransitionType? trans = null, Action callback = null, object id = default,
            int loops = 1, GTween.LoopMode loopMode = GTween.LoopMode.Linear, float delay = 0f)
    {
        TweenData tweenData = new TweenData
        {
            Target = target,
            Property = property,
            Segments = new(),
            SegmentElapsed = 0f,
            CurrentSegmentIndex = 0,
            Loops = loops,
            LoopMode = loopMode,
            Callback = callback,
            Id = id,
            Delay = delay
        };

        var last = target.Get(property);

        for (int i = 0; i < values.Length; i++)
        {
            tweenData.Segments.Add(new TweenSegment
            {
                Start = last,
                End = values[i],
                Duration = durations[i],
                Ease = easeType ?? GTween.EaseDirection.In,
                TransitionType = trans ?? GTween.TransitionType.Linear
            });

            last = values[i];
        }

        return GTween.Instance.tweenManager.RegisterTween(tweenData);
    }
}

public class TweenBuilder
{
    private GTween.ConflictMode conflictMode = GTween.DefaultConflictMode;
    private GTween.EaseDirection easingType;
    private GTween.TransitionType transitionType;

    private TweenManager manager;
    private GodotObject target;
    private TweenData tweenData;
    private List<Variant> steps = new();
    private List<float> durations = new();
    private Variant? from;

    private Curve customCurve;
    private Func<float, float> customEaseFunction;

    private bool isRelative = false;
    private bool snapToInt = false;
    private bool autoKill = true;


    public TweenBuilder(TweenManager manager, GodotObject target, string property)
    {
        this.manager = manager;
        this.target = target;

        tweenData = new TweenData
        {
            Target = target,
            Property = property,
            Segments = new(),
            CurrentSegmentIndex = 0,
            SegmentElapsed = 0,  
        };
    }

    /// <summary>
    /// Set how this tween should handle conflicts with existing tweens
    /// </summary>
    public TweenBuilder SetConflictMode(GTween.ConflictMode mode)
    {
        conflictMode = mode;
        return this;
    }

    public TweenBuilder SetId(object id)
    {
        tweenData.Id = id;
        return this;
    }

    public TweenBuilder From(Variant value)
    {
        from = value;
        return this;
    }

    public TweenBuilder To(params Variant[] value)
    {
        steps.AddRange(value);
        return this;
    }

    public TweenBuilder Durations(params float[] value)
    {
        durations.AddRange(value);
        return this;
    }

    public TweenBuilder SetEase(Curve curve)
    {
        customCurve = curve;
        return this;
    }

    public TweenBuilder SetEase(Func<float, float> easeFunction)
    {
        customEaseFunction = easeFunction;
        return this;
    }

    public TweenBuilder EaseWith(GTween.EaseDirection easing)
    {
        easingType = easing;
        return this;
    }

    public TweenBuilder TransitionWith(GTween.TransitionType type)
    {
        transitionType = type;
        return this;
    }

    public TweenBuilder Loop(int count, GTween.LoopMode mode = GTween.LoopMode.Linear)
    {
        tweenData.Loops = count;
        tweenData.LoopMode = mode;
        return this;
    }

    public TweenBuilder OnStart(Action callback)
    {
        tweenData.OnStartCallback = callback;
        return this;
    }

    public TweenBuilder OnUpdate(Action callback)
    {
        tweenData.OnUpdateCallback = callback;
        return this;
    }

    public TweenBuilder OnComplete(Action callback)
    {
        tweenData.Callback = callback;
        return this;
    }

    public TweenBuilder OnKill(Action callback)
    {
        tweenData.OnKillCallback = callback;
        return this;
    }

    public TweenBuilder SetDelay(float value)
    {
        tweenData.Delay = value;
        return this;
    }

    public TweenBuilder SetSpeedScale(float value)
    {
        tweenData.SpeedScale = value;
        return this;
    }

    public TweenBuilder SetRelative(bool relative = true)
    {
        isRelative = relative;
        return this;
    }

    public TweenBuilder SetSnapping(bool snap = true)
    {
        snapToInt = snap;
        return this;
    }

    public TweenBuilder SetAutoKill(bool autoKill = true)
    {
        this.autoKill = autoKill;
        return this;
    }

    public TweenController Start()
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            GD.PushError("GTween: Cannot start tween with null or invalid target");
            return null;
        }
        if (string.IsNullOrEmpty(tweenData.Property))
        {
            GD.PushError("GTween: Cannot start tween with null or empty property");
            return null;
        }
        if (steps.Count == 0)
        {
            GD.PushError("GTween: Cannot start tween with no target values");
            return null;
        }
        if (steps.Count != durations.Count)
        {
            GD.PushError($"GTween: Mismatch - {steps.Count} values but {durations.Count} durations");
            return null;
        }
        
        for (int i = 0; i < durations.Count; i++)
        {
            if (durations[i] < 0)
            {
                GD.PushWarning($"GTween: Negative duration clamped to 0.01");
                durations[i] = 0.01f;
            }
        }

        var last = from ?? target.Get(tweenData.Property);

        if (isRelative)
        {
            var currentValue = target.Get(tweenData.Property);
            
            for (int i = 0; i < steps.Count; i++)
            {
                Variant absoluteValue = AddVariants(currentValue, steps[i]);
                
                tweenData.Segments.Add(new TweenSegment
                {
                    Start = last,
                    End = absoluteValue,
                    Duration = durations[i],
                    Ease = easingType,
                    TransitionType = transitionType,
                    CustomCurve = customCurve,
                    CustomEaseFunction = customEaseFunction,
                    UseCustomCurve = customCurve != null || customEaseFunction != null
                });

                last = absoluteValue;
            }
        }
        else
        {
            for (int i = 0; i < steps.Count; i++)
            {
                tweenData.Segments.Add(new TweenSegment
                {
                    Start = last,
                    End = steps[i],
                    Duration = durations[i],
                    Ease = easingType,
                    TransitionType = transitionType
                });

                last = steps[i];
            }
        }

        if (GTween.IsPropertyTweening(target, tweenData.Property))
        {
            switch (conflictMode)
            {
                case GTween.ConflictMode.Ignore:
                    GD.Print($"GTween: Ignoring new tween on {tweenData.Property}, one already exists");
                    return null;
                    
                case GTween.ConflictMode.Kill:
                    GTween.KillPropertyTweens(target, tweenData.Property);
                    break;
                    
                case GTween.ConflictMode.Complete:
                    GTween.CompletePropertyTweens(target, tweenData.Property);
                    break;
                    
                case GTween.ConflictMode.Parallel:
                    // Allow multiple tweens
                    break;
            }
        }

        tweenData.SnapToInt = snapToInt;
        tweenData.AutoKill = autoKill;

        return manager.RegisterTween(tweenData);
    }

    public TweenBuilder Cubic()
    {
        TransitionWith(GTween.TransitionType.Cubic);
        return this;
    }

    public TweenBuilder Sine()
    {
        TransitionWith(GTween.TransitionType.Sine);
        return this;
    }

    public TweenBuilder Elastic()
    {
        TransitionWith(GTween.TransitionType.Elastic);
        return this;
    }

    public TweenBuilder Back()
    {
        TransitionWith(GTween.TransitionType.Back);
        return this;
    }

    public TweenBuilder Quad()
    {
        TransitionWith(GTween.TransitionType.Quad);
        return this;
    }

    public TweenBuilder Bounce()
    {
        TransitionWith(GTween.TransitionType.Bounce);
        return this;
    }

    public TweenBuilder Circ()
    {
        TransitionWith(GTween.TransitionType.Circ);
        return this;
    }

    public TweenBuilder EaseIn()
    {
        EaseWith(GTween.EaseDirection.In);
        return this;
    }

    public TweenBuilder EaseOut()
    {
        EaseWith(GTween.EaseDirection.Out);
        return this;
    }

    public TweenBuilder EaseInOut()
    {
        EaseWith(GTween.EaseDirection.InOut);
        return this;
    }

    public TweenBuilder EaseOutIn()
    {
        EaseWith(GTween.EaseDirection.OutIn);
        return this;
    }

    private Variant AddVariants(Variant a, Variant b)
    {
        return a.VariantType switch
        {
            Variant.Type.Float => (Variant)(a.AsSingle() + b.AsSingle()),
            Variant.Type.Vector2 => (Variant)(a.AsVector2() + b.AsVector2()),
            Variant.Type.Vector3 => (Variant)(a.AsVector3() + b.AsVector3()),
            _ => b,
        };
    }
}


// Corrected GCurves class using Godot's Curve properly
public static class GCurves
{
    // Cache curves to avoid recreation
    private static Curve _easeIn;
    private static Curve _easeOut;
    private static Curve _easeInOut;
    private static Curve _bounce;
    private static Curve _elastic;
    private static Curve _backIn;
    private static Curve _backOut;

    public static Curve EaseIn
    {
        get
        {
            if (_easeIn == null)
            {
                _easeIn = new Curve();
                _easeIn.AddPoint(new Vector2(0, 0));
                _easeIn.AddPoint(new Vector2(1, 1));
                // Configure for ease-in (starts slow, ends fast)
                _easeIn.SetPointRightMode(0, Curve.TangentMode.Linear);
                _easeIn.SetPointLeftMode(1, Curve.TangentMode.Linear);
            }
            return _easeIn;
        }
    }

    public static Curve EaseOut
    {
        get
        {
            if (_easeOut == null)
            {
                _easeOut = new Curve();
                _easeOut.AddPoint(new Vector2(0, 0));
                _easeOut.AddPoint(new Vector2(1, 1));
                // Configure for ease-out (starts fast, ends slow)
                _easeOut.SetPointRightMode(0, Curve.TangentMode.Linear);
                _easeOut.SetPointLeftMode(1, Curve.TangentMode.Linear);
            }
            return _easeOut;
        }
    }

    public static Curve EaseInOut
    {
        get
        {
            if (_easeInOut == null)
            {
                _easeInOut = new Curve();
                _easeInOut.AddPoint(new Vector2(0, 0));
                _easeInOut.AddPoint(new Vector2(0.5f, 0.5f));
                _easeInOut.AddPoint(new Vector2(1, 1));
                // S-curve shape
                _easeInOut.SetPointRightMode(0, Curve.TangentMode.Linear);
                _easeInOut.SetPointLeftMode(2, Curve.TangentMode.Linear);
            }
            return _easeInOut;
        }
    }

    public static Curve Bounce
    {
        get
        {
            if (_bounce == null)
            {
                _bounce = new Curve();
                _bounce.AddPoint(new Vector2(0, 0));
                _bounce.AddPoint(new Vector2(0.2f, 0.8f));
                _bounce.AddPoint(new Vector2(0.4f, 0.3f));
                _bounce.AddPoint(new Vector2(0.6f, 0.9f));
                _bounce.AddPoint(new Vector2(0.8f, 0.5f));
                _bounce.AddPoint(new Vector2(1, 1));
            }
            return _bounce;
        }
    }

    public static Curve Elastic
    {
        get
        {
            if (_elastic == null)
            {
                _elastic = new Curve();
                _elastic.AddPoint(new Vector2(0, 0));
                _elastic.AddPoint(new Vector2(0.1f, -0.2f));
                _elastic.AddPoint(new Vector2(0.3f, 1.3f));
                _elastic.AddPoint(new Vector2(0.5f, 0.7f));
                _elastic.AddPoint(new Vector2(0.7f, 1.1f));
                _elastic.AddPoint(new Vector2(1, 1));
            }
            return _elastic;
        }
    }

    public static Curve BackIn
    {
        get
        {
            if (_backIn == null)
            {
                _backIn = new Curve();
                _backIn.AddPoint(new Vector2(0, 0));
                _backIn.AddPoint(new Vector2(0.3f, -0.2f)); // Back up first
                _backIn.AddPoint(new Vector2(1, 1));
            }
            return _backIn;
        }
    }

    public static Curve BackOut
    {
        get
        {
            if (_backOut == null)
            {
                _backOut = new Curve();
                _backOut.AddPoint(new Vector2(0, 0));
                _backOut.AddPoint(new Vector2(0.7f, 1.2f)); // Overshoot
                _backOut.AddPoint(new Vector2(1, 1));
            }
            return _backOut;
        }
    }

    // Helper method to create custom curves easily
    public static Curve CreateCustom(params Vector2[] points)
    {
        var curve = new Curve();
        foreach (var point in points)
        {
            curve.AddPoint(point);
        }
        return curve;
    }

    // Load curve from resource file
    public static Curve LoadFromResource(string path)
    {
        return ResourceLoader.Load<Curve>(path);
    }
}

