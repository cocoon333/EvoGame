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

    Creature SelectedCreature = null;

    public List<Team> TeamsList = new List<Team>();

    public int FoodCount = 0;
    List<Food> FoodList = new List<Food>();

    Team PlayerTeam;

    public const int MAP_SIZE = 513;

    const int DEFAULT_REPLENSHIMENT = 25;

    // Export Variables

    [Export] public int NumberOfTeams { get; set; } = 1;
    [Export] public int CreaturesPerTeam { get; set; } = 100;
    [Export] public int InitialFoodAmount { get; set; } = 200;
    [Export] public bool IsDrought { get; set; } = false; // auto get set for isDrought private variable
    [Export] public float WaterLevel { get; set; } = 0.5f;
    [Export] public float DrinkableWaterDepth { get; set; } = 2;

    float[] MapArray = new float[MAP_SIZE * MAP_SIZE];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    public void NewGame()
    {
        // create the map
        CreateMap();

        // Empty Team List
        foreach (Team team in TeamsList)
        {
            team.QueueFree();
        }
        TeamsList.Clear();

        // Empty Food List
        FoodCount = 0;
        foreach (Food food in FoodList)
        {
            if (!IsNullOrQueued(food)) food.QueueFree();
        }
        FoodList.Clear();

        // Spawn Food
        for (int i = 0; i < InitialFoodAmount; i++)
        {
            SpawnFood();
        }

        // Spawn Teams
        for (int i = 0; i < NumberOfTeams; i++)
        {
            Team team = (Team)TeamScene.Instance();
            team.TeamNumber = i;
            TeamsList.Add(team);
            team.Initialize();

            Node teamParent = GetNode<Node>("TeamParent");
            teamParent.AddChild(team);

            // Spawn Creatures
            for (int j = 0; j < CreaturesPerTeam; j++)
            {
                SpawnCreature(team);
            }
        }

        // Set the PlayerTeam
        PlayerTeam = TeamsList[0];

        // Close the main menu and make the world visible
        Control mainMenu = GetNode<Control>("MainMenuScreen");
        mainMenu.Visible = false;
        Spatial arenaNodes = GetNode<Spatial>("ArenaNodes");
        arenaNodes.Visible = true;

        // Make scoreboard and creature label visible
        UpdateCreatureLabel(null);
        Label creatureLabel = GetNode<Label>("CreatureLabel");
        creatureLabel.Visible = true;
        Label scoreLabel = GetNode<Label>("ScoreLabel");
        scoreLabel.Visible = true;

    }

    public void CreateMap()
    {
        // Set up the map
        var terrain = GetNode<Node>("ArenaNodes/Terrain");
        Godot.Object hterraindata = (Godot.Object)terrain.Call("get_data");

        // looks complicated but checks if MAP_SIZE is a power of two plus one
        // neither c# or godot has built in log methods for anything other than natural log and common log which is incredibly annoying
        Debug.Assert(Mathf.FloorToInt(Mathf.Log(MAP_SIZE - 1) / Mathf.Log(2)) == Mathf.CeilToInt(Mathf.Log(MAP_SIZE - 1) / Mathf.Log(2)));
        int mapsize = (int)hterraindata.Call("get_resolution");
        if (mapsize != MAP_SIZE) // TODO: resizing doesnt even work, collision is gone for some reason
        {
            hterraindata.Call("resize", MAP_SIZE, true, new Vector2(-1, -1));
            hterraindata.Call("notify_full_change");
        }

        MapArray = (float[])hterraindata.Call("get_all_heights");
        Debug.Assert(MapArray.Length == MAP_SIZE * MAP_SIZE);

        MeshInstance ground = GetNode<MeshInstance>("ArenaNodes/Water");
        PlaneMesh mesh = (PlaneMesh)(ground.Mesh);
        mesh.Size = new Vector2(MAP_SIZE, MAP_SIZE);
        ground.Translation = new Vector3(MAP_SIZE / 2, WaterLevel, MAP_SIZE / 2);

        for (int i = 1; i <= 4; i++)
        {
            StaticBody wall = GetNode<StaticBody>("ArenaNodes/Wall" + i);
            float scale = (MAP_SIZE - 1) / 512; // TODO: 512 is hardcoded to be default map size
            wall.Scale = new Vector3(scale, 1, scale);
        }
    }

    public void SpawnCreature(Vector3 location, Team team)
    {
        if (team.TotalBirths >= CreaturesPerTeam && IsDrought)
        {
            WaterLevel -= 0.01f;
            MeshInstance water = GetNode<MeshInstance>("ArenaNodes/Water");
            Vector3 waterTranslation = water.Translation;
            waterTranslation.y = WaterLevel;
            water.Translation = waterTranslation;
            GD.Print(WaterLevel);
        }

        team.SpawnCreature(location);
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

        //GetNode<ScoreLabel>("ScoreLabel").UpdateString(TeamsList, FoodCount);

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

        FoodCount++;
    }

    public void EatFood(Food food)
    {
        List<Creature> seekers = food.CurrentSeekers;
        Debug.Assert(seekers.Count <= TeamsList.Count);
        foreach (Creature seeker in seekers)
        {
            seeker.DesiredFood = null;
            seeker.EatingTimeLeft = 0;
            seeker.State = Creature.StatesEnum.Nothing;
        }
        FoodList.Remove(food);

        SpawnFood();
        food.QueueFree();

        FoodCount--;
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

    public Boolean IsInDrinkableWater(Vector3 location)
    {
        return (GetHeightAt(location) <= WaterLevel - DrinkableWaterDepth);
    }

    public Tuple<Boolean, float> IsInDrinkableWaterAndGetHeight(Vector3 location)
    {
        float height = GetHeightAt(location);
        return new Tuple<Boolean, float>(height <= WaterLevel - DrinkableWaterDepth, height);
    }

    public float GetHeightAt(Vector3 location)
    {
        int index = Mathf.RoundToInt(location.z) * MAP_SIZE + Mathf.RoundToInt(location.x); // x and z are intentionally swapped, library is reversed
        if (index >= MapArray.Length || index < 0)
        {
            GD.Print("Index out of range for MapArray");
            // TODO: this is a band aid fix
            // rounds z and x to either 0 or MAP_SIZE-1
            index = Mathf.RoundToInt(Mathf.Min(Mathf.Max(location.z, 0), MAP_SIZE - 1)) * MAP_SIZE + Mathf.RoundToInt(Mathf.Min(Mathf.Max(location.x, 0), MAP_SIZE - 1));
        }
        return MapArray[index];
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
        UpdateStatsMenu(); // this assumes stats can only be changed inside the stats menu
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
