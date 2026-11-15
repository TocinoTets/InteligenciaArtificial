using UnityEngine;

namespace IA.Agents
{
    public partial class HunterNPC
    {
        public float EnergyNormalized => Mathf.Clamp01(energy / maxEnergy);
        public bool IsEnergyDepleted => energy <= 0f;
        public bool IsEnergyFull => energy >= maxEnergy;

        public void DrainEnergy(float deltaTime)
        {
            energy = Mathf.Max(0f, energy - huntEnergyDrainPerSecond * deltaTime);
        }

        public void RechargeEnergy(float deltaTime)
        {
            energy = Mathf.Min(maxEnergy, energy + restEnergyRechargePerSecond * deltaTime);
        }
    }
}