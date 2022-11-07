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

    public override void _Process(float delta)
    {
        if (Abils.Energy > 0) // temporary so energy isnt negative and screwing with color
        {
            Color color = material.AlbedoColor;
            if (CanMate())
            {
                color.r = 1;
                color.g = 0;
                color.b = 0.5f;
            }
            else
            {
                color.r = 0;
                color.g = Abils.Energy / 100f;
                color.b = (100 - Abils.Energy) / 100f;
            }
            material.AlbedoColor = color;
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
            _velocity = Vector3.Forward * Abils.GetModifiedSpeed();
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

                if (Mate == null)
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
                if (!((collision.Collider is StaticBody sb && sb.IsInGroup("ground")) || (collision.Collider is Creature creat && creat != this)))
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

            // TODO: insert fight code here

            // Divide the difference of 100 and intelligence by 1/10th of your sight to get the error in enemy strength guess
            // for example 50 intelligence and sight will result in 50 / (50/10) or error margin of 10

            // dividing by 10 is basically a constant, should be tweaked later
            float errorMargin = (100 - Abils.GetModifiedIntelligence()) / (Abils.GetModifiedSight() / 10);
            float strength = Abils.GetModifiedStrength();
            float enemyStrength = enemy.Abils.GetModifiedStrength();
            float estimatedEnemyStrength = (float)GD.RandRange(enemyStrength - errorMargin, enemyStrength + errorMargin);

            // right now there is no functionality for a blob to not fight even if they estimate they are stronger, this may be a change to make later
            if (estimatedEnemyStrength > strength)
            {
                // what should happen if estimated strength is greater than our strength
                // potentially check differences in speed and run away but should enemy speed be estimated in the same way as enemy strength
                // and what if speed is less than enemy speed, do we always try and brave it in battle if both stats are against us or do we always try and run or mix of both

                if (Abils.GetModifiedSpeed() < enemy.Abils.GetModifiedSpeed()) // currently this uses their exact speed ( not an estimation )
                {
                    blacklist.Add(DesiredFood);
                    DesiredFood.CurrentSeekers.Remove(this);
                    DesiredFood = null;
                    EatingTimeLeft = 0;
                }
                else    // what to do if we are both weaker and slower ?
                {
                    // This is just placeholder code for something to happen
                    // if we are weaker, we run no matter if slower or faster

                    blacklist.Add(DesiredFood);
                    DesiredFood.CurrentSeekers.Remove(this);
                    DesiredFood = null;
                    EatingTimeLeft = 0;
                }
            }
            else
            {
                // we think we can take them since supposedly higher strength
                // cue fighting

                Creature killedCreature = this;
                if (enemyStrength < strength)   // defender has the slight edge in equal cases
                {
                    killedCreature = enemy;
                }
                main.CreatureDeath(killedCreature);
                return;
            }
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