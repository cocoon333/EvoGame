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

    List<float> StatsList;

    RandomNumberGenerator Rng;

    public int TeamNumber;
    public int CreatureCount;

    public void Initialize()
    {
        Rng = new RandomNumberGenerator();
        Rng.Randomize();
        GD.Randomize();

        StatsList = new List<float> { 50f, 50f, 50f, 50f, 50f, 50f, 50f };
    }

    public List<float> GetStats()
    {
        List<float> randomStats = new List<float>();
        for (int i = 0; i < StatsList.Count; i++)
        {
            randomStats.Add(Rng.Randfn(StatsList[i], StatsList[i] * 0.05f)); // normal distribution with +-5% for standard deviation
        }
        return randomStats;
    }

    public void ChangeStats(int statIndex, int change)
    {
        StatsList[statIndex] += change;
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
        //scoreLabel.Text = string.Format(scoreLabel.DisplayString, --scoreLabel.CreatureCount, scoreLabel.FoodCount);
    }
}
