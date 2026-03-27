using Bilnex.Pos.ViewModels.Base;

namespace Bilnex.Pos.States;

public sealed class PosStateManager : ViewModelBase
{
    private PosState _currentState = PosState.Idle;

    public PosState CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public void SetState(PosState state)
    {
        CurrentState = state;
    }

    public bool IsInState(PosState state)
    {
        return CurrentState == state;
    }
}
