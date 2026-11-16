using UnityEngine;

namespace IA.Agents
{
    public partial class BoidAgent
    {
        private int wrapCount;

        public int WrapCount => wrapCount;

        internal void StepFlocking(float dt)
        {
            Vector3 steering = Separation(separationRadius) * separationWeight
                              + Alignment() * alignmentWeight
                              + Cohesion() * cohesionWeight
                              + Wander(dt) * wanderWeight
                              + SeparationFromDead() * (separationWeight * 0.6f);

            Integrate(Vector3.ClampMagnitude(steering, maxForce), dt);
        }

        internal void StepEvading(float dt)
        {
            Vector3 steering = Evade() * evadeWeight
                              + Separation(separationRadius) * separationWeight
                              + SeparationFromDead() * (separationWeight * 0.6f);

            Integrate(Vector3.ClampMagnitude(steering, maxForce), dt);
        }

        internal void StepTowardsInterest(float dt)
        {
            Vector3 steering = Separation(separationRadius) * separationWeight
                              + SeparationFromDead() * (separationWeight * 0.6f);

            if (targetInterest != null) steering += Arrive(targetInterest.transform.position) * arriveWeight;

            Integrate(Vector3.ClampMagnitude(steering, maxForce), dt);
        }

        private void Integrate(Vector3 steering, float dt)
        {
            velocity += steering * dt;
            velocity = AgentMath.Flat(velocity);
            if (velocity.magnitude > maxSpeed) velocity = velocity.normalized * maxSpeed;

            Vector3 position = WrapAroundArena(transform.position + velocity * dt);
            position.y = groundY;
            transform.position = position;

            FaceVelocity(dt);
            ResolveOverlaps();
        }

        internal void RefreshThreatMemory(Vector3 threatPosition)
        {
            lastThreatPosition = threatPosition;
            evadeMemoryTimer = evadeMemoryDuration;
        }

        internal void TickThreatMemory(float dt) => evadeMemoryTimer = Mathf.Max(0f, evadeMemoryTimer - dt);
        internal bool HasThreatMemory => evadeMemoryTimer > 0f;

        private Vector3 SteerTo(Vector3 desired)
        {
            Vector3 steer = AgentMath.Flat(desired) - AgentMath.Flat(velocity);
            return Vector3.ClampMagnitude(steer, maxForce);
        }

        private Vector3 Separation(float radius)
        {
            Vector3 force = Vector3.zero;
            int count = 0;
            float sqrRadius = radius * radius;

            for (int i = 0; i < neighbors.Count; i++)
            {
                float sqrDistance = AgentMath.FlatSqrDistance(transform.position, neighbors[i].transform.position);
                if (sqrDistance <= 1e-4f || sqrDistance > sqrRadius) continue;

                Vector3 offset = AgentMath.Flat(transform.position - neighbors[i].transform.position);
                force += offset.normalized / Mathf.Sqrt(sqrDistance);
                count++;
            }

            if (count == 0) return Vector3.zero;

            force /= count;
            return SteerTo(force.normalized * maxSpeed);
        }

        private Vector3 SeparationFromDead()
        {
            Vector3 force = Vector3.zero;
            int count = 0;

            for (int i = 0; i < deadNeighbors.Count; i++)
            {
                float sqrDistance = AgentMath.FlatSqrDistance(transform.position, deadNeighbors[i].transform.position);
                if (sqrDistance <= 1e-4f) continue;

                Vector3 offset = AgentMath.Flat(transform.position - deadNeighbors[i].transform.position);
                force += offset.normalized / Mathf.Sqrt(sqrDistance);
                count++;
            }

            if (count == 0) return Vector3.zero;

            force /= count;
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
            Vector3 threatPosition = detectedHunter != null ? detectedHunter.transform.position : lastThreatPosition;
            Vector3 threatVelocity = detectedHunter != null ? detectedHunter.Velocity : Vector3.zero;

            Vector3 toThreat = AgentMath.Flat(threatPosition - transform.position);
            float distance = toThreat.magnitude;

            float safeDistance = Mathf.Max(viewRadius + 0.01f, evadeSafeDistance);
            if (distance >= safeDistance) return Vector3.zero;

            float speedFactor = Mathf.Clamp01((safeDistance - distance) / (safeDistance - viewRadius));
            float threatSpeed = AgentMath.Flat(threatVelocity).magnitude;
            float lookAhead = Mathf.Clamp(distance / (maxSpeed + threatSpeed + 0.001f), 0f, 1.5f);

            Vector3 predicted = threatPosition + threatVelocity * lookAhead;
            Vector3 away = AgentMath.FlatDirection(predicted, transform.position);
            if (away == Vector3.zero) away = distance > 1e-4f ? -toThreat.normalized : Vector3.forward;

            return SteerTo(away * (maxSpeed * speedFactor));
        }

        private Vector3 Arrive(Vector3 target)
        {
            Vector3 offset = AgentMath.Flat(target - transform.position);
            float distance = offset.magnitude;
            if (distance <= 1e-3f) return Vector3.zero;

            float slowingRadius = interactionDistance * 2.5f;
            float targetSpeed = maxSpeed * Mathf.Clamp01((distance - interactionDistance) / Mathf.Max(0.01f, slowingRadius - interactionDistance));

            Vector3 desired = offset / distance * targetSpeed;
            return Vector3.ClampMagnitude(desired - AgentMath.Flat(velocity), maxForce);
        }

        private Vector3 Wander(float dt)
        {
            wanderAngle += Random.Range(-1f, 1f) * 60f * dt;
            Vector3 direction = new Vector3(Mathf.Cos(wanderAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(wanderAngle * Mathf.Deg2Rad));
            return SteerTo(direction * (maxSpeed * 0.6f));
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
            Vector3 correction = Vector3.zero;

            for (int i = 0; i < neighbors.Count; i++) correction += PushAway(neighbors[i].transform.position);
            for (int i = 0; i < deadNeighbors.Count; i++) correction += PushAway(deadNeighbors[i].transform.position);
            for (int i = 0; i < visibleInterests.Count; i++)
                if (visibleInterests[i] != null) correction += PushAway(visibleInterests[i].transform.position);
            if (detectedHunter != null) correction += PushAway(detectedHunter.transform.position);

            if (correction.sqrMagnitude < 1e-8f) return;

            Vector3 position = transform.position + correction;
            position.y = groundY;
            transform.position = position;
        }

        private Vector3 PushAway(Vector3 otherPosition)
        {
            Vector3 offset = AgentMath.Flat(transform.position - otherPosition);
            float distance = offset.magnitude;
            if (distance >= minSeparationDistance) return Vector3.zero;

            Vector3 direction = distance < 1e-3f
                ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized
                : offset / distance;

            return direction * ((minSeparationDistance - distance) * 0.5f);
        }

        private void FaceVelocity(float dt)
        {
            Vector3 flat = AgentMath.Flat(velocity);
            if (flat.sqrMagnitude < 0.05f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * dt);
        }
    }
}