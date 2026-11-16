using System.Collections.Generic;

namespace IA.Agents
{
    public class BoidFSM
    {
        private readonly Dictionary<BoidStateId, BoidState> states = new Dictionary<BoidStateId, BoidState>();

        public BoidState CurrentState { get; private set; }
        public BoidStateId CurrentId => CurrentState != null ? CurrentState.Id : BoidStateId.Flocking;

        public void Register(BoidState state)
        {
            if (state == null) return;
            states[state.Id] = state;
        }

        public void ChangeState(BoidStateId id)
        {
            if (!states.TryGetValue(id, out BoidState next)) return;
            if (CurrentState != null && CurrentState.Id == id) return;

            CurrentState?.Exit();
            CurrentState = next;
            CurrentState.Enter();
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }
    }
}