using Godot;
using System;
using System.Collections.Generic;


public class TweenSequence
{
    private readonly GodotObject target;
    private object id;

    private readonly List<SequenceStep> steps = new();
    private Action onCompleteCallback;
    private Action onStartCallback;
    private Action onStepCallback;

    public TweenSequence(GodotObject target, object id)
    {
        this.target = target;
        this.id = id;
    }

    public TweenSequence Append(string property, Variant value, float duration = 1f)
    {
        steps.Add(SequenceStep.Animate(property, value, duration, GTween.TransitionType.Linear, GTween.EaseDirection.In));
        return this;
    }

    public TweenSequence AppendInterval(float duration)
    {
        steps.Add(SequenceStep.WaitStep(duration));
        return this;
    }

    public TweenSequence AppendCallback(Action callback)
    {
        steps.Add(SequenceStep.CallbackStep(callback));
        return this;
    }

    public TweenSequence SetEase(GTween.EaseDirection easing)
    {
        if (steps.Count > 0)
            steps[^1].EaseType = easing;
        return this;
    }

    public TweenSequence SetTransition(GTween.TransitionType type)
    {
        if (steps.Count > 0)
            steps[^1].Transition = type;
        return this;
    }

    public TweenSequence OnComplete(Action callback)
    {
        onCompleteCallback = callback;
        return this;
    }

    public TweenSequence OnStart(Action callback)
    {
        onStartCallback = callback;
        return this;
    }

    public TweenSequence OnStep(Action callback)
    {
        onStartCallback = callback;
        return this;
    }

    public TweenSequence SetLoops(int count, GTween.LoopMode mode)
    {
        if (steps.Count > 0)
        {
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                steps[i].Loops = count;
                steps[i].LoopMode = mode;
            }
        }
        return this;
    }

    public TweenSequence Play()
    {
        ExecuteNextStep(0, 0);
        onStartCallback?.Invoke();
        return this;
    }

    private void ExecuteNextStep(int index, int loopIndex)
    {
        if (index >= steps.Count)
        {
            onCompleteCallback?.Invoke();
            return;
        }

        SequenceStep step = steps[index];

        switch (step.Type)
        {
            case SequenceStep.StepType.Animate:
                var tween = target.Animate(step.Property, [step.Value], [step.Duration], 
                    step.EaseType, step.Transition, 
                    () => ExecuteNextStep(index + 1, loopIndex));

                onStepCallback?.Invoke();
                break;

            case SequenceStep.StepType.Wait:
                GTween.Instance.GetTree().CreateTimer(step.Duration).Timeout += 
                    () => ExecuteNextStep(index + 1, loopIndex);
                break;

            case SequenceStep.StepType.Callback:
                step.Callback?.Invoke();
                ExecuteNextStep(index + 1, loopIndex);
                break;
        }
    }

    //---------------------------------
    public TweenSequence Cubic()
    {
        SetTransition(GTween.TransitionType.Cubic);
        return this;
    }

    public TweenSequence Sine()
    {
        SetTransition(GTween.TransitionType.Sine);
        return this;
    }

    public TweenSequence Elastic()
    {
        SetTransition(GTween.TransitionType.Elastic);
        return this;
    }

    public TweenSequence Back()
    {
        SetTransition(GTween.TransitionType.Back);
        return this;
    }

    public TweenSequence Quad()
    {
        SetTransition(GTween.TransitionType.Quad);
        return this;
    }

    public TweenSequence Bounce()
    {
        SetTransition(GTween.TransitionType.Bounce);
        return this;
    }

    public TweenSequence Circ()
    {
        SetTransition(GTween.TransitionType.Circ);
        return this;
    }

    public TweenSequence EaseIn()
    {
        SetEase(GTween.EaseDirection.In);
        return this;
    }

    public TweenSequence EaseOut()
    {
        SetEase(GTween.EaseDirection.Out);
        return this;
    }

    public TweenSequence EaseInOut()
    {
        SetEase(GTween.EaseDirection.InOut);
        return this;
    }

    public TweenSequence EaseOutIn()
    {
        SetEase(GTween.EaseDirection.OutIn);
        return this;
    }
}

public class SequenceStep
{
    public enum StepType { Animate, Wait, Callback }

    public StepType Type { get; set; }
    public string Property { get; set; }
    public Variant Value { get; set; }
    public float Duration { get; set; } = 1f;
    public GTween.TransitionType Transition { get; set; }
    public GTween.EaseDirection EaseType { get; set; }
    public Action Callback { get; set; }

    public int Loops { get; set; }
    public GTween.LoopMode LoopMode { get; set; }


    public static SequenceStep Animate(string property, Variant value, float duration, GTween.TransitionType trans, GTween.EaseDirection easeType,
        int loops = 1, GTween.LoopMode loopMode = GTween.LoopMode.Linear)
    {
        SequenceStep step = new SequenceStep()
        {
            Type = StepType.Animate,
            Property = property,
            Value = value,
            Duration = duration,
            Transition = trans,
            EaseType = easeType,
            Loops = loops,
            LoopMode = loopMode
        };

        return step;
    }

    public static SequenceStep WaitStep(float duration)
    {
        SequenceStep step = new SequenceStep()
        {
            Type = StepType.Wait,
            Duration = duration
        };

        return step;
    }

    public static SequenceStep CallbackStep(Action callback)
    {
        SequenceStep step = new SequenceStep()
        {
            Type = StepType.Callback,
            Callback = callback
        };

        return step;
    }
}