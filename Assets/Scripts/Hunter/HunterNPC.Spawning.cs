using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        private const int MaxSpawnAttempts = 10;
        private const float MinSpawnDistanceFromHunter = 6f;

        public int ActiveInterestObjectCount()
        {
            return InterestObject.ActiveCount;
        }

        public bool SpawnInterestObject()
        {
            if (interestObjectPrefab == null) return false;

            Instantiate(interestObjectPrefab, FindSpawnPosition(), Quaternion.identity);
            return true;
        }

        private Vector3 FindSpawnPosition()
        {
            Vector3 position = RandomSpawnPoint();

            for (int attempt = 0; attempt < MaxSpawnAttempts && IsTooCloseToHunter(position); attempt++)
                position = RandomSpawnPoint();

            return position;
        }

        private Vector3 RandomSpawnPoint()
        {
            return AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);
        }

        private bool IsTooCloseToHunter(Vector3 position)
        {
            return AgentMath.FlatDistance(position, transform.position) < MinSpawnDistanceFromHunter;
        }
    }
}