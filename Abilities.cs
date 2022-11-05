using Godot;
using System;

public class Abilities : Node
{
    [Export]
    public float Speed;
    public float Strength;
    public float Intelligence;
    public float Libido; 
    public float Sight;
    public float Endurance; 
    public float Health;
    public float Concealment; // range from 0 t0 1
    
    public float Energy;
    public float Combat;
    public float Metabolism;
    
    public float EatingTime = 2;
    public float EnergyLoss;

    public void Initialize(float speed, float strength, float intelligence, float libido, float sight, float endurance, float health, float concealment)
    {
        Speed = speed/2;    // modify stats here so "default" stats can stay at 50
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

        EnergyLoss = (100-Endurance)/100f * 5;
    }
    
    public float GetSpeed() {
        return Speed;
    }
    
    public float GetStrength() {
        return Strength;
    }
    
    public float GetIntelligence() {
        return Intelligence;
    }

    public float GetLibido() {
        return Libido;
    }

    public float GetSight() {
        return Sight;
    }

    public float GetEndurance() {
        return Endurance;
    }

    public float GetHealth() {
        return Health;
    }

    public float GetConcealment() {
        return Concealment;
    }

    public float GetEnergy() {
        return Energy;
    }

    public void SetEnergy(float newEnergy) {
        Energy = newEnergy;
    }
}