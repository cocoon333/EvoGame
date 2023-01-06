using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Food : StaticBody
{
    Main MainObj;
    public int Replenishment;
    public Boolean Poisonous;

    public List<Creature> CurrentSeekers;
    public float Lifetime;

    SpatialMaterial PoisonousColor = new SpatialMaterial();


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PoisonousColor.AlbedoColor = new Color((175 / 256.0f), 0, 0);

        MainObj = (Main)GetParent().GetParent();
        int numTeams = MainObj.TeamsList.Count;
        CurrentSeekers = new List<Creature>(numTeams);
    }
    public void Initialize(int replenishment, Boolean poisonous, Vector3 spawnLoc, float lifetime)
    {
        Replenishment = replenishment;
        Poisonous = poisonous;
        Lifetime = lifetime;

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

    public override void _PhysicsProcess(float delta)
    {
        Lifetime -= delta;
        if (Lifetime < 0)
        {
            MainObj.EatFood(this); // call EatFood on this object to despawn this food
        }
    }

    public Boolean IsBeingAte(Creature ignoreCreature)
    {
        return CurrentSeekers.Any(creature => (creature != ignoreCreature && creature.State is Creature.StatesEnum.Eating));
    }
}
