using Godot;
using System;
using System.Collections.Generic;

public class Food : StaticBody
{
    public int Replenishment;
    public Boolean Poisonous;

    public Boolean BeingAte;
    public List<Creature> CurrentSeekers = new List<Creature>(2); // Change this if there are more than 2 teams

    SpatialMaterial PoisonousColor = new SpatialMaterial();


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PoisonousColor.AlbedoColor = new Color((175 / 256.0f), 0, 0);
    }
    public void Initialize(int replenishment, Boolean poisonous, Vector3 spawnLoc)
    {
        Replenishment = replenishment;
        Poisonous = poisonous;

        if (Poisonous)
        {
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            meshInst.MaterialOverride = PoisonousColor;
        }

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
