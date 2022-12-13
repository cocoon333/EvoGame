using Godot;
using System;
using System.Collections.Generic;

public class Team : Node
{
#pragma warning disable 649
    // We assign this in the editor, so we don't need the warning about not being assigned.

    [Export]
    public PackedScene CreatureScene;
#pragma warning restore 649

    //public List<float> StatsList;
    public Abilities TeamAbilities;

    public RandomNumberGenerator Rng;

    public int TeamNumber;
    public int CreatureCount;

    public int EvoPoints;

    public int TotalBirths;

    public int TotalDeaths;

    public int TotalKills;

    public void Initialize()
    {
        Rng = new RandomNumberGenerator();
        Rng.Randomize();
        GD.Randomize();

        //StatsList = new List<float> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
        TeamAbilities = new Abilities();
        TeamAbilities.Initialize(new List<float> { 50f, 50f, 50f, 50f, 50f, 50f, 50f });
    }

    public List<float> GetStats()
    {
        List<float> stats = TeamAbilities.GetStats();
        List<float> randomStats = new List<float>();
        for (int i = 0; i < stats.Count; i++)
        {
            randomStats.Add(Rng.Randfn(stats[i], stats[i] * 0.05f)); // normal distribution with +-5% for standard deviation
        }
        return randomStats;
    }

    public void ChangeStats(int statIndex, int change)
    {
        TeamAbilities.GetStats()[statIndex] += change;
        
        List<float> newStats = TeamAbilities.GetStats();
        newStats[statIndex] += change;
        TeamAbilities.Initialize(newStats);
    }

    public void SpawnCreature(Vector3 location)
    {
        Creature creature = (Creature)CreatureScene.Instance();
        AddChild(creature);

        Abilities abils = (Abilities)creature.GetNode<Node>("Abilities");
        float stats = (float)GD.RandRange(45, 50);
        abils.Initialize(GetStats());

        creature.Initialize(location);

        CreatureCount++;
        TotalBirths++;

        if (TotalBirths % 5 == 0) EvoPoints++;
    }

    public void CreatureDeath(Creature blob)
    {
        if (blob.DesiredFood != null)
        {
            blob.DesiredFood.CurrentSeekers.Remove(blob);
            blob.DesiredFood.BeingAte = false;
        }

        blob.QueueFree();
        CreatureCount--;
        TotalDeaths++;
    }

    public String DisplayTeamInfo()
    {
        String returnString = "";
        returnString += "Team " + (TeamNumber+1) + "\n";
        returnString += "Creature Count: " + CreatureCount + "\n";
        returnString += "Evolution Points: " + EvoPoints + "\n";
        returnString += "Total Births: " + TotalBirths + "\n";
        returnString += "Total Deaths: " + TotalDeaths + "\n";
        returnString += "Total Kills: " + TotalKills + "\n";

        return returnString;
    }
}
