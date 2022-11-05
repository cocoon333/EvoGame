using Godot;
using System;

public class ScoreLabel : Label
{
    public int CreatureCount = 0;
    public int FoodCount = 0;
    
    public string DisplayString = "Creature Count: {0}\nFood Count: {1}";
}
