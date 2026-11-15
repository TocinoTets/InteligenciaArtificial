using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        private void Sense()
        {
            detectedBoids.Clear();
            closestVisibleBoid = null;
            detectedDeadBoid = null;

            float closestAliveDistance = float.MaxValue;
            float closestDeadDistance = float.MaxValue;

            int count = Physics.OverlapSphereNonAlloc(transform.position, visionRadius, overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null || other == bodyCollider) continue;

                BoidAgent boid = other.GetComponentInParent<BoidAgent>();
                if (boid == null) continue;

                float distance = AgentMath.FlatDistance(transform.position, boid.transform.position);
                if (distance > visionRadius) continue;

                if (boid.IsAlive)
                {
                    detectedBoids.Add(boid);
                    if (distance < closestAliveDistance)
                    {
                        closestAliveDistance = distance;
                        closestVisibleBoid = boid;
                    }
                }
                else if (boid.IsHarvestable && distance < closestDeadDistance)
                {
                    closestDeadDistance = distance;
                    detectedDeadBoid = boid;
                }
            }
        }

        public BoidAgent SelectPrey()
        {
            return closestVisibleBoid;
        }
    }
}