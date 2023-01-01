using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Main : Node
{
#pragma warning disable 649
    // We assign this in the editor, so we don't need the warning about not being assigned.
    [Export]
    public PackedScene FoodScene;

    [Export]
    public PackedScene TeamScene;

#pragma warning restore 649

    Boolean rightDragging = false;
    Boolean middleDragging = false;

    Creature SelectedCreature = null;

    List<Team> TeamsList = new List<Team>();

    int FoodCount = 0;
    List<Food> FoodList = new List<Food>();

    Team PlayerTeam;

    public float WaterLevel;

    bool isDrought = false;

    public const int MAP_SIZE = 513;

    const int DEFAULT_REPLENSHIMENT = 25;

    int initialFoodAmount = 200;
    public int InitialFoodAmount { get; set; }

    int numberOfTeams = 1;
    public int NumberOfTeams { get; set; }
    int creaturesPerTeam = 1;
    public int CreaturesPerTeam { get; set; }

    float[] MapArray = new float[MAP_SIZE * MAP_SIZE];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    public void NewGame()
    {
        foreach (Team team in TeamsList)
        {
            team.QueueFree();
        }
        TeamsList.Clear();
        FoodCount = 0;
        UpdateCreatureLabel(null);

        foreach (Food food in FoodList)
        {
            if (!IsNullOrQueued(food)) food.QueueFree();
        }
        FoodList.Clear();

        MeshInstance ground = GetNode<MeshInstance>("ArenaNodes/Water");
        WaterLevel = ground.Translation.y;

        var terrain = GetNode<Node>("ArenaNodes/Terrain");
        Godot.Object hterraindata = (Godot.Object)terrain.Call("get_data");
        MapArray = (float[])hterraindata.Call("get_all_heights");

        for (int i = 0; i < initialFoodAmount; i++)
        {
            SpawnFood();
        }

        for (int i = 0; i < numberOfTeams; i++)
        {
            Team team = (Team)TeamScene.Instance();
            team.TeamNumber = i;
            TeamsList.Add(team);
            team.Initialize();

            Node teamParent = GetNode<Node>("TeamParent");
            teamParent.AddChild(team);

            for (int j = 0; j < creaturesPerTeam; j++)
            {
                SpawnCreature(team);
            }
        }

        PlayerTeam = TeamsList[0];

        Control mainMenu = GetNode<Control>("MainMenuScreen");
        mainMenu.Visible = false;
        Spatial arenaNodes = GetNode<Spatial>("ArenaNodes");
        arenaNodes.Visible = true;

        Label creatureLabel = GetNode<Label>("CreatureLabel");
        creatureLabel.Visible = true;
        Label scoreLabel = GetNode<Label>("ScoreLabel");
        scoreLabel.Visible = true;

    }

    public void SpawnCreature(Vector3 location, Team team)
    {
        if (team.TotalBirths >= numberOfTeams * creaturesPerTeam && isDrought)
        {
            WaterLevel -= 0.01f;
            MeshInstance water = GetNode<MeshInstance>("ArenaNodes/Water");
            Vector3 waterTranslation = water.Translation;
            waterTranslation.y = WaterLevel;
            water.Translation = waterTranslation;
            GD.Print(WaterLevel);
        }

        team.SpawnCreature(location);

        GetNode<ScoreLabel>("ScoreLabel").UpdateString(TeamsList, FoodCount);
    }

    public void SpawnCreature(Team team)
    {
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(0, MAP_SIZE - 5), 0, (float)GD.RandRange(0, MAP_SIZE - 5));
        while (IsInWater(spawnLoc))
        {
            spawnLoc = new Vector3((float)GD.RandRange(0, MAP_SIZE - 5), 0, (float)GD.RandRange(0, MAP_SIZE - 5));
        }
        spawnLoc.y = GetHeightAt(spawnLoc) + 1;
        SpawnCreature(spawnLoc, team);
    }

    public void CreatureDeath(Creature creature)
    {
        creature.TeamObj.CreatureDeath(creature);

        GetNode<ScoreLabel>("ScoreLabel").UpdateString(TeamsList, FoodCount);

        // TODO: game winning code commented out for debugging purposes
        /*
        if (creature.TeamObj.CreatureCount == 0)
        {
            if (creature.TeamObj == PlayerTeam) GameOver();
            else
            {
                int aliveTeams = 0;
                foreach (Team team in TeamsList)
                {
                    if (team.CreatureCount != 0)
                    {
                        aliveTeams++;
                    }
                }
                if (aliveTeams <= 1) GameOver();
            }
        }
        */
    }

    public void SpawnFood()
    {
        Food food = (Food)FoodScene.Instance();
        Node foodParent = GetNode<Node>("FoodParent");
        foodParent.AddChild(food);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(0, MAP_SIZE - 5), 1.6f, (float)GD.RandRange(0, MAP_SIZE - 5));
        while (IsInWater(spawnLoc))
        {
            spawnLoc = new Vector3((float)GD.RandRange(0, MAP_SIZE - 5), 1.6f, (float)GD.RandRange(0, MAP_SIZE - 5));
        }
        spawnLoc.y = GetHeightAt(spawnLoc) + 1;
        food.Initialize(DEFAULT_REPLENSHIMENT, (GD.Randf() < 0.2f), spawnLoc);
        FoodList.Add(food);

        GetNode<ScoreLabel>("ScoreLabel").UpdateString(TeamsList, ++FoodCount); // increments FoodCount and then updates FoodCount label
    }

    public void EatFood(Food food)
    {
        List<Creature> seekers = food.CurrentSeekers;
        foreach (Creature seeker in seekers)
        {
            seeker.DesiredFood = null;
            seeker.EatingTimeLeft = 0;
            seeker.State = Creature.StatesEnum.Nothing;
        }
        FoodList.Remove(food);

        SpawnFood();
        food.QueueFree();

        GetNode<ScoreLabel>("ScoreLabel").UpdateString(TeamsList, --FoodCount); // decrements FoodCount and then updates FoodCount label
    }

    public void SelectCreature(Creature creature)
    {
        /*
        Updates Creature to selected state
        Changes creature color and displays stats in the top left corner
        */
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
            labelText += (kvp.Key + ": " + Mathf.RoundToInt(kvp.Value) + "\n");
        }
        label.Text = labelText;
    }

    public bool IsDrought { get; set; } // auto get set for isDrought private variable

    public List<Food> GetAllFoodInSight(Creature creature)
    {
        List<Food> allFood = new List<Food>();
        float sightSquared = Mathf.Pow(creature.Abils.GetModifiedSight(), 2);
        foreach (Food food in FoodList)
        {
            if (!IsNullOrQueued(food) && food.Translation.DistanceSquaredTo(creature.Translation) < sightSquared)
            {
                allFood.Add(food);
            }
        }
        return allFood;
    }

    public List<Creature> GetAllCreaturesInSight(Creature creature)
    {
        List<Creature> allCreatures = new List<Creature>();
        List<Creature> allCreaturesTwo = new List<Creature>();
        foreach (Team team in TeamsList)
        {
            allCreatures.AddRange(team.TeamMembers);
        }

        float sightSquared = Mathf.Pow(creature.Abils.GetModifiedSight(), 2);

        foreach (Creature otherCreature in allCreatures)
        {
            if (!IsNullOrQueued(otherCreature) && otherCreature.Translation.DistanceSquaredTo(creature.Translation) < sightSquared && creature != otherCreature)
            {
                allCreaturesTwo.Add(otherCreature);
            }
        }

        return allCreaturesTwo;
    }

    public List<Creature> GetAllTeamMembersInSight(Creature creature)
    {
        // finds the team members in the sight of a creature
        List<Creature> teamMembers = new List<Creature>();
        List<Creature> teamMembersTwo = new List<Creature>();
        teamMembers.AddRange(creature.TeamObj.TeamMembers);
        float sightSquared = Mathf.Pow(creature.Abils.GetModifiedSight(), 2);
        foreach (Creature otherCreature in teamMembers)
        {
            if (!IsNullOrQueued(otherCreature) && otherCreature.Translation.DistanceSquaredTo(creature.Translation) < sightSquared && creature != otherCreature)
            {
                teamMembersTwo.Add(otherCreature);
            }
        }
        return teamMembersTwo;
    }

    public Boolean IsInWater(Vector3 location)
    {
        /*
        What a sexy function
        Returns whether a location is in water
        */
        return (GetHeightAt(location) <= WaterLevel);
    }

    public float GetHeightAt(Vector3 location)
    {
        int index = Mathf.RoundToInt(location.z) * 513 + Mathf.RoundToInt(location.x); // x and z are intentionally swapped, library is reversed
        return MapArray[index];
        /*
        var terrain = GetNode<Node>("ArenaNodes/Terrain");
        Godot.Object hterraindata = (Godot.Object)terrain.Call("get_data");
        float height = (float)hterraindata.Call("get_interpolated_height_at", location);
        return height;
        */
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
                if (!rightDragging && mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    rightDragging = true;
                }
                // stop dragging if right click released
                if (rightDragging && !mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    rightDragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.Middle)
            {
                // Start dragging if middle click pressed
                if (!middleDragging && mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    middleDragging = true;
                }
                // stop dragging if middle click released
                if (middleDragging && !mouseEvent.Pressed)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    middleDragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelUp)
            {
                ClippedCamera cam = GetNode<ClippedCamera>("ArenaNodes/CameraPivot/ClippedCamera");
                Vector3 direction = cam.Translation;
                direction.z -= 2;
                if (direction.z <= 5) direction.z = 5;
                cam.Translation = direction;
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelDown)
            {
                ClippedCamera cam = GetNode<ClippedCamera>("ArenaNodes/CameraPivot/ClippedCamera");
                Vector3 direction = cam.Translation;
                direction.z += 2;
                cam.Translation = direction;
            }
        }
        else if (@event is InputEventMouseMotion motionEvent)
        {
            if (rightDragging)
            {
                Vector2 relative = motionEvent.Relative;
                Position3D camPivot = GetNode<Position3D>("ArenaNodes/CameraPivot");
                ClippedCamera cam = GetNode<ClippedCamera>("ArenaNodes/CameraPivot/ClippedCamera");
                camPivot.RotateY(relative.x / (-1000));
                cam.RotateX(relative.y / (-1000));
                Vector3 rotation = cam.GlobalRotation;
                rotation.x = Math.Max(rotation.x, -1.5f);
                rotation.x = Math.Min(rotation.x, 0.5f);
                cam.GlobalRotation = rotation;
            }
            else if (middleDragging)
            {
                Vector2 relative = motionEvent.Relative;
                Position3D camPivot = GetNode<Position3D>("ArenaNodes/CameraPivot");
                ClippedCamera cam = GetNode<ClippedCamera>("ArenaNodes/CameraPivot/ClippedCamera");

                cam.Translation += new Vector3(relative.x, 0, 0);
                camPivot.Translation += new Vector3(0, 0, relative.y);
            }
        }
    }

    public void OnStatsButtonPressed(String buttonPressed)
    {
        if (PlayerTeam.EvoPoints <= 0)
        {
            return;
        }
        else
        {
            PlayerTeam.EvoPoints--;
        }

        if (buttonPressed == "increment_speed")
        {
            ChangeStat(PlayerTeam, 0, 1); // TODO: magic number L, 0 for stat id and 1 for increment value
        }
        else if (buttonPressed == "decrement_speed")
        {
            ChangeStat(PlayerTeam, 0, -1);
        }
        else if (buttonPressed == "increment_strength")
        {
            ChangeStat(PlayerTeam, 1, 1);
        }
        else if (buttonPressed == "decrement_strength")
        {
            ChangeStat(PlayerTeam, 1, -1);
        }
        else if (buttonPressed == "increment_intelligence")
        {
            ChangeStat(PlayerTeam, 2, 1);
        }
        else if (buttonPressed == "decrement_intelligence")
        {
            ChangeStat(PlayerTeam, 2, -1);
        }
        else if (buttonPressed == "increment_libido")
        {
            ChangeStat(PlayerTeam, 3, 1);
        }
        else if (buttonPressed == "decrement_libido")
        {
            ChangeStat(PlayerTeam, 3, -1);
        }
        else if (buttonPressed == "increment_sight")
        {
            ChangeStat(PlayerTeam, 4, 1);
        }
        else if (buttonPressed == "decrement_sight")
        {
            ChangeStat(PlayerTeam, 4, -1);
        }
        else if (buttonPressed == "increment_endurance")
        {
            ChangeStat(PlayerTeam, 5, 1);
        }
        else if (buttonPressed == "decrement_endurance")
        {
            ChangeStat(PlayerTeam, 5, -1);
        }
        else if (buttonPressed == "increment_concealment")
        {
            ChangeStat(PlayerTeam, 6, 1);
        }
        else if (buttonPressed == "decrement_concealment")
        {
            ChangeStat(PlayerTeam, 6, -1);
        }
        else
        {
            // Something terrible has gone wrong
            GD.Print("Tried to change a stat but button passed in an invalid string D: String: " + buttonPressed);
        }
        UpdateStatsMenu();
    }

    public override void _Process(float delta)
    {
        UpdateCreatureLabel(SelectedCreature);

        if (Input.IsActionJustPressed("pause_game"))
        {
            GetTree().Paused = !GetTree().Paused;
        }

        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnFood();
        }
        if (Input.IsActionJustPressed("spawn_blob"))
        {
            SpawnCreature(PlayerTeam);
        }

        if (Input.IsActionJustPressed("exit_menu"))
        {
            if (GetNode<Control>("MainMenuScreen").Visible)
            {
                return;
            }

            if (GetNode<Control>("StatsMenu").Visible)
            {
                ToggleStatsMenu();
            }
            else if (GetNode<Control>("AdvancedStatsScreen").Visible)
            {
                GetNode<Control>("AdvancedStatsScreen").Visible = false;
                Control statsMenu = GetNode<Control>("StatsMenu");
                statsMenu.Visible = true;
            }
            else
            {
                TogglePauseMenu();
            }
        }

        if (Input.IsActionJustPressed("toggle_stats_menu"))
        {
            if (GetNode<Control>("MainMenuScreen").Visible)
            {
                return;
            }

            if (GetNode<Control>("AdvancedStatsScreen").Visible)
            {
                GetNode<Control>("AdvancedStatsScreen").Visible = false;
                Control statsMenu = GetNode<Control>("StatsMenu");
                statsMenu.Visible = true;
            }
            else
            {
                ToggleStatsMenu();
            }
        }
    }

    public void GameOver()
    {
        GetTree().Paused = true;
        Control gameOverScreen = GetNode<Control>("GameOverScreen");
        gameOverScreen.Visible = true;

        Label gameWinnerLabel = gameOverScreen.GetNode<Label>("GameWinnerLabel");
        gameWinnerLabel.Text = "You " + (PlayerTeam.CreatureCount == 0 ? "Lost" : "Won") + "!";
    }

    public void OnStatsButtonPressed()
    {
        // Show User statistics after a game is over
    }

    public void OnAchievementsButtonPressed()
    {
        // Show User achievements from main menu screen
    }

    public void OnMainMenuStatsButtonPressed()
    {
        // Show user lifetime statistics on main menu screen
    }

    public void ToggleStatsMenu()
    {
        Label creatureLabel = GetNode<Label>("CreatureLabel");
        creatureLabel.Visible = !creatureLabel.Visible;
        Label scoreLabel = GetNode<Label>("ScoreLabel");
        scoreLabel.Visible = !scoreLabel.Visible;

        Control statsMenu = GetNode<Control>("StatsMenu");
        statsMenu.Visible = !statsMenu.Visible;
        GetTree().Paused = !GetTree().Paused;

        if (statsMenu.Visible)
        {
            UpdateStatsMenu();
        }
    }

    public void UpdateStatsMenu()
    {
        // All the code to setup/update the stats menu
        Control statsMenu = GetNode<Control>("StatsMenu");

        List<String> stats = new List<String> { "Speed", "Strength", "Intelligence", "Libido", "Sight", "Endurance", "Concealment" };
        List<float> statsList = PlayerTeam.TeamAbilities.GetStats();

        for (int i = 0; i < stats.Count; i++)
        {
            Label number = GetNode<Label>("StatsMenu/" + stats[i] + "Label/Number");
            number.Text = statsList[i] + "";
        }


        Label evoPoints = GetNode<Label>("StatsMenu/EvoPointsLabel");
        evoPoints.Text = "Evolution Points\n" + PlayerTeam.EvoPoints;

        Label idealStats = GetNode<Label>("StatsMenu/StatsInfo/IdealStatsLabel");
        String idealStatsString = "";
        for (int i = 0; i < statsList.Count; i++)
        {
            idealStatsString += statsList[i] + "\n";
        }
        idealStats.Text = idealStatsString;

        List<float> modifiedStats = PlayerTeam.TeamAbilities.GetModifiedStats();
        Label modifiedStatsLabel = GetNode<Label>("StatsMenu/StatsInfo/ActualStatsLabel");
        String modifiedStatsString = "";
        for (int i = 0; i < modifiedStats.Count; i++)
        {
            modifiedStatsString += (Mathf.Round(modifiedStats[i])) + "\n";
        }
        modifiedStatsLabel.Text = modifiedStatsString;
    }

    public void OnAdvancedStatsButtonPressed()
    {
        Control statsMenu = GetNode<Control>("StatsMenu");
        statsMenu.Visible = !statsMenu.Visible;

        Control advancedScreen = GetNode<Control>("AdvancedStatsScreen");
        advancedScreen.Visible = true;
        UpdateAdvancedStatsScreen();
    }

    public void UpdateAdvancedStatsScreen()
    {
        Control advancedScreen = GetNode<Control>("AdvancedStatsScreen");

        // TODO: This code is same as in UpdateStatsMenu() but slightly different, make this a method later maybe
        Label idealStats = GetNode<Label>("AdvancedStatsScreen/StatsInfo/IdealStatsLabel");
        String idealStatsString = "";
        List<float> statsList = PlayerTeam.TeamAbilities.GetStats();
        for (int i = 0; i < statsList.Count; i++)
        {
            idealStatsString += statsList[i] + "\n";
        }
        idealStats.Text = idealStatsString;

        List<float> modifiedStats = PlayerTeam.TeamAbilities.GetModifiedStats();
        Label modifiedStatsLabel = GetNode<Label>("AdvancedStatsScreen/StatsInfo/ActualStatsLabel");
        String modifiedStatsString = "";
        for (int i = 0; i < modifiedStats.Count; i++)
        {
            modifiedStatsString += (Mathf.Round(modifiedStats[i])) + "\n";
        }
        modifiedStatsLabel.Text = modifiedStatsString;

        // This is the extra code for the advanced information
        Label averageStatsLabel = GetNode<Label>("AdvancedStatsScreen/StatsInfo/AverageStatsLabel");
        String averageStatsString = "";
        List<float> averageStats = new List<float>(new float[statsList.Count]); // array sets all entries to 0 and then copied into list

        foreach (Creature creature in PlayerTeam.TeamMembers)
        {
            Abilities abils = creature.Abils;
            List<float> actualStats = abils.GetModifiedStats();
            for (int i = 0; i < actualStats.Count; i++)
            {
                averageStats[i] += actualStats[i];
            }
        }

        for (int i = 0; i < averageStats.Count; i++)
        {
            averageStatsString += (Mathf.Round(averageStats[i] / PlayerTeam.CreatureCount)) + "\n";
        }
        averageStatsLabel.Text = averageStatsString;

        GetNode<Label>("AdvancedStatsScreen/AveragesInfo/AverageTimeAliveLabel").Text = "Age\n" + Mathf.Round(PlayerTeam.GetAverageAge());
        GetNode<Label>("AdvancedStatsScreen/AveragesInfo/AverageDeathAgeLabel").Text = "Death Age\n" + Mathf.Round(PlayerTeam.GetAverageDeathAge());
        GetNode<Label>("AdvancedStatsScreen/AveragesInfo/AverageNumChildrenLabel").Text = "Number of Children\n" + (Mathf.Round(PlayerTeam.GetAverageNumChildren() * 100) / 100);
    }

    public void TogglePauseMenu()
    {
        GetTree().Paused = !GetTree().Paused;
        Control pauseMenu = GetNode<Control>("PauseMenu");
        pauseMenu.Visible = !pauseMenu.Visible;

        Label creatureLabel = GetNode<Label>("CreatureLabel");
        creatureLabel.Visible = !creatureLabel.Visible;
        Label scoreLabel = GetNode<Label>("ScoreLabel");
        scoreLabel.Visible = !scoreLabel.Visible;
    }

    public void OnResumeButtonPressed()
    {
        TogglePauseMenu();
    }

    public void OnNewGameButtonPressed()
    {
        //GetTree().ReloadCurrentScene();
        NewGame();
        Control pauseMenu = GetNode<Control>("PauseMenu");
        if (pauseMenu.Visible) pauseMenu.Visible = false;
        else
        {
            Control gameOverScreen = GetNode<Control>("GameOverScreen");
            if (gameOverScreen.Visible) gameOverScreen.Visible = false;
        }
        GetTree().Paused = false;
    }

    public void OnMainMenuButtonPressed()
    {
        Control mainMenu = GetNode<Control>("MainMenuScreen");
        mainMenu.Visible = true;
        Spatial arenaNodes = GetNode<Spatial>("ArenaNodes");
        arenaNodes.Visible = false;
        GetNode<ScoreLabel>("ScoreLabel").Text = "";
        UpdateCreatureLabel(null);

        Control pauseMenu = GetNode<Control>("PauseMenu");
        if (pauseMenu.Visible) pauseMenu.Visible = false;
        else
        {
            Control gameOverScreen = GetNode<Control>("GameOverScreen");
            if (gameOverScreen.Visible) gameOverScreen.Visible = false;
        }
    }

    public void OnExitButtonPressed()
    {
        GetTree().Quit();
    }
}
