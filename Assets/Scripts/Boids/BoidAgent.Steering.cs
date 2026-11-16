using System.Collections.Generic;
using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        private const float MinDistance = 1e-2f;
        private const float NeighborOverlapShare = 0.5f;
        private const float StaticOverlapShare = 1f;

        private int wrapCount;

        public int WrapCount => wrapCount;

        internal void StepFlocking(float dt)
        {
            Vector3 steering = Separation() * separationWeight
                              + Alignment() * alignmentWeight
                              + Cohesion() * cohesionWeight
                              + Wander(dt) * wanderWeight
                              + SeparationFromDead() * (separationWeight * deadSeparationFactor);

            Move(steering, dt);
        }

        internal void StepEvading(float dt)
        {
            Vector3 steering = Evade() * evadeWeight
                              + Separation() * separationWeight
                              + SeparationFromDead() * (separationWeight * deadSeparationFactor);

            Move(steering, dt);
        }

        internal void StepTowardsInterest(float dt)
        {
            Vector3 steering = Separation() * separationWeight
                              + SeparationFromDead() * (separationWeight * deadSeparationFactor);

            if (targetInterest != null) steering += Arrive(targetInterest.transform.position) * arriveWeight;

            Move(steering, dt);
        }

        private void Move(Vector3 steering, float dt)
        {
            ApplySteering(Vector3.ClampMagnitude(steering, maxForce), dt);
            SetGroundedPosition(transform.position + velocity * dt);
            FaceVelocity(dt);
            ResolveOverlaps();
        }

        private void ApplySteering(Vector3 steering, float dt)
        {
            velocity = AgentMath.Flat(velocity + steering * dt);
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        }

        private void SetGroundedPosition(Vector3 position)
        {
            position = WrapAroundArena(position);
            position.y = groundY;
            transform.position = position;
        }

        private Vector3 SteerTo(Vector3 desired)
        {
            Vector3 steer = AgentMath.Flat(desired) - AgentMath.Flat(velocity);
            return Vector3.ClampMagnitude(steer, maxForce);
        }

        private Vector3 Separation()
        {
            return SeparationFrom(neighbors, separationRadius);
        }

        private Vector3 SeparationFromDead()
        {
            return SeparationFrom(deadNeighbors, separationRadius);
        }

        private Vector3 SeparationFrom(List<BoidAgent> others, float radius)
        {
            Vector3 force = Vector3.zero;
            int count = 0;
            float sqrRadius = radius * radius;

            for (int i = 0; i < others.Count; i++)
            {
                float sqrDistance = AgentMath.FlatSqrDistance(transform.position, others[i].transform.position);
                if (sqrDistance <= MinDistance * MinDistance || sqrDistance > sqrRadius) continue;

                Vector3 offset = AgentMath.Flat(transform.position - others[i].transform.position);
                force += offset.normalized / Mathf.Sqrt(sqrDistance);
                count++;
            }

            if (count == 0) return Vector3.zero;

            return SteerTo(force.normalized * maxSpeed);
        }

        private Vector3 Alignment()
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++) sum += neighbors[i].Velocity;

            sum = AgentMath.Flat(sum / neighbors.Count);
            if (sum.sqrMagnitude < 1e-4f) return Vector3.zero;

            return SteerTo(sum.normalized * maxSpeed);
        }

        private Vector3 Cohesion()
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++) center += neighbors[i].transform.position;
            center /= neighbors.Count;

            Vector3 desired = AgentMath.FlatDirection(transform.position, center) * maxSpeed;
            if (desired == Vector3.zero) return Vector3.zero;

            return SteerTo(desired);
        }

        private Vector3 Evade()
        {
            if (detectedHunter == null) return Vector3.zero;

            Vector3 threatPosition = detectedHunter.transform.position;
            Vector3 threatVelocity = AgentMath.Flat(detectedHunter.Velocity);

            float distance = AgentMath.FlatDistance(transform.position, threatPosition);
            float combinedSpeed = Mathf.Max(MinDistance, maxSpeed + threatVelocity.magnitude);
            float lookAhead = Mathf.Clamp(distance / combinedSpeed, 0f, evadeMaxPrediction);

            Vector3 predicted = threatPosition + threatVelocity * lookAhead;
            Vector3 away = AgentMath.FlatDirection(predicted, transform.position);
            if (away == Vector3.zero) away = AgentMath.FlatDirection(threatPosition, transform.position);
            if (away == Vector3.zero) away = transform.forward;

            return SteerTo(away * maxSpeed);
        }

        private Vector3 Arrive(Vector3 target)
        {
            Vector3 offset = AgentMath.Flat(target - transform.position);
            float distance = offset.magnitude;
            if (distance <= MinDistance) return SteerTo(Vector3.zero);

            float slowingSpan = Mathf.Max(MinDistance, arriveSlowingRadius - arriveStopDistance);
            float targetSpeed = maxSpeed * Mathf.Clamp01((distance - arriveStopDistance) / slowingSpan);

            return SteerTo(offset / distance * targetSpeed);
        }

        private Vector3 Wander(float dt)
        {
            wanderAngle += Random.Range(-1f, 1f) * wanderJitter * dt;

            float radians = wanderAngle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));

            return SteerTo(direction * (maxSpeed * wanderSpeedFactor));
        }

        private Vector3 WrapAroundArena(Vector3 position)
        {
            float span = arenaHalfExtent * 2f;
            bool wrapped = false;

            if (position.x > arenaHalfExtent) { position.x -= span; wrapped = true; }
            else if (position.x < -arenaHalfExtent) { position.x += span; wrapped = true; }

            if (position.z > arenaHalfExtent) { position.z -= span; wrapped = true; }
            else if (position.z < -arenaHalfExtent) { position.z += span; wrapped = true; }

            if (wrapped) wrapCount++;
            return position;
        }

        private void ResolveOverlaps()
        {
            Vector3 correction = NeighborOverlapCorrection()
                               + DeadOverlapCorrection()
                               + HunterOverlapCorrection()
                               + InterestOverlapCorrection();

            if (correction.sqrMagnitude < 1e-8f) return;

            SetGroundedPosition(transform.position + correction);
        }

        private Vector3 NeighborOverlapCorrection()
        {
            Vector3 correction = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++)
                correction += PushAway(neighbors[i].transform.position, minSeparationDistance, NeighborOverlapShare);
            return correction;
        }

        private Vector3 DeadOverlapCorrection()
        {
            Vector3 correction = Vector3.zero;
            for (int i = 0; i < deadNeighbors.Count; i++)
                correction += PushAway(deadNeighbors[i].transform.position, minSeparationDistance, StaticOverlapShare);
            return correction;
        }

        private Vector3 HunterOverlapCorrection()
        {
            if (detectedHunter == null) return Vector3.zero;
            return PushAway(detectedHunter.transform.position, minSeparationDistance, StaticOverlapShare);
        }

        private Vector3 InterestOverlapCorrection()
        {
            Vector3 correction = Vector3.zero;
            float bodyRadius = minSeparationDistance * 0.5f;

            for (int i = 0; i < visibleInterests.Count; i++)
            {
                InterestObject interest = visibleInterests[i];
                if (interest == null) continue;

                correction += PushAway(interest.transform.position, interest.FootprintRadius + bodyRadius, StaticOverlapShare);
            }

            return correction;
        }

        private Vector3 PushAway(Vector3 otherPosition, float minDistance, float share)
        {
            Vector3 offset = AgentMath.Flat(transform.position - otherPosition);
            float distance = offset.magnitude;
            if (distance >= minDistance) return Vector3.zero;

            Vector3 direction = distance < 1e-3f ? RandomFlatDirection() : offset / distance;
            return direction * ((minDistance - distance) * share);
        }

        private static Vector3 RandomFlatDirection()
        {
            float radians = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        private void FaceVelocity(float dt)
        {
            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude < 0.05f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * dt);
        }
    }
}