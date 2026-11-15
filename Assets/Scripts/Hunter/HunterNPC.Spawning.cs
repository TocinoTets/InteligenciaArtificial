using UnityEngine;

namespace IA.Agents
{

    public partial class HunterNPC
    {
        public int ActiveInterestObjectCount()
        {
            InterestObject[] found = Object.FindObjectsByType<InterestObject>(FindObjectsSortMode.None);
            int active = 0;
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null && found[i].IsAvailable) active++;
            return active;
        }

        public bool SpawnInterestObject()
        {
            if (interestObjectPrefab == null) return false;

            Vector3 position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);
            for (int attempt = 0; attempt < 10 && AgentMath.FlatDistance(position, transform.position) < 6f; attempt++)
                position = AgentMath.RandomPointInSquare(spawnAreaHalfExtent, InterestObjectY);

            Instantiate(interestObjectPrefab, position, Quaternion.identity);
            return true;
        }
    }
}