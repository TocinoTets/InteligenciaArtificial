using System;
using System.Collections.Generic;

namespace IA.Agents
{
    public class HunterFSM
    {
        private readonly Dictionary<HunterStateId, HunterState> states = new Dictionary<HunterStateId, HunterState>();

        public HunterState CurrentState { get; private set; }
        public HunterStateId CurrentId => CurrentState != null ? CurrentState.Id : HunterStateId.Patrol;
        public float TimeInState { get; private set; }

        public event Action<HunterStateId, HunterStateId> StateChanged;

        public void Register(HunterState state)
        {
            if (state == null) return;
            states[state.Id] = state;
        }

        public void Start(HunterStateId initialState)
        {
            ChangeState(initialState);
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            TimeInState += deltaTime;

            if (CurrentState.TryGetTransition(out HunterStateId next))
            {
                ChangeState(next);
                return;
            }

            CurrentState.Execute(deltaTime);
        }

        private void ChangeState(HunterStateId id)
        {
            if (!states.TryGetValue(id, out HunterState next)) return;
            if (CurrentState != null && CurrentState.Id == id) return;

            HunterStateId previous = CurrentId;

            CurrentState?.Exit();
            CurrentState = next;
            TimeInState = 0f;
            CurrentState.Enter();

            StateChanged?.Invoke(previous, id);
        }
    }
}