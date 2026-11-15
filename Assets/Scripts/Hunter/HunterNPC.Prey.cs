using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        private BoidAgent currentPrey;
        private int preyWrapCountAtLock;
        private Vector3 lastKnownPreyPosition;

        public BoidAgent CurrentPrey => currentPrey;
        public Vector3 LastKnownPreyPosition => lastKnownPreyPosition;
        public bool HasLivePrey => currentPrey != null && currentPrey.IsAlive;
        public bool HasPreyLeftArena => HasLivePrey && currentPrey.WrapCount != preyWrapCountAtLock;

        public void LockPrey(BoidAgent prey)
        {
            currentPrey = prey;

            if (prey == null)
            {
                lastKnownPreyPosition = transform.position;
                return;
            }

            preyWrapCountAtLock = prey.WrapCount;
            lastKnownPreyPosition = prey.Position;
        }

        public void ReleasePrey()
        {
            currentPrey = null;
        }

        public void RememberPreyPosition()
        {
            if (currentPrey != null) lastKnownPreyPosition = currentPrey.Position;
        }
    }
}