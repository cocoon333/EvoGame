using Godot;
using System;
using System.Collections.Generic;

public class Main : Node
{
#pragma warning disable 649
    // We assign this in the editor, so we don't need the warning about not being assigned.
    [Export]
    public PackedScene FoodScene;

    [Export]
    public PackedScene TeamScene;

#pragma warning restore 649

    public Boolean Dragging = false;

    ScoreLabel scoreLabel;
    Creature SelectedCreature = null;

    List<Team> TeamsList;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        scoreLabel = GetNode<ScoreLabel>("ScoreLabel");
        Label label = GetNode<Label>("CreatureLabel");
        label.MarginRight = GetViewport().Size.x;

        TeamsList = new List<Team>();


        for (int i = 0; i < 40; i++)
        {
            SpawnFood();
        }

        for (int i = 0; i < 2; i++)
        {
            Team team = (Team)TeamScene.Instance();
            team.TeamNumber = i;
            TeamsList.Add(team);
            team.Initialize();

            Node teamParent = GetNode<Node>("TeamParent");
            teamParent.AddChild(team);

            for (int j = 0; j < 50; j++)
            {
                SpawnCreature(team);
            }
        }
    }

    public void SpawnCreature(Vector3 location, Team team)
    {
        team.SpawnCreature(location);

        // update the score label here
    }

    public void SpawnCreature(Team team)
    {
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-95, 95), 1.6f, (float)GD.RandRange(-95, 95));
        SpawnCreature(spawnLoc, team);
    }

    public void CreatureDeath(Creature creature)
    {
        Team team = creature.TeamObj;
        team.CreatureDeath(creature);

        // update the score label here
    }

    public void SpawnFood()
    {
        Food food = (Food)FoodScene.Instance();
        Node foodParent = GetNode<Node>("FoodParent");
        foodParent.AddChild(food);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-95, 95), 1.6f, (float)GD.RandRange(-95, 95));
        food.Initialize(25, (GD.Randf() < 0.2), spawnLoc);

        scoreLabel.Text = string.Format(scoreLabel.DisplayString, scoreLabel.CreatureCount, ++scoreLabel.FoodCount);
    }

    public void EatFood(Food food)
    {
        List<Creature> seekers = food.CurrentSeekers;
        foreach (Creature seeker in seekers)
        {
            seeker.DesiredFood = null;
            seeker.EatingTimeLeft = 0;
        }

        SpawnFood();
        food.QueueFree();
        scoreLabel.Text = string.Format(scoreLabel.DisplayString, scoreLabel.CreatureCount, --scoreLabel.FoodCount);
    }

    public void SelectCreature(Creature creature)
    {
        creature.Selected = true;
        creature.UpdateColor();

        if (!IsNullOrQueued(SelectedCreature))
        {
            SelectedCreature.Selected = false;
            SelectedCreature.UpdateColor();

            if (SelectedCreature == creature)
            {
                creature = null;
            }

            SelectedCreature = null;
        }

        SelectedCreature = creature;
        UpdateCreatureLabel(creature);
    }

    public void UpdateCreatureLabel(Creature creature)
    {
        Label label = GetNode<Label>("CreatureLabel");

        if (IsNullOrQueued(creature))
        {
            label.Text = "";
            return;
        }

        Dictionary<String, float> abils = creature.Abils.GetAllAbils();
        String labelText = "";
        foreach (KeyValuePair<String, float> kvp in abils)
        {
            // This seems like a cleaner way to do it but need to figure out how to make it work
            // String appendString = ("{0}: {1}", kvp.Key, kvp.Value);
            // labelText += appendString;

            labelText += (kvp.Key + ": " + Mathf.RoundToInt(kvp.Value) + "\n");
        }
        label.Text = labelText;
    }


    public List<Food> GetAllFoodInSight(Creature creature)
    {
        Node foodParent = GetNode<Node>("FoodParent");
        int foodCount = foodParent.GetChildCount();
        List<Food> allFood = new List<Food>();
        for (int i = 0; i < foodCount; i++)
        {
            Food current = (Food)foodParent.GetChild(i);

            if (!IsNullOrQueued(current) && current.Translation.DistanceTo(creature.Translation) < creature.Abils.GetModifiedSight())
            {
                allFood.Add(current);
            }
        }
        return allFood;
    }

    public List<Creature> GetAllCreaturesInSight(Creature creature)
    {
        List<Creature> allCreatures = new List<Creature>();
        foreach(Team team in TeamsList)
        {
           for (int i = 0; i < team.GetChildCount(); i++)
            {
                Creature current = (Creature)team.GetChild(i);

                if (!IsNullOrQueued(current) && current.Translation.DistanceTo(creature.Translation) < creature.Abils.GetModifiedSight() && current != creature)
                {
                    allCreatures.Add(current);
                }
            } 
        }
        return allCreatures;
    }

    public List<Creature> GetAllTeamMembersInSight(Creature creature)
    {
        List<Creature> allCreatures = GetAllCreaturesInSight(creature);
        GD.Print(allCreatures.Count);
        List<Creature> teamMembers = new List<Creature>();
        foreach (Creature teamMember in allCreatures)
        {
            if (teamMember.TeamObj.TeamNumber == creature.TeamObj.TeamNumber)
            {
                teamMembers.Add(teamMember);
            }
        }
        return teamMembers;
    }

    public Boolean IsNullOrQueued(Node node)
    {
        Boolean retValue = false;
        try
        {
            retValue = (node == null || node.IsQueuedForDeletion());
        }
        catch (ObjectDisposedException)  // the method to check if something is deleted crashes cuz turns out it was deleted
        {
            retValue = true;
        }
        return retValue;
    }

    public void ChangeStat(Team team, int statIndex, int increment)
    {
        team.ChangeStats(statIndex, increment);
    }

    public override void _UnhandledInput(InputEvent @event) // weird architecture
    {
        //base._UnhandledInput(@event);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.Right)
            {
                // Start dragging if right click pressed
                if (!Dragging && mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    Dragging = true;
                }
                // stop dragging if right click released
                if (Dragging && !mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    Dragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelUp)
            {
                Camera cam = GetNode<Camera>("CameraPivot/Camera");
                Vector3 direction = cam.Translation;
                direction.z -= 2;
                if (direction.z <= 5) direction.z = 5;
                cam.Translation = direction;
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelDown)
            {
                Camera cam = GetNode<Camera>("CameraPivot/Camera");
                Vector3 direction = cam.Translation;
                direction.z += 2;
                cam.Translation = direction;
            }
        }
        else if (@event is InputEventMouseMotion motionEvent && Dragging)
        {
            Vector2 relative = motionEvent.Relative;
            Position3D camPivot = GetNode<Position3D>("CameraPivot");
            Camera cam = GetNode<Camera>("CameraPivot/Camera");
            camPivot.RotateY(relative.x / (-1000));
            cam.RotateX(relative.y / (-1000));
            Vector3 rotation = cam.GlobalRotation;
            rotation.x = Math.Max(rotation.x, -1.5f);
            rotation.x = Math.Min(rotation.x, 0.5f);
            cam.GlobalRotation = rotation;
        }
    }



    public override void _Process(float delta)
    {
        UpdateCreatureLabel(SelectedCreature);

        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnFood();
        }
        if (Input.IsActionJustPressed("spawn_blob"))
        {
            SpawnCreature(TeamsList[0]);
        }

        if (Input.IsActionJustPressed("pause_game"))
        {
            GetTree().Paused = !GetTree().Paused;
        }
