using System;
using System.Collections.Generic;

namespace IA.Agents
{
    public class BoidFSM
    {
        private readonly BoidAgent boid;
        private readonly Dictionary<BoidStateId, BoidState> states = new Dictionary<BoidStateId, BoidState>();

        public BoidState CurrentState { get; private set; }
        public BoidStateId CurrentId => CurrentState != null ? CurrentState.Id : BoidStateId.Flocking;
        public float TimeInState { get; private set; }

        public event Action<BoidStateId, BoidStateId> StateChanged;

        public BoidFSM(BoidAgent boid)
        {
            this.boid = boid;
        }

        public void Register(BoidState state)
        {
            if (state == null) return;
            states[state.Id] = state;
        }

        public void Start(BoidStateId initialState)
        {
            ChangeState(initialState);
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == null) return;

            TimeInState += deltaTime;

            if (TryGetGlobalTransition(out BoidStateId globalNext))
            {
                ChangeState(globalNext);
                return;
            }

            if (CurrentState.TryGetTransition(out BoidStateId next))
            {
                ChangeState(next);
                return;
            }

            CurrentState.Execute(deltaTime);
        }

        private bool TryGetGlobalTransition(out BoidStateId next)
        {
            next = BoidStateId.Dead;

            bool alreadyDown = CurrentId == BoidStateId.Dead || CurrentId == BoidStateId.Respawn;
            return !alreadyDown && boid.IsOutOfHits;
        }

        private void ChangeState(BoidStateId id)
        {
            if (!states.TryGetValue(id, out BoidState next)) return;
            if (CurrentState != null && CurrentState.Id == id) return;

            BoidStateId previous = CurrentId;

            CurrentState?.Exit();
            CurrentState = next;
            TimeInState = 0f;
            CurrentState.Enter();

            StateChanged?.Invoke(previous, id);
        }
    }
}