using Godot;
using System;
using System.Collections.Generic;

public class Food : KinematicBody
{
    public int Replenishment;
    public Boolean Poisonous;

    public Boolean BeingAte;
    public List<Creature> CurrentSeekers = new List<Creature>(2); // Change this if there are more than 2 teams

    
    // Called when the node enters the scene tree for the first time.

    public void Initialize(int replenishment, Boolean poisonous, Vector3 spawnLoc)
    {
        Replenishment = replenishment;
        Poisonous = poisonous;

        if(Poisonous)
        {
            // TODO: in the future, have two SpatialMaterial static objects defined
            // one for red and one for green and then materials dont need to be local to scene
            // maybe it saves memory
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            SpatialMaterial material = (SpatialMaterial)meshInst.GetActiveMaterial(0);
            Color color = material.AlbedoColor;
            color = new Color((175/256.0f), 0, 0, color.a);
            material.AlbedoColor = color;
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