/*
        if (Input.IsActionJustPressed("increment_speed"))
        {
            ChangeStat(TeamsList[0], 0, 1); // TODO: magic number L, 0 for team id and 1 for increment value
        }

        if (Input.IsActionJustPressed("decrement_speed"))
        {
            ChangeStat(TeamsList[0], 0, -1);
        }

        if (Input.IsActionJustPressed("increment_strength"))
        {
            ChangeStat(TeamsList[0], 1, 1);
        }

        if (Input.IsActionJustPressed("decrement_strength"))
        {
            ChangeStat(TeamsList[0], 1, -1);
        }

        if (Input.IsActionJustPressed("increment_intelligence"))
        {
            ChangeStat(TeamsList[0], 2, 1);
        }

        if (Input.IsActionJustPressed("decrement_intelligence"))
        {
            ChangeStat(TeamsList[0], 2, -1);
        }

        if (Input.IsActionJustPressed("increment_libido"))
        {
            ChangeStat(TeamsList[0], 3, 1);
        }

        if (Input.IsActionJustPressed("decrement_libido"))
        {
            ChangeStat(TeamsList[0], 3, -1);
        }

        if (Input.IsActionJustPressed("increment_sight"))
        {
            ChangeStat(TeamsList[0], 4, 1);
        }

        if (Input.IsActionJustPressed("decrement_sight"))
        {
            ChangeStat(TeamsList[0], 4, -1);
        }

        if (Input.IsActionJustPressed("increment_endurance"))
        {
            ChangeStat(TeamsList[0], 5, 1);
        }

        if (Input.IsActionJustPressed("decrement_endurance"))
        {
            ChangeStat(TeamsList[0], 5, -1);
        }

        if (Input.IsActionJustPressed("increment_concealment"))
        {
            ChangeStat(TeamsList[0], 6, 1);
        }

        if (Input.IsActionJustPressed("decrement_concealment"))
        {
            ChangeStat(TeamsList[0], 6, -1);
        }
*/

        if (Input.IsActionPressed("exit_game"))
        {
            GetTree().Quit();
        }


    }
}
