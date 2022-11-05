using Godot;
using System;

public class ScoreLabel : Label
{
    public int CreatureCount = 0;
    public int FoodCount = 0;
    
    public string DisplayString = "Creature Count: {0}\nFood Count: {1}";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void IncrementCreature() {
        CreatureCount++;
        
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
