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
    public float Health;
    public float Concealment;

    public float Energy;
    public float Combat;
    public float Metabolism;

    public float EatingTime = 2;
    public float EnergyLoss;

    public float ENERGY_MAX = 150;

    public float ENERGY_MODIFIER = 0.2f;
    public void Initialize(float speed, float strength, float intelligence, float libido, float sight, float endurance, float health, float concealment)
    {
        Speed = speed;
        Strength = strength;
        Intelligence = intelligence;
        Libido = libido;
        Sight = sight;
        Endurance = endurance;
        Health = health;
        Concealment = concealment;

        //Calculate these stats
        Energy = 50;
        Combat = 0;
        Metabolism = 0;

        EnergyLoss = (100 - GetModifiedEndurance()) / 100f * 5;
    }

    public float GetModifiedStat(float mainStat, float inverseStat)
    {
        float offset = (-Mathf.Pow((inverseStat-50)/15, 3));
        offset = Mathf.Min(offset, 0);
        float finalStat = mainStat + offset;
        finalStat *= 1.0f - ENERGY_MODIFIER * Energy / ENERGY_MAX;
        return finalStat;
    }

    public float GetModifiedSpeed()
    {
        return GetModifiedStat(Speed, Strength)/2;
    }

    public float GetModifiedStrength()
    {
        return GetModifiedStat(Strength, Speed);
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

        finalEndurance += Mathf.Min(-Mathf.Pow(Speed-50, 3), 0);
        finalEndurance += Mathf.Min(-Mathf.Pow(Strength-50, 3), 0);
        finalEndurance += Mathf.Min(-Mathf.Pow(Intelligence-50, 3), 0);
        finalEndurance += Mathf.Min(-Mathf.Pow(Sight-50, 3), 0);
        finalEndurance += Mathf.Min(-Mathf.Pow(Concealment-50, 3), 0);
        finalEndurance += Mathf.Min(-Mathf.Pow(Health-50, 3), 0);

        finalEndurance /= 1000;
        finalEndurance += Endurance;
        
        return finalEndurance;
    }

    public float GetModifiedHealth()
    {
        return Health;
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

    public Dictionary<String, float> GetAllAbils()
    {
        Dictionary<String, float> allAbils = new Dictionary<String, float>();

        allAbils.Add("Speed", GetModifiedSpeed());
        allAbils.Add("Strength", GetModifiedStrength());
        allAbils.Add("Intelligence", GetModifiedIntelligence());
        allAbils.Add("Libido", GetModifiedLibido());
        allAbils.Add("Sight", GetModifiedSight());
        allAbils.Add("Endurance", GetModifiedEndurance());
        allAbils.Add("Health", GetModifiedHealth());
        allAbils.Add("Concealment", GetModifiedConcealment());
        allAbils.Add("Energy", Energy);
        allAbils.Add("Combat", Combat);
        allAbils.Add("EnergyLoss", EnergyLoss);

        return allAbils;
    }
}