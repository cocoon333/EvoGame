using Godot;
using System;
using System.Collections.Generic;

namespace CreatureUtils
{
    public class Abilities
    {
        float Speed;
        float Strength;
        float Intelligence;
        float Libido;
        float Sight;
        float Endurance;
        float Concealment;

        public float ENERGY_MAX = 150;

        public float EatingTime = 2;
        public float DrinkingTime = 5;

        public float SATURATION_MAX = 150;

        public float ENERGY_MODIFIER = 0.2f;

        float Saturation;
        float SaturationLoss;

        float Hydration;
        float HydrationLoss;
        float ENERGY_DIFF_MODIFIER = 10;
        public float HYDRATION_MAX = 150;
        public void Initialize(float speed, float strength, float intelligence, float libido, float sight, float endurance, float concealment)
        {
            List<float> stats = new List<float> { speed, strength, intelligence, libido, sight, endurance, concealment };
            Initialize(stats);
        }

        public void Initialize(List<float> stats)
        {
            Speed = stats[0];
            Strength = stats[1];
            Intelligence = stats[2];
            Libido = stats[3];
            Sight = stats[4];
            Endurance = stats[5];
            Concealment = stats[6];

            //Calculate these stats
            Saturation = 50;
            Hydration = 50;
            SaturationLoss = (100 - GetModifiedEndurance()) / 100f * 5;
            HydrationLoss = (100 - GetModifiedEndurance()) / 100f * 5;

            // DEBUG PURPOSES
            //Hydration = 151;
            //HydrationLoss = 0;
        }

        public float GetModifiedStat(float mainStat, float inverseStat)
        {
            float offset = (-Mathf.Pow((inverseStat - 50) / 15, 3));
            offset = Mathf.Min(offset, 0);
            float finalStat = mainStat + offset;
            finalStat *= 1.0f - ENERGY_MODIFIER * ((ENERGY_MAX - GetEnergy()) / ENERGY_MAX);
            return finalStat;
        }

        public float GetModifiedSpeed()
        {
            return GetModifiedStat(Speed, Strength);
        }

        public float GetModifiedStrength()
        {
            return GetModifiedStat(Strength, Speed);
        }

        public float GetCombatScore()
        {
            float combatScore = GetModifiedStrength();
            combatScore += (GetModifiedSpeed() * 0.1f);
            combatScore += (GetModifiedIntelligence() + GetModifiedSight()) * 0.05f;
            return combatScore;
        }

        public float GetModifiedIntelligence()
        {
            return Intelligence;
        }

        public float GetModifiedLibido()
        {
            return Libido;
        }

        public float GetModifiedSight()
        {
            float finalSight = Sight + Intelligence * 0.1f;
            return finalSight;
        }

        public float GetModifiedEndurance()
        {
            float finalEndurance = 0;

            finalEndurance += Mathf.Min(-Mathf.Pow(Speed - 50, 3), 0);
            finalEndurance += Mathf.Min(-Mathf.Pow(Strength - 50, 3), 0);
            finalEndurance += Mathf.Min(-Mathf.Pow(Intelligence - 50, 3), 0);
            finalEndurance += Mathf.Min(-Mathf.Pow(Sight - 50, 3), 0);
            finalEndurance += Mathf.Min(-Mathf.Pow(Concealment - 50, 3), 0);

            finalEndurance /= 1000;
            finalEndurance += Endurance;

            return finalEndurance;
        }

        public float GetModifiedConcealment()
        {
            return Concealment;
        }

        public float GetSaturation()
        {
            return Saturation;
        }
        public void SetSaturation(float saturation)
        {
            Saturation = saturation;
        }
        public float GetSaturationLoss()
        {
            return SaturationLoss;
        }
        public float GetHydration()
        {
            return Hydration;
        }
        public void SetHydration(float hydration)
        {
            Hydration = hydration;
        }
        public float GetHydrationLoss()
        {
            return HydrationLoss;
        }
        public float GetEnergy()
        {
            return ((GetSaturation() + GetHydration()) / 2) - ((Mathf.Abs(GetSaturation() - GetHydration()) / ENERGY_DIFF_MODIFIER));
        }


        public void WonFight(float winnerScore, float loserScore)
        {
            // TODO: change this to something better
            this.Saturation -= (1 / (winnerScore - loserScore)) * 15;
            this.Hydration -= (1 / (winnerScore - loserScore)) * 15;
        }

        public Dictionary<String, float> GetAllAbils()
        {
            Dictionary<String, float> allAbils = new Dictionary<String, float>();

            allAbils.Add("Speed", GetModifiedSpeed());
            allAbils.Add("Strength", GetModifiedStrength());
            allAbils.Add("Intelligence", GetModifiedIntelligence());
            allAbils.Add("Libido", GetModifiedLibido());
            allAbils.Add("Sight", GetModifiedSight());
            allAbils.Add("Endurance", GetModifiedEndurance());
            allAbils.Add("Concealment", GetModifiedConcealment());
            allAbils.Add("Energy", GetEnergy());
            allAbils.Add("Saturation", Saturation);
            allAbils.Add("SaturationLoss", SaturationLoss);
            allAbils.Add("Hydration", Hydration);
            allAbils.Add("HydrationLoss", HydrationLoss);

            return allAbils;
        }

        public List<float> GetModifiedStats()
        {
            return new List<float> { GetModifiedSpeed(), GetModifiedStrength(), GetModifiedIntelligence(), GetModifiedLibido(), GetModifiedSight(), GetModifiedEndurance(), GetModifiedConcealment() };
        }

        public List<float> GetStats()
        {
            return new List<float> { Speed, Strength, Intelligence, Libido, Sight, Endurance, Concealment };
        }
    }
}