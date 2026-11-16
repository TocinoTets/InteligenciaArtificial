using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        private InterestObject closestVisibleInterest;
        private float closestHunterDistance;

        public InterestObject ClosestVisibleInterest => closestVisibleInterest;
        public bool SeesHunter => detectedHunter != null;
        public bool SeesInterest => closestVisibleInterest != null;

        public bool HasValidTarget =>
            targetInterest != null &&
            targetInterest.IsAvailable &&
            SqrDistanceToTarget() <= interestSightRadius * interestSightRadius;

        public bool IsTargetInReach =>
            HasValidTarget &&
            SqrDistanceToTarget() <= interactionDistance * interactionDistance;

        public bool IsTargetWithinConsumeRange =>
            HasValidTarget &&
            SqrDistanceToTarget() <= (interactionDistance + consumeExitMargin) * (interactionDistance + consumeExitMargin);

        private void Sense()
        {
            ClearPerception();

            int count = Physics.OverlapSphereNonAlloc(transform.position, PerceptionQueryRadius(), overlapBuffer);
            for (int i = 0; i < count; i++) ClassifyCollider(overlapBuffer[i]);

            closestVisibleInterest = SelectClosestInterest();
        }

        private void ClearPerception()
        {
            neighbors.Clear();
            deadNeighbors.Clear();
            visibleInterests.Clear();
            detectedHunter = null;
            closestVisibleInterest = null;
            closestHunterDistance = float.MaxValue;
        }

        private float PerceptionQueryRadius()
        {
            return Mathf.Max(Mathf.Max(perceptionRadius, viewRadius), Mathf.Max(interestSightRadius, separationRadius));
        }

        private void ClassifyCollider(Collider other)
        {
            if (other == null || other == bodyCollider) return;

            if (TryRegisterBoid(other)) return;
            if (TryRegisterHunter(other)) return;
            TryRegisterInterest(other);
        }

        private bool TryRegisterBoid(Collider other)
        {
            BoidAgent otherBoid = other.GetComponentInParent<BoidAgent>();
            if (otherBoid == null) return false;
            if (otherBoid == this) return true;

            float sqrDistance = AgentMath.FlatSqrDistance(transform.position, otherBoid.transform.position);

            if (otherBoid.IsAlive)
            {
                if (sqrDistance <= perceptionRadius * perceptionRadius) neighbors.Add(otherBoid);
            }
            else if (otherBoid.IsHarvestable && sqrDistance <= separationRadius * separationRadius)
            {
                deadNeighbors.Add(otherBoid);
            }

            return true;
        }

        private bool TryRegisterHunter(Collider other)
        {
            HunterNPC hunter = other.GetComponentInParent<HunterNPC>();
            if (hunter == null) return false;

            float distance = AgentMath.FlatDistance(transform.position, hunter.transform.position);
            if (distance <= viewRadius && distance < closestHunterDistance)
            {
                closestHunterDistance = distance;
                detectedHunter = hunter;
            }

            return true;
        }

        private void TryRegisterInterest(Collider other)
        {
            InterestObject interest = other.GetComponentInParent<InterestObject>();
            if (interest == null || !interest.IsAvailable) return;

            float sqrDistance = AgentMath.FlatSqrDistance(transform.position, interest.transform.position);
            if (sqrDistance <= interestSightRadius * interestSightRadius) visibleInterests.Add(interest);
        }

        private InterestObject SelectClosestInterest()
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

        private float SqrDistanceToTarget()
        {
            return AgentMath.FlatSqrDistance(transform.position, targetInterest.transform.position);
        }
    }
}