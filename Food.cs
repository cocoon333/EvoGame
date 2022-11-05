using Godot;
using System;

public class Food : KinematicBody
{
    public int Replenishment;
    public Boolean Poisonous;

    public Boolean BeingAte;

    public Creature CurrentSeeker;

    
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
            color.g = 0.5f;
            color.r = 0;
            color.b = 0;
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
