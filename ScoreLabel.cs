using Godot;
using System;
using System.Collections.Generic;

public class ScoreLabel : Label
{
    Main MainObj;
    public override void _Ready()
    {
        MainObj = (Main)GetParent();
    }

    public override void _Process(float delta)
    {
        if (this.Visible)
        {
            UpdateString(MainObj.TeamsList, MainObj.FoodList);
        }
    }

    public void UpdateString(List<Team> teams, List<Food> foodList)
    {
        String display = "Teams\n\n";
        foreach (Team team in teams)
        {
            //display += ("Team " + (team.TeamNumber+1) + ": " + team.CreatureCount) + "\n";
            display += team.DisplayTeamInfo() + "\n";
        }
        int poisonedFood = (foodList.FindAll(food => food.Poisonous)).Count;
        display += "Healthy Food Count: " + (foodList.Count-poisonedFood) + "\n";
        display += "Poisonous Food Count: " + poisonedFood;

        if (!Text.Equals(display))
        {
            // not sure if this is all that helpful but maybe reduce a bit of strain to only update the screen text if its actually changed
            Text = display;
        }
    }
}
