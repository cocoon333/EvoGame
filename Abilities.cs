using Godot;
using System;
using System.Collections.Generic;


public class Abilities : Node
{
    public float Speed;
    public float Strength;
    public float Intelligence;
    public float Libido;
    public float Sight;
    public float Endurance;
    public float Concealment;

    public float Energy;

    public float EatingTime = 2;
    public float EnergyLoss;

    public float ENERGY_MAX = 150;

    public float ENERGY_MODIFIER = 0.2f;

    public float Hydration;
    public float HydrationLoss;
    public void Initialize(float speed, float strength, float intelligence, float libido, float sight, float endurance, float concealment)
    {
        List<float> stats = new List<float>{speed, strength, intelligence, libido, sight, endurance, concealment};
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
        Energy = 50;
        Hydration = 50;
        EnergyLoss = (100 - GetModifiedEndurance()) / 100f * 5;
        HydrationLoss = (100 - GetModifiedEndurance()) / 100f * 5;
    }

    public float GetModifiedStat(float mainStat, float inverseStat)
    {
        float offset = (-Mathf.Pow((inverseStat - 50) / 15, 3));
        offset = Mathf.Min(offset, 0);
        float finalStat = mainStat + offset;
        finalStat *= 1.0f - ENERGY_MODIFIER * ((ENERGY_MAX - Energy) / ENERGY_MAX);
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

    public float GetEnergy()
    {
        return Energy;
    }

    public void SetEnergy(float newEnergy)
    {
        Energy = newEnergy;
    }

    public void WonFight(float winnerScore, float loserScore) {
        this.Energy -= (winnerScore - loserScore) * 10;
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
        allAbils.Add("Energy", Energy);
        allAbils.Add("EnergyLoss", EnergyLoss);

        return allAbils;
    }

    public List<float> GetModifiedStats()
    {
        return new List<float> {GetModifiedSpeed(), GetModifiedStrength(), GetModifiedIntelligence(), GetModifiedLibido(), GetModifiedSight(), GetModifiedEndurance(), GetModifiedConcealment()};
    }

    public List<float> GetStats()
    {
        return new List<float> {Speed, Strength, Intelligence, Libido, Sight, Endurance, Concealment};
    }
}