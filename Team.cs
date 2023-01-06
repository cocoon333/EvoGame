using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CreatureUtils;

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
    public int StarvationDeaths;
    public int FightDeaths;
    public int DehydrationDeaths;

    public int TotalKills;

    float totalDeathAgeTime;
    public SpatialMaterial TeamColor;

    public void Initialize()
    {
        Rng = new RandomNumberGenerator();
        Rng.Randomize();
        GD.Randomize();

        TeamAbilities = new Abilities();
        TeamAbilities.Initialize(new List<float> { 50f, 50f, 50f, 50f, 50f, 75f, 50f }); // endurance set to 100 for testing
        TeamAbilities.SetSaturation(100);
        TeamAbilities.SetHydration(100);
        TeamMembers = new List<Creature>();
        Debug.Assert(TeamMembers.Count == 0);

        TeamColor = new SpatialMaterial();
        TeamColor.AlbedoColor = new Color(TeamNumber, TeamNumber, TeamNumber); // this only works for team 0 and 1, black and white
        if (TeamNumber == 2)
        {
            TeamColor.AlbedoColor = new Color(0.5f, 0.5f, 0.5f);
        }
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

        creature.Initialize(location);
        creature.Abils.Initialize(GetStats());

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
        }

        TeamMembers.Remove(creature);
        totalDeathAgeTime += creature.TimeAlive;
        CreatureCount--;
        TotalDeaths++;

        creature.QueueFree();
    }

    public float GetAverageDeathAge()
    {
        return (totalDeathAgeTime / TotalDeaths);
    }

    public float GetAverageAge()
    {
        return TeamMembers.Average(creature => creature.TimeAlive);
    }

    public float GetAverageNumChildren()
    {
        return (float)TeamMembers.Average(creature => creature.NumChildren);
    }

    public float GetAverageKills()
    {
        return (float)TeamMembers.Average(creature => creature.Kills);
    }

    public String DisplayTeamInfo()
    {
        String returnString = "";
        returnString += "Team " + (TeamNumber + 1) + "\n";
        returnString += "Creature Count: " + CreatureCount + "\n";
        returnString += "Evolution Points: " + EvoPoints + "\n";
        returnString += "Total Births: " + TotalBirths + "\n";
        returnString += "Total Kills: " + TotalKills + "\n";
        returnString += "Total Deaths: " + TotalDeaths + "\n";
        returnString += "    Starvation Deaths: " + StarvationDeaths + "\n";
        returnString += "    Dehydration Deaths: " + DehydrationDeaths + "\n";
        returnString += "    Fighting Deaths: " + FightDeaths + "\n";
        returnString += "DEBUG: Avg Energy: " + Mathf.Round(TeamMembers.Average(creature => creature.Abils.GetEnergy()));
        returnString += "\n";

        return returnString;
    }
}
