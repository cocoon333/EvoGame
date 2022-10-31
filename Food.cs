using Godot;
using System;

public class Food : KinematicBody
{
    public int Replenishment;
    public Boolean Poisonous;

    public Boolean Eating;

    
    // Called when the node enters the scene tree for the first time.

    public void Initialize(int replenishment, Boolean poisonous, Vector3 spawnLoc)
    {
        Replenishment = replenishment;
        Poisonous = poisonous;
        if (spawnLoc == null)
        {
            Translation = new Vector3((float)GD.RandRange(-50, 50), 1.6f, (float)GD.RandRange(-50, 50));
        }
        else
        {
            Translation = spawnLoc;
        }
    }
}
