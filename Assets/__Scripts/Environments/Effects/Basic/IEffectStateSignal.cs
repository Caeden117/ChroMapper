using System;

public interface IEffectStateSignal<out TSignal>
{
    event Action<TSignal> OnStateChanged;
    TSignal GetCurrentState();
}
