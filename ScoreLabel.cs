using Godot;
using System;
using System.Collections.Generic;

public class ScoreLabel : Label
{    
    public void UpdateString(List<Team> teams, int foodCount)
    {
        String display = "Teams\n\n";
        foreach (Team team in teams)
        {
            //display += ("Team " + (team.TeamNumber+1) + ": " + team.CreatureCount) + "\n";
            display += team.DisplayTeamInfo() + "\n";
        }
        display += "Food Count: " + foodCount;
        Text = display;
    }
}
