using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        public void MoveTowards(Vector3 worldTarget)
        {
            Vector3 desired = AgentMath.FlatDirection(transform.position, worldTarget) * moveSpeed;
            AccelerateTowards(desired, acceleration);
            ApplyMotion();
        }

        public void Pursue(BoidAgent prey)
        {
            if (prey == null)
            {
                StopMoving();
                return;
            }

            MoveTowards(PredictPreyPosition(prey));
        }

        public void StopMoving()
        {
            AccelerateTowards(Vector3.zero, acceleration * brakeMultiplier);
            ApplyMotion();
        }

        public bool HasReached(Vector3 worldTarget, float tolerance)
        {
            return AgentMath.FlatDistance(transform.position, worldTarget) <= tolerance;
        }

        private Vector3 PredictPreyPosition(BoidAgent prey)
        {
            float distance = AgentMath.FlatDistance(transform.position, prey.Position);
            float combinedSpeed = Mathf.Max(0.01f, moveSpeed + AgentMath.Flat(prey.Velocity).magnitude);
            float lookAhead = Mathf.Clamp(distance / combinedSpeed, 0f, pursuitMaxPrediction);

            return prey.Position + AgentMath.Flat(prey.Velocity) * lookAhead;
        }

        private void AccelerateTowards(Vector3 desiredVelocity, float rate)
        {
            velocity = Vector3.MoveTowards(velocity, desiredVelocity, rate * Time.deltaTime);
        }

        private void ApplyMotion()
        {
            Vector3 position = transform.position + velocity * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -arenaHalfExtent, arenaHalfExtent);
            position.z = Mathf.Clamp(position.z, -arenaHalfExtent, arenaHalfExtent);
            position.y = groundY;
            transform.position = position;

            FaceVelocity();
        }

        private void FaceVelocity()
        {
            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude <= 0.05f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
        }
    }
}