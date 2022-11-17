using Godot;
using System;
using System.Collections.Generic;

public class Creature : KinematicBody
{
    public Abilities Abils;
    public float EatingTimeLeft;
    public Food DesiredFood;

    public int FallAcceleration = 75;

    public Creature Mate;

    public int Team;
    public float TimeAlive;

    private Vector3 _velocity = Vector3.Zero;

    Main main;
    SpatialMaterial material;
    List<Food> blacklist = new List<Food>();

    public Boolean Selected = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Randomize();
    }

    public void Initialize(Vector3 spawnLoc, int team)
    {
        Translation = spawnLoc;
        Abils = GetNode<Abilities>("Abilities");
        Team = team;

        Node parent = GetParent();
        main = (Main)parent.GetParent();

        MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
        material = (SpatialMaterial)meshInst.GetActiveMaterial(0);

        if (team == 1)
        {
            MeshInstance hat1 = GetNode<MeshInstance>("Hat1");
            SpatialMaterial material1 = (SpatialMaterial)hat1.GetActiveMaterial(0);
            Color color1 = material1.AlbedoColor;

            MeshInstance hat2 = GetNode<MeshInstance>("Hat2");
            SpatialMaterial material2 = (SpatialMaterial)hat2.GetActiveMaterial(0);
            Color color2 = material2.AlbedoColor;
            color1.r = color1.g = color1.b = color2.r = color2.g = color2.b = 1;

            material1.AlbedoColor = color1;
            material2.AlbedoColor = color2;
        }
    }

    public void UpdateColor()
    {
        Color color = material.AlbedoColor;
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
        material.AlbedoColor = color;
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
        TimeAlive += delta;

        Abils.Energy -= (Abils.EnergyLoss * delta);

        if (Abils.Energy <= 0)
        {
            // TODO: make it so health depletes rapidly when energy is 0
            // blob is dead
            main.CreatureDeath(this);
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

                if (main.IsNullOrQueued(Mate))
                {
                    Creature creature = GetNearestMate();

                    if (creature != null)
                    {
                        Mate = creature;
                        creature.Mate = this;

                        LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                        Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);
                    }
                }
                else
                {
                    LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                    Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);

                    if (Translation.DistanceTo(Mate.Translation) < 3)
                    {
                        main.SpawnCreature(Translation, Team);
                        Mate.Abils.SetEnergy(Mate.Abils.GetEnergy() - 60);
                        Abils.SetEnergy(Abils.GetEnergy() - 60);
                        Mate.Mate = null;
                        Mate = null;
                    }
                }
            }
            else
            {
                LookAtClosestFood();
            }


            for (int i = 0; i < GetSlideCount(); i++)
            {
                KinematicCollision collision = GetSlideCollision(i);
                if (!(collision.Collider is StaticBody sb && sb.IsInGroup("ground")))
                {
                    if (!(collision.Collider is Creature creat && creat != this))
                    {
                        //if (DesiredFood != null) GD.Print(DesiredFood.Translation, " ", EatingTimeLeft);
                        if (DesiredFood != null)
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
                    else if (collision.Collider is Creature && creat.Team != Team)
                    {
                        Fight(creat);
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
                main.EatFood(DesiredFood);
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

    public void OnFoodDetectorBodyEntered(Node body)
    {
        if (!(body is Food food) || food != DesiredFood) return;

        Creature enemy = null;
        if (food.BeingAte)
        {
            foreach (Creature creature in food.CurrentSeekers)
            {
                if (creature != this)
                {
                    if (creature.Team == Team) { GD.Print("Friendly fire has occured"); }
                    enemy = creature;
                    break;
                }
            }
        }

        food.BeingAte = true;
        EatingTimeLeft = Abils.EatingTime;

        if (enemy != null)
        {

            // this means that the other seeker (the enemy) has already reached this food
            // this blob is the second to arrive to the food and can now determine whether or not a fight occurs

            Fight(enemy);
        }
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
        if (main.IsNullOrQueued(enemy) || main.IsNullOrQueued(this)) // enemy already dead
        {
            return;
        }

        // right now there is no functionality for a blob to not fight even if they estimate they are stronger, this may be a change to make later
        if (!WantsToFight(enemy))
        {
            // this is what happens if estimated strength is greater than our strength
            
            // exact speed is greater, or enemy doesnt want to fight, or random small chance to escape
            if (Abils.GetModifiedSpeed() < enemy.Abils.GetModifiedSpeed() || !enemy.WantsToFight(this) || GD.Randf() < 0.1f)
            {
                if (DesiredFood != null)
                {
                    blacklist.Add(DesiredFood);
                    DesiredFood.CurrentSeekers.Remove(this);
                    DesiredFood = null;
                    EatingTimeLeft = 0;
                }
            }
            else    // fight happen
            {
                main.CreatureDeath(GetLoser(enemy));
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
            // cue fighting

            main.CreatureDeath(GetLoser(enemy));
        }
    }

    public Creature GetLoser(Creature enemy)
    {
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
            // do stuff
            main.SelectCreature(this);
        }
    }

    public Creature GetNearestMate()
    {
        if (!main.IsNullOrQueued(Mate)) // This shouldnt happen
        {
            LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
            return Mate;
        }

        List<Creature> teamMembers = main.GetAllTeamMembersInSight(this);
        Creature closestMate = null;
        float closestDistance = 1000000;

        foreach (Creature teamMember in teamMembers)
        {
            if (main.IsNullOrQueued(teamMember) || !teamMember.CanMate() || !main.IsNullOrQueued(teamMember.Mate)) continue;

            float distance = Translation.DistanceTo(teamMember.Translation);
            if (distance < closestDistance && distance < teamMember.Abils.GetModifiedSight())
            {
                closestMate = teamMember;
                closestDistance = distance;
            }
        }

        return closestMate;
    }

    public void LookAtClosestFood()
    {
        if (!main.IsNullOrQueued(DesiredFood)) return;

        List<Food> allFood = main.GetAllFoodInSight(this);

        float shortestTime = 1000000;
        Food closestFood = null;

        foreach (Food food in allFood)
        {
            if (blacklist.Contains(food)) continue;

            float distance = Translation.DistanceTo(food.Translation);
            float timeToFood = distance / Abils.GetModifiedSpeed();

            if (timeToFood < shortestTime)
            {
                List<Creature> seekers = food.CurrentSeekers;
                Boolean isAllyCloser = false;
                foreach (Creature seeker in seekers)
                {
                    if (!main.IsNullOrQueued(seeker) && seeker.Team == Team)
                    {
                        float timeAlly = (seeker.Translation.DistanceTo(food.Translation)) / seeker.Abils.GetModifiedSpeed();
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
                if (!main.IsNullOrQueued(ally) && ally.Team == Team)
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

    /*
    public void LookAtFood()
    {
        if (DesiredFood != null && !DesiredFood.IsQueuedForDeletion() && !DesiredFood.BeingAte) return;

        //Vector3 loc = main.GetNearestFoodLocation(this);
        Food food = main.GetNearestFoodLocation(this);
        if (food != null)
        {
            LookAtFromPosition(Translation, food.Translation, Vector3.Up);
            DesiredFood = food;
            if (!food.CurrentSeekers.Contains(this))
            {
                food.CurrentSeekers.Add(this);
            }
        }
        else
        {

            // What to do if no available food within a blobs sight distance
            //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));  // right now just have them turn randomly
        }
    }
    */

    public void Eat(float delta)    // Assert that food better exist
    {
        if (DesiredFood == null) GD.Print(EatingTimeLeft);  // This should never happen
        Abils.Energy += (DesiredFood.Replenishment * (DesiredFood.Poisonous ? -1 : 1) * delta) / Abils.EatingTime;
        Abils.Energy = Math.Min(Abils.Energy, Abils.ENERGY_MAX); // Energy capped at 150
    }
}