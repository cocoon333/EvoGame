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

    public Team TeamObj;

    public float TimeAlive;

    public int NumChildren;
    public int Kills;

    private Vector3 _velocity = Vector3.Zero;

    const int WATER_REPLENISHMENT = 20;

    Main MainObj;
    List<Food> Blacklist = new List<Food>();

    //public Vector3 DesiredWater = Vector3.Zero;

    Water DesiredWater = null;
    public Boolean Selected = false;

    enum StatesEnum
    {
        Eating,
        Drinking,
        PathingToFood, // these 4 names feel pretty verbose, should change at some point
        PathingToWater,
        LookingForMate,
        PathingToMate,
        Nothing, // dont like this name for doing nothing, should change at some point
        Fighting // potentially also add a Fleeing state alongside Fighting
    }
    StatesEnum State = StatesEnum.Nothing;

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

        MeshInstance hat1 = GetNode<MeshInstance>("Hat1");
        MeshInstance hat2 = GetNode<MeshInstance>("Hat2");
        hat1.MaterialOverride = TeamObj.TeamColor;
        hat2.MaterialOverride = TeamObj.TeamColor;

    }

    public void UpdateColor()
    {
        MeshInstance meshInst = GetNode<MeshInstance>("BodyMesh");
        ShaderMaterial shader = (ShaderMaterial)meshInst.GetActiveMaterial(0);
        shader.SetShaderParam("energy", Abils.GetEnergy());
        int state = 0;
        if (Selected) state = 3;
        else if (State is StatesEnum.LookingForMate || State is StatesEnum.PathingToMate) state = 1;
        else if (State is StatesEnum.Drinking) state = 2;
        shader.SetShaderParam("state", state);
    }

    public override void _Process(float delta)
    {
        if (Abils.GetEnergy() > 0) // temporary so energy isnt negative and screwing with color
        {
            UpdateColor();
        }
    }

    public void OnCreatureInputEvent(object camera, object @event, Vector3 position, Vector3 normal, int shape_idx)
    {
        if (@event is InputEventMouseButton buttonEvent && buttonEvent.Pressed && (ButtonList)buttonEvent.ButtonIndex == ButtonList.Left && buttonEvent.Doubleclick)
        {
            MainObj.SelectCreature(this);
        }
    }

    public override void _PhysicsProcess(float delta)
    {

        if (IsQueuedForDeletion())
        {
            return;
        }

        TimeAlive += delta;

        Abils.SetSaturation(Abils.GetSaturation() - Abils.GetSaturationLoss() * delta);
        Abils.SetHydration(Abils.GetHydration() - Abils.GetHydrationLoss() * delta);

        if (Abils.GetSaturation() <= 0 || Abils.GetHydration() <= 0)
        {
            // TODO: make it so health depletes rapidly when energy is 0
            // blob is dead, thanks who guessed
            if (Abils.GetSaturation() < 0) TeamObj.StarvationDeaths++;
            else TeamObj.DehydrationDeaths++;
            MainObj.CreatureDeath(this);
            return;
        }

        if (State is StatesEnum.Eating)
        {
            // Creature is eating
            // Decrement eating time, replenish energy, and then consume food if finished
            EatingTimeLeft -= delta;
            Eat(delta);
            if (EatingTimeLeft <= 0)
            {
                EatingTimeLeft = 0;
                MainObj.EatFood(DesiredFood);
                State = StatesEnum.Nothing;
                if (CanMate())
                {
                    State = StatesEnum.LookingForMate;
                }
            }
        }
        else if (State is StatesEnum.Drinking)
        {
            // Creature is drinking
            // replenish hydration and stop drinking if over hydration max
            Drink(delta);
            if (Abils.GetHydration() >= Abils.HYDRATION_MAX) // TODO: Define a hydration max
            {
                DesiredWater = null;
                State = StatesEnum.Nothing;
                if (CanMate())
                {
                    State = StatesEnum.LookingForMate;
                }
            }
        }
        else
        {
            _velocity = Vector3.Forward * Abils.GetModifiedSpeed() / 2;
            if (MainObj.IsInWater(Translation, true))
            {
                _velocity *= 0.5f;
            }
            _velocity = _velocity.Rotated(Vector3.Up, Rotation.y);
            // Vertical velocity
            _velocity.y -= FallAcceleration * delta;
            _velocity = MoveAndSlide(_velocity);

            if (State is StatesEnum.PathingToMate)
            {
                if (MainObj.IsNullOrQueued(Mate))
                {
                    Mate = null;
                    State = StatesEnum.LookingForMate; // if they cannot mate anymore, in the next if block CanMate() is called
                }
                else
                {
                    LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                    Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);

                    if (Translation.DistanceSquaredTo(Mate.Translation) < 9)
                    {
                        Mate.Abils.SetSaturation(Mate.Abils.GetSaturation() - 50);
                        Mate.Abils.SetHydration(Mate.Abils.GetHydration() - 50);
                        Abils.SetSaturation(Abils.GetSaturation() - 50);
                        Abils.SetHydration(Abils.GetHydration() - 50);

                        if (Abils.GetSaturation() < 0 || Abils.GetHydration() < 0)
                        {
                            GD.Print("Truly a bruh moment occurred, died by breeding");
                        }

                        NumChildren++;
                        Mate.NumChildren++;

                        Mate.State = StatesEnum.Nothing;
                        State = StatesEnum.Nothing;

                        Mate.Mate = null;
                        Mate = null;

                        MainObj.SpawnCreature(Translation, TeamObj);
                    }
                }
            }
            else if (State is StatesEnum.LookingForMate)
            {
                if (CanMate())
                {
                    if (DesiredFood != null) // this shouldnt happen ideally
                    {
                        DesiredFood.BeingAte = false;
                        DesiredFood.CurrentSeekers.Remove(this);
                        DesiredFood = null;
                    }

                    Mate = GetNearestMate();
                    if (Mate != null)
                    {
                        Mate.Mate = this;
                        State = StatesEnum.PathingToMate;
                        Mate.State = StatesEnum.PathingToMate;
                    }
                }
                else
                {
                    State = StatesEnum.Nothing;
                }
            }
            else if (State is StatesEnum.PathingToFood)
            {
                LookAtClosestFood();
                if (DesiredFood is null) // this shouldnt happen really
                {
                    State = StatesEnum.Nothing;
                }
                else if (Translation.DistanceSquaredTo(DesiredFood.Translation) < 4.1)
                {
                    State = StatesEnum.Eating;
                    StartEatingFood();
                }
            }
            else if (State is StatesEnum.PathingToWater)
            {
                LookAtClosestWater(); // keep this so it can continue recalculating to closer water
                if (DesiredWater is null) // this shouldnt happen pretty sure
                {
                    State = StatesEnum.Nothing;
                }
                else if (MainObj.IsInWater(Translation, false) && Translation.DistanceSquaredTo(DesiredWater.Location) < 4.1)
                {
                    State = StatesEnum.Drinking;
                }
            }
            else if (State is StatesEnum.Nothing)
            {
                if (CanMate())
                {
                    State = StatesEnum.LookingForMate;
                }
                else
                {
                    if ((Abils.GetSaturation() / Abils.GetSaturationLoss()) <= (Abils.GetHydration() / Abils.GetHydrationLoss()))
                    {
                        LookAtClosestFood();
                        if (DesiredFood != null) State = StatesEnum.PathingToFood;
                    }
                    else
                    {
                        LookAtClosestWater();
                        if (DesiredWater != null) State = StatesEnum.PathingToWater;
                    }
                }
            }

            for (int i = 0; i < GetSlideCount(); i++)
            {
                KinematicCollision collision = GetSlideCollision(i);
                if (!(collision.Collider is StaticBody sb && sb.IsInGroup("ground")))
                {
                    if (!(collision.Collider is Creature creat && creat != this))
                    {
                        if (State is StatesEnum.PathingToFood)
                        {
                            //LookAtFromPosition(Translation, DesiredFood.Translation, Vector3.Up);
                        }
                        else if (State is StatesEnum.PathingToWater)
                        {
                            //LookAtFromPosition(Translation, DesiredWater.Location, Vector3.Up);
                        }
                        else if (State is StatesEnum.PathingToMate)
                        {
                            //LookAtFromPosition(Translation, Mate.Translation, Vector3.Up);
                            //Mate.LookAtFromPosition(Mate.Translation, Translation, Vector3.Up);
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
        if (Mate != null || (DesiredFood == null && DesiredWater == null && TimeAlive > 20 && (energy > (150 - libido)))) canMate = true;

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
        // TODO: if creature is drinking same water with enemy go for a fight?
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
        loser.TeamObj.FightDeaths++;
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
            if (MainObj.IsNullOrQueued(teamMember) || !(State is StatesEnum.LookingForMate)) continue;

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
        if (!MainObj.IsNullOrQueued(DesiredFood))
        {
            if (Translation.IsEqualApprox(DesiredFood.Translation))
                LookAtFromPosition(Translation, DesiredFood.Translation, Vector3.Up);
            return;
        }

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
                    ally.State = StatesEnum.Nothing;
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
        if (DesiredWater != null)
        {
            if (!Translation.IsEqualApprox(DesiredWater.Location))
            {
                LookAtFromPosition(Translation, DesiredWater.Location, Vector3.Up);
            }
            return;
        }

        int distance = 0;
        Boolean waterFound = false;

        Water closestWater = null;
        while (!waterFound)
        {
            for (int i = 0; i < 8; i++)
            {
                // this is a mess of rules that covers all 8 directions but only makes sense if u expand them for each case
                // dont touch or everything might break
                // simplified it a bit, still sucks tho
                // kdtree coming soon to an indie game that might fail near you

                float x = Translation.x;
                float z = Translation.z;

                if (i < 3) x += distance;
                else if (i < 6) x -= distance;

                if (i != 0 && i != 5)
                {
                    if (i % 2 == 0) z -= distance;
                    else z += distance;
                }

                if (x > 100 || x < -100 || z < -100 || z > 100) continue;

                if (MainObj.IsInWater(new Vector3(x, Translation.y, z), false))
                {
                    Vector3 tempVector = new Vector3(x, Translation.y, z);
                    if (tempVector.DistanceSquaredTo(Translation) <= Mathf.Pow(Abils.GetModifiedSight(), 2))
                    {
                        waterFound = true;
                        closestWater = new Water(tempVector);
                        break;
                    }
                }
            }

            distance++;
            if (distance >= Abils.GetModifiedSight())
            {
                break;
            }
        }

        if (closestWater != null)
        {
            closestWater.Location.y = Translation.y;
            DesiredWater = closestWater;
            if (!Translation.IsEqualApprox(DesiredWater.Location))
            {
                LookAtFromPosition(Translation, DesiredWater.Location, Vector3.Up);
            }
        }
        else
        {
            // do nothing if cant find any water
        }
    }

    public void Eat(float delta)    // Assert that food better exist
    {
        Debug.Assert(DesiredFood != null);
        Abils.SetSaturation(Mathf.Min(Abils.GetSaturation() + (DesiredFood.Replenishment * (DesiredFood.Poisonous ? -1 : 1) * delta) / Abils.EatingTime, Abils.SATURATION_MAX));
    }

    public void Drink(float delta)
    {
        Abils.SetHydration(Math.Min(Abils.GetHydration() + WATER_REPLENISHMENT * delta, Abils.HYDRATION_MAX));
    }

    class Water
    {
        public Vector3 Location;
        public Water(Vector3 location)
        {
            Location = location;
        }
    }
}