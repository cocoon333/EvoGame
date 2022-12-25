using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Creature : KinematicBody
{
    public Abilities Abils;
    public float EatingTimeLeft;
    public Food DesiredFood;

    public int FallAcceleration = 75;

    public Creature Mate;

    //public int Team;

    public Team TeamObj;

    public float TimeAlive;

    public int NumChildren;
    public int Kills;

    private Vector3 _velocity = Vector3.Zero;

    const int WATER_REPLENISHMENT = 20;

    Main MainObj;
    SpatialMaterial Material;
    List<Food> Blacklist = new List<Food>();

    public Vector3 DesiredWater = Vector3.Zero;

    public Boolean Selected = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    public void Initialize(Vector3 spawnLoc)
    {

        Translation = spawnLoc;
        Abils = GetNode<Abilities>("Abilities");
        TeamObj = (Team)GetParent();

        Node teamParent = TeamObj.GetParent();
        MainObj = (Main)teamParent.GetParent();

        MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
        Material = (SpatialMaterial)meshInst.GetActiveMaterial(0);


        MeshInstance hat1 = GetNode<MeshInstance>("Hat1");
        MeshInstance hat2 = GetNode<MeshInstance>("Hat2");
        hat1.MaterialOverride = TeamObj.TeamColor;
        hat2.MaterialOverride = TeamObj.TeamColor;
    }

    public void UpdateColor()
    {
        Color color = Material.AlbedoColor;

        // if (MainObj.IsInWater(this))
        // {
        //     color = new Color(1, 1, 1, color.a);
        // }

        if (Selected)
        {
            color = new Color(1, (68 / 256.0f), (51 / 256.0f), color.a);
        }
        else if (CanMate())
        {
            color = new Color(1, 0, 0.5f, color.a);
        }
        else
        {
            color.r = 0;
            color.g = Abils.Energy / 100f;
            color.b = (100 - Abils.Energy) / 100f;
        }
        Material.AlbedoColor = color;
    }

    public override void _Process(float delta)
    {
        if (Abils.Energy > 0) // temporary so energy isnt negative and screwing with color
        {
            UpdateColor();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (IsQueuedForDeletion())
        {
            return;
        }

        TimeAlive += delta;

        Abils.Energy -= (Abils.EnergyLoss * delta);
        Abils.Hydration -= (Abils.HydrationLoss * delta);

        if (Abils.Energy <= 0 || Abils.Hydration <= 0)
        {
            // TODO: make it so health depletes rapidly when energy is 0
            // blob is dead
            MainObj.CreatureDeath(this);
            return;
        }

        if (EatingTimeLeft <= 0)
        {
            _velocity = Vector3.Forward * Abils.GetModifiedSpeed() / 2;
            _velocity = _velocity.Rotated(Vector3.Up, Rotation.y);

            var direction = Vector3.Zero;

            // Vertical velocity
            _velocity.y -= FallAcceleration * delta;

            _velocity = MoveAndSlide(_velocity);

            if (CanMate())
            {
                if (DesiredFood != null)
                {
                    DesiredFood.BeingAte = false;
                    DesiredFood.CurrentSeekers.Remove(this);
                    DesiredFood = null;
                }

                if (MainObj.IsNullOrQueued(Mate))
                {
                    Mate = GetNearestMate();

                    if (Mate != null)
                    {
                        Mate.Mate = this;

                        LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                        Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);
                    }
                }
                else
                {
                    LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                    Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);

                    if (Translation.DistanceSquaredTo(Mate.Translation) < 9)
                    {
                        Mate.Abils.SetEnergy(Mate.Abils.GetEnergy() - 60);
                        Abils.SetEnergy(Abils.GetEnergy() - 60);

                        NumChildren++;
                        Mate.NumChildren++;

                        Mate.Mate = null;
                        Mate = null;

                        MainObj.SpawnCreature(Translation, TeamObj);
                    }
                }
            }
            else
            {
                if (Abils.Energy <= Abils.Hydration)
                { // TODO: more detailed calculation in the future for which will run out first
                    LookAtClosestFood();
                }
                else
                {
                    LookAtClosestWater();
                }

                if (DesiredFood != null && Translation.DistanceSquaredTo(DesiredFood.Translation) < 4.1)
                {
                    // just over distance of 2 (food and creature have radius 1) to be safe
                    StartEatingFood();
                }
                else if (MainObj.IsInWater(Translation) && DesiredFood == null && DesiredWater != Vector3.Zero && Abils.Hydration < 100 && Translation.DistanceSquaredTo(DesiredWater) < 4.1)
                {
                    Drink(delta);
                }
            }


            for (int i = 0; i < GetSlideCount(); i++)
            {
                KinematicCollision collision = GetSlideCollision(i);
                if (!(collision.Collider is StaticBody sb && sb.IsInGroup("ground")))
                {
                    if (!(collision.Collider is Creature creat && creat != this))
                    {
                        //if (DesiredFood != null) GD.Print(DesiredFood.Translation, " ", EatingTimeLeft);
                        if (!MainObj.IsNullOrQueued(DesiredFood))
                        {
                            LookAtFromPosition(Translation, DesiredFood.Translation, Vector3.Up);
                        }
                        else if (Mate != null)
                        {
                            LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                            Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);
                        }
                        else
                        {
                            RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
                        }
                        break;
                    }
                    else if (collision.Collider is Creature && creat.TeamObj != TeamObj)
                    {
                        //Fight(creat);
                    }
                }
            }
        }
        else
        {
            EatingTimeLeft -= delta;
            Eat(delta);
            if (EatingTimeLeft <= 0)
            {
                EatingTimeLeft = 0;
                MainObj.EatFood(DesiredFood);
            }
        }
    }

    // TODO: Sort this method out and organize all its conditions
    // somewhat sorted but still kinda sucks tbh
    public Boolean CanMate()
    {
        Boolean canMate = false;
        float libido = Abils.GetModifiedLibido();
        float energy = Abils.GetEnergy();

        // TODO: make energy - libido relationship curved
        // true if Mate already exists or all of the following are true: No desired food, alive for 20+ seconds, and energy is less than 150 minus libido
        if (Mate != null || (DesiredFood == null && TimeAlive > 20 && (energy > (150 - libido)))) canMate = true;

        return canMate;
    }

    public void StartEatingFood()
    {
        if (DesiredFood == null) GD.Print("Started eating food but desired food was null");

        Creature enemy = null;
        if (DesiredFood.BeingAte)
        {
            foreach (Creature creature in DesiredFood.CurrentSeekers)
            {
                if (creature != this)
                {
                    if (creature.TeamObj == TeamObj) { GD.Print("Friendly fire has occured"); }
                    enemy = creature;
                    break;
                }
            }
        }

        DesiredFood.BeingAte = true;
        EatingTimeLeft = Abils.EatingTime;

        if (enemy != null)
        {
            // this means that the other seeker (the enemy) has already reached this food
            // this blob is the second to arrive to the food and can now determine whether or not a fight occurs
            Fight(enemy);
        }
    }

    public void StartDrinkingWater()
    {
        // if creature is drinking same water with enemy go for a fight?
    }

    public Boolean WantsToFight(Creature enemy)
    {
        // Divide the difference of 100 and intelligence by 1/10th of your sight to get the error in enemy strength guess
        // for example 50 intelligence and sight will result in 50 / (50/10) or error margin of 10

        // dividing by 10 is basically a constant, should be tweaked later
        float errorMargin = (100 - Abils.GetModifiedIntelligence()) / (Abils.GetModifiedSight() / 10);
        float combatScore = Abils.GetCombatScore();
        float enemyCombatScore = enemy.Abils.GetCombatScore();
        float estimatedEnemyCombatScore = (float)GD.RandRange(enemyCombatScore - errorMargin, enemyCombatScore + errorMargin);

        return (estimatedEnemyCombatScore < combatScore);
    }

    public void Fight(Creature enemy)
    {
        if (MainObj.IsNullOrQueued(enemy) || MainObj.IsNullOrQueued(this)) // enemy already dead
        {
            return;
        }

        if (!this.WantsToFight(enemy)) // You don't want to fight the enemy
        {
            // this is what happens if their estimated strength is greater than our strength

            Boolean escaped = TryEscape(this, enemy);
            if (!escaped)
            {
                KillLoser(enemy); // fight occurs
            }

            // if speed > opponent speed
            //  ez dub get away
            // else run number
            //  greater than threshold -> getaway
            //  less than threshold -> check if opponent wants to fight/chase
            // if false or small chance you escape anyways -> getaway
            // else (true) -> fight
        }
        else
        {
            // we think we can take them since supposedly higher strength
            Boolean escaped = TryEscape(enemy, this);
            if (!escaped)
            {
                KillLoser(enemy); // fight occurs
            }
        }
    }

    public bool TryEscape(Creature escaper, Creature fighter)
    // Can the first creature escape from the second
    {
        if (escaper.Abils.GetModifiedSpeed() > fighter.Abils.GetModifiedSpeed() || !fighter.WantsToFight(escaper) || GD.Randf() < 0.1f)
        // exact speed is greater, or enemy doesnt want to fight, or random small chance to escape
        {
            if (escaper.DesiredFood != null)
            {
                escaper.Blacklist.Add(escaper.DesiredFood);
                escaper.DesiredFood.CurrentSeekers.Remove(escaper);
                escaper.DesiredFood = null;
                escaper.EatingTimeLeft = 0;
            }
            return true;
        }
        else return false;
    }

    public void KillLoser(Creature enemy)
    {
        // killing mechanic
        Creature loser = GetLoser(enemy);
        Creature winner = (loser == enemy ? this : enemy);
        winner.Kills++;
        winner.TeamObj.TotalKills++;
        winner.Abils.WonFight(winner.Abils.GetCombatScore(), loser.Abils.GetCombatScore());
        MainObj.CreatureDeath(loser);
    }

    public Creature GetLoser(Creature enemy)
    {
        // returns the loser of a fight
        Creature killedCreature = this;
        if (enemy.Abils.GetCombatScore() < Abils.GetCombatScore())   // defender has the slight edge in equal cases
        {
            killedCreature = enemy;
        }
        return killedCreature;
    }

    public void OnFoodDetectorInputEvent(object camera, object @event, Vector3 position, Vector3 normal, int shape_idx)
    {
        if (@event is InputEventMouseButton buttonEvent && buttonEvent.Pressed && (ButtonList)buttonEvent.ButtonIndex == ButtonList.Left && buttonEvent.Doubleclick)
        {
            MainObj.SelectCreature(this);
        }
    }

    public Creature GetNearestMate() // TODO: This doesnt work as intended sometimes, two blobs dont choose each other despite being close enough
    {
        if (!MainObj.IsNullOrQueued(Mate)) // This shouldnt happen
        {
            GD.Print("Called GetNearestMate() to look for new mate but Mate is not null or queued");
            LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
            return Mate;
        }

        List<Creature> visibleTeamMembers = MainObj.GetAllTeamMembersInSight(this);
        Creature closestMate = null;
        float closestDistance = 1000000;

        foreach (Creature teamMember in visibleTeamMembers)
        {
            if (MainObj.IsNullOrQueued(teamMember) || !teamMember.CanMate() || !MainObj.IsNullOrQueued(teamMember.Mate)) continue;

            float distance = Translation.DistanceSquaredTo(teamMember.Translation);
            if (distance < closestDistance && distance < Math.Pow(teamMember.Abils.GetModifiedSight(), 2))
            {
                closestMate = teamMember;
                closestDistance = distance;
            }
        }

        return closestMate;
    }

    public void LookAtClosestFood()
    {
        if (!MainObj.IsNullOrQueued(DesiredFood)) return;

        List<Food> visibleFood = MainObj.GetAllFoodInSight(this);

        float shortestTime = 1000000;
        Food closestFood = null;

        foreach (Food food in visibleFood)
        {
            if (Blacklist.Contains(food)) continue;

            float distance = Translation.DistanceSquaredTo(food.Translation);
            float timeToFood = distance / Abils.GetModifiedSpeed();

            if (timeToFood < shortestTime)
            {
                List<Creature> seekers = food.CurrentSeekers;
                Boolean isAllyCloser = false;
                foreach (Creature seeker in seekers)
                {
                    if (!MainObj.IsNullOrQueued(seeker) && seeker.TeamObj == TeamObj)
                    {
                        float timeAlly = (seeker.Translation.DistanceSquaredTo(food.Translation)) / seeker.Abils.GetModifiedSpeed();
                        if (timeToFood > timeAlly || seeker.EatingTimeLeft > 0) // if u r eating already, u are "closer"
                        {
                            isAllyCloser = true;
                        }
                    }
                }

                if (!isAllyCloser)
                {
                    shortestTime = timeToFood;
                    closestFood = food;
                }
            }
        }

        if (closestFood != null)
        {
            List<Creature> seekers = closestFood.CurrentSeekers;
            foreach (Creature ally in seekers)
            {
                if (!MainObj.IsNullOrQueued(ally) && ally.TeamObj == TeamObj)
                {
                    ally.DesiredFood = null;
                    seekers.Remove(ally);    // concurrent modification exception just isnt a thing apparently
                    break;
                }
            }

            LookAtFromPosition(Translation, closestFood.Translation, Vector3.Up);
            DesiredFood = closestFood;
            if (!closestFood.CurrentSeekers.Contains(this))
            {
                closestFood.CurrentSeekers.Add(this);
            }
        }
        else
        {
            // What to do if no available food within a blobs sight distance
        }
    }


    public void LookAtClosestWater()
    {
        if (DesiredWater != Vector3.Zero)
        {
            LookAtFromPosition(Translation, DesiredWater, Vector3.Up);
        }

        List<Vector3> allWater = MainObj.GetWaterLocations();
        Vector3 closestWater = Vector3.Zero;
        float closestDistance = 1000000;
        foreach (Vector3 water in allWater)
        {
            float sightSquared = Mathf.Pow(this.Abils.GetModifiedSight(), 2);
            float distance = water.DistanceSquaredTo(this.Translation);
            if (distance <= sightSquared && distance < closestDistance)
            {
                closestWater = water;
                closestDistance = distance;
            }
        }

        if (closestWater == Vector3.Zero) // TODO: this is a very hacky workaround b/c Vector3 is a nonnullable type
        {
            closestWater.y = this.Translation.y; // make it so they keep looking forward instead of throwing themselves up or down
            DesiredWater = closestWater;
            LookAtFromPosition(Translation, DesiredWater, Vector3.Up);
        }
        else
        {
            // do nothing if cant find any water
        }
        
    }

    public void Eat(float delta)    // Assert that food better exist
    {
        Debug.Assert(DesiredFood != null);
        Abils.Energy += (DesiredFood.Replenishment * (DesiredFood.Poisonous ? -1 : 1) * delta) / Abils.EatingTime;
        Abils.Energy = Math.Min(Abils.Energy, Abils.ENERGY_MAX); // Energy capped at 150
    }

    public void Drink(float delta)
    {
        Abils.Hydration += WATER_REPLENISHMENT * delta;
    }
}