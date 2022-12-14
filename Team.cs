using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public List<Creature> TeamMembers;

    public int EvoPoints;

    public int TotalBirths;

    public int TotalDeaths;

    public int TotalKills;

    float totalDeathAgeTime;

    public void Initialize()
    {
        Rng = new RandomNumberGenerator();
        Rng.Randomize();
        GD.Randomize();

        TeamAbilities = new Abilities();
        TeamAbilities.Initialize(new List<float> { 50f, 50f, 50f, 50f, 50f, 50f, 50f });
        TeamAbilities.Energy = 100;
        TeamMembers = new List<Creature>();
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
        TeamMembers.Add(creature);

        if (TotalBirths % 5 == 0) EvoPoints++;
    }

    public void CreatureDeath(Creature creature)
    {
        if (creature.DesiredFood != null)
        {
            creature.DesiredFood.CurrentSeekers.Remove(creature);
            creature.DesiredFood.BeingAte = false;
        }

        TeamMembers.Remove(creature);
        totalDeathAgeTime += creature.TimeAlive;
        CreatureCount--;
        TotalDeaths++;

        if (TeamMembers.Count != CreatureCount)
        {
            GD.Print(TeamMembers.Count + " " + CreatureCount + "\n");
        }

        creature.QueueFree();
    }

    public float GetAverageDeathAge()
    {
        return (totalDeathAgeTime / TotalDeaths);
    }

    public float GetAverageAge()
    {
        return (TeamMembers.Sum(creature => creature.TimeAlive) / TeamMembers.Count);
    }

    public float GetAverageNumChildren()
    {
        // GD.Print(TeamMembers.Sum(creature => creature.NumChildren) + "\n");
        GD.Print(TeamMembers.Count + " " + CreatureCount + "\n");
        // int test = 0;
        // foreach (Creature creature in TeamMembers)
        // {
        //     test++;
        // }
        // GD.Print(test);

        return (TeamMembers.Sum(creature => creature.NumChildren) / TeamMembers.Count);
    }

    public float GetAverageKills()
    {
        return (TeamMembers.Sum(creature => creature.Kills) / TeamMembers.Count);
    }

    public String DisplayTeamInfo()
    {
        String returnString = "";
        returnString += "Team " + (TeamNumber + 1) + "\n";
        returnString += "Creature Count: " + CreatureCount + "\n";
        returnString += "Evolution Points: " + EvoPoints + "\n";
        returnString += "Total Births: " + TotalBirths + "\n";
        returnString += "Total Deaths: " + TotalDeaths + "\n";
        returnString += "Total Kills: " + TotalKills + "\n";

        return returnString;
    }
}
