using UnityEngine;

namespace IA.Agents
{

    public partial class BoidAgent
    {
        private void Sense()
        {
            neighbors.Clear();
            deadNeighbors.Clear();
            visibleInterests.Clear();
            detectedHunter = null;

            float queryRadius = Mathf.Max(Mathf.Max(perceptionRadius, viewRadius), Mathf.Max(interestSightRadius, separationRadius));
            int count = Physics.OverlapSphereNonAlloc(transform.position, queryRadius, overlapBuffer);

            float closestHunter = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                BoidAgent otherBoid = other.GetComponentInParent<BoidAgent>();
                if (otherBoid != null)
                {
                    if (otherBoid == this) continue;

                    float sqrDistance = AgentMath.FlatSqrDistance(transform.position, otherBoid.transform.position);

                    if (otherBoid.IsAlive)
                    {
                        if (sqrDistance <= perceptionRadius * perceptionRadius) neighbors.Add(otherBoid);
                    }
                    else if (otherBoid.IsHarvestable && sqrDistance <= separationRadius * separationRadius)
                    {
                        deadNeighbors.Add(otherBoid);
                    }
                    continue;
                }

                HunterNPC hunter = other.GetComponentInParent<HunterNPC>();
                if (hunter != null)
                {
                    float distance = AgentMath.FlatDistance(transform.position, hunter.transform.position);
                    if (distance <= viewRadius && distance < closestHunter)
                    {
                        closestHunter = distance;
                        detectedHunter = hunter;
                    }
                    continue;
                }

                InterestObject interest = other.GetComponentInParent<InterestObject>();
                if (interest != null && interest.IsAvailable &&
                    AgentMath.FlatSqrDistance(transform.position, interest.transform.position) <= interestSightRadius * interestSightRadius)
                {
                    visibleInterests.Add(interest);
                }
            }
        }

        internal InterestObject SelectInterest()
        {
            InterestObject best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < visibleInterests.Count; i++)
            {
                InterestObject candidate = visibleInterests[i];
                if (candidate == null || !candidate.IsAvailable) continue;

                float distance = AgentMath.FlatSqrDistance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}