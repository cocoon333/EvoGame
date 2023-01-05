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
    //Vector3 RotationAxis;

    const int WATER_REPLENISHMENT = 10;
    const float WATER_MOVEMENT_SPEED = 0.5f;

    Main MainObj;
    List<Food> Blacklist = new List<Food>();

    Water DesiredWater = null;
    public Boolean Selected = false;

    public enum StatesEnum
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
    public StatesEnum State = StatesEnum.Nothing;

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
        Vector3 colorVector;

        if (State is StatesEnum.LookingForMate || State is StatesEnum.PathingToMate)
        {
            colorVector = new Vector3(1.0f, 0.0f, 0.5f);
        }
        else if (State is StatesEnum.Drinking)
        {
            colorVector = new Vector3(1.0f, 1.0f, 1.0f);
        }
        else if (Selected)
        {
            colorVector = new Vector3(1.0f, (170.0f / 256.0f), (29.0f / 256.0f));
        }
        else
        {
            colorVector = new Vector3(0f, Abils.GetEnergy() / 100.0f, (100.0f - Abils.GetEnergy()) / 100.0f);
        }
        shader.SetShaderParam("colorVector", colorVector);
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
            Debug.Assert(DesiredFood != null);
            EatingTimeLeft -= delta;
            Eat(delta);
            if (EatingTimeLeft <= 0)
            {
                EatingTimeLeft = 0;
                MainObj.EatFood(DesiredFood);
                // State = StatesEnum.Nothing; // this happens in the above method automatically
            }
        }
        else if (State is StatesEnum.Drinking)
        {
            // Creature is drinking
            // replenish hydration and stop drinking if over hydration max
            Debug.Assert(DesiredWater != null);
            if (!MainObj.IsInDrinkableWater(Translation, "first"))
            {
                DesiredWater = null;
                State = StatesEnum.Nothing;
            }
            else
            {
                Drink(delta);
                if (Abils.GetHydration() >= Abils.HYDRATION_MAX || Abils.GetHydration() > Abils.GetSaturation())
                {
                    DesiredWater = null;
                    State = StatesEnum.Nothing;
                }
            }
        }
        else
        {
            float yVel = _velocity.y;
            //_velocity = Vector3.Forward * Abils.GetModifiedSpeed() / 2;

            // negative cuz forward is on negative z axis in godot (look at Vector3.Forward)
            _velocity = (-this.Transform.basis.z) * Abils.GetModifiedSpeed() / 2;
            if (MainObj.IsInWater(Translation))
            {
                _velocity *= WATER_MOVEMENT_SPEED;
            }
            //_velocity = _velocity.Rotated(Vector3.Up, Rotation.y);

            _velocity.y = yVel;
            _velocity.y -= FallAcceleration * delta;
            Debug.Assert(_velocity.y > -10000); // makes sure velocity isnt snowballing off the charts
            _velocity = MoveAndSlide(_velocity);

            //Vector3 lookDir = (Translation - prevLocation); // Notably unnormalized
            
            // negative cuz forward is on negative z axis in godot (look at Vector3.Forward)
            Vector3 localForward = -this.Transform.basis.z; // not guaranteed to be normalized

            if (!localForward.IsEqualApprox(Vector3.Zero))
            {
                Transform transform = this.Transform;
                transform.basis.y = GetRotationVector(localForward);
                this.Transform = transform;
                
                //RotationAxis = GetRotationVector(lookDir);
            }
            else
            {
                GD.Print(State);
                GD.Print("The creature is in the previous state and lookdir is the zero vector");
            }

            if (State is StatesEnum.PathingToMate)
            {
                if (MainObj.IsNullOrQueued(Mate))
                {
                    Mate = null;
                    State = StatesEnum.Nothing;
                }
                else
                {
                    //LookAtFromPosition(Translation, Mate.Translation, RotationAxis);
                    //Mate.LookAtFromPosition(Mate.Translation, Translation, Mate.RotationAxis);
                    LookAt(Mate.Translation, this.Transform.basis.y.Normalized());
                    Mate.LookAt(Translation, Mate.Transform.basis.y.Normalized());

                    if (Translation.DistanceSquaredTo(Mate.Translation) < 9)
                    {
                        Mate.Abils.SetSaturation(Mate.Abils.GetSaturation() - 50);
                        Mate.Abils.SetHydration(Mate.Abils.GetHydration() - 50);
                        Abils.SetSaturation(Abils.GetSaturation() - 50);
                        Abils.SetHydration(Abils.GetHydration() - 50);

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
                else if (MainObj.IsInDrinkableWater(Translation, "second") && Translation.DistanceSquaredTo(DesiredWater.Location) < 4.1)
                {
                    State = StatesEnum.Drinking;
                    //StartDrinkingWater(); // at some point, if it is warranted, wrap all this code into a StartDrinkingWater() method

                    Vector3 nextLocation = Translation + localForward; // Imagine your next location if you kept walking, not 100% accurate but accurate enough
                    nextLocation.y = Translation.y;
                    LookAt(nextLocation, this.Transform.basis.y.Normalized());
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
                Node node = (Node)collision.Collider;
                if (!node.IsInGroup("ground"))
                {
                    if (!(collision.Collider is Creature creat && creat != this))
                    {
                        if (State is StatesEnum.PathingToFood)
                        {
                            // // LookAtFromPosition(Translation, DesiredFood.Translation, RotationAxis);
                            // LookAt(DesiredFood.Translation, RotationAxis);
                        }
                        else if (State is StatesEnum.PathingToWater)
                        {
                            // // LookAtFromPosition(Translation, DesiredWater.Location, RotationAxis);
                            // LookAt(DesiredWater.Location, RotationAxis);
                        }
                        else if (State is StatesEnum.PathingToMate)
                        {
                            // // LookAtFromPosition(Translation, Mate.Translation, RotationAxis);
                            // // Mate.LookAtFromPosition(Mate.Translation, Translation, Mate.RotationAxis);
                            // LookAt(Mate.Translation, RotationAxis);
                            // Mate.LookAt(Translation, Mate.RotationAxis);
                        }
                        else
                        {
                            RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
                        }
                        break;
                    }
                    else if (collision.Collider is Creature && creat.TeamObj != TeamObj)
                    {
                        //TryFight(creat);
                    }
                }
            }
        }
    }

    public Vector3 GetRotationVector(Vector3 lookDir)
    {
        Vector3 rotationAxis = new Vector3(lookDir.z, 0, (-lookDir.x));

        // this still fails the dot product assertion but no idea what to default it to, need to fix the source of this bug
        if (rotationAxis.IsEqualApprox(Vector3.Zero))
        {
            rotationAxis = Vector3.Up;
            GD.Print("Vector3 lookDir in GetRotationVector was the zero vector, can't normalize this");
        }
        else rotationAxis = rotationAxis.Normalized();

        Vector3 rotatedVector = lookDir.Rotated(rotationAxis, -Mathf.Pi / 2);
        Debug.Assert(Mathf.IsEqualApprox(rotatedVector.Dot(lookDir), 0));

        return rotatedVector;
    }

    // TODO: Sort this method out and organize all its conditions
    // somewhat sorted but still kinda sucks tbh
    public Boolean CanMate()
    {
        float libido = Abils.GetModifiedLibido();
        float energy = Abils.GetEnergy();

        if (Mate != null) return true;
        if (!(State is StatesEnum.Nothing || State is StatesEnum.LookingForMate || State is StatesEnum.PathingToMate)) return false;

        // TODO: make energy libido relationship curved
        if (TimeAlive > 20 && energy > (150 - libido)) return true;
        return false;
    }

    public void StartEatingFood()
    {
        Debug.Assert(DesiredFood != null);
        //if (DesiredFood == null) GD.Print("Started eating food but desired food was null");

        Creature enemy = null;
        if (DesiredFood.BeingAte)
        {
            foreach (Creature creature in DesiredFood.CurrentSeekers)
            {
                if (creature != this)
                {
                    Debug.Assert(creature.TeamObj != this.TeamObj); // TODO: This failed once, check on this (two teams present)
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
            TryFight(enemy);
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

    public void TryFight(Creature enemy)
    {
        if (MainObj.IsNullOrQueued(enemy) || MainObj.IsNullOrQueued(this)) // enemy already dead
        {
            return;
        }

        Boolean wantsToFight = this.WantsToFight(enemy);
        if (!wantsToFight) // You don't want to fight the enemy
        {
            // this is what happens if their estimated strength is greater than our strength

            Boolean escaped = TryEscape(this, enemy, wantsToFight);
            if (!escaped)
            {
                Fight(enemy); // fight occurs
            }
        }
        else
        {
            // we think we can take them since supposedly higher strength
            Boolean escaped = TryEscape(enemy, this, enemy.WantsToFight(this));
            if (!escaped)
            {
                Fight(enemy); // fight occurs
            }
        }
    }

    public bool TryEscape(Creature escaper, Creature fighter, Boolean wantsToFight)
    // Can the first creature escape from the second
    {
        if (escaper.Abils.GetModifiedSpeed() > fighter.Abils.GetModifiedSpeed() || !wantsToFight || GD.Randf() < 0.1f)
        // exact speed is greater, or enemy doesnt want to fight, or random small chance to escape
        {
            if (escaper.DesiredFood != null)
            {
                escaper.Blacklist.Add(escaper.DesiredFood);
                escaper.DesiredFood.CurrentSeekers.Remove(escaper);
                escaper.DesiredFood = null;
                escaper.EatingTimeLeft = 0;
                escaper.State = StatesEnum.Nothing;
            }
            return true;
        }
        else return false;
    }

    public void Fight(Creature enemy)
    {
        Creature loser = GetLoser(enemy);
        Creature winner = GetWinner(enemy);
        float combatScoreDiff = winner.Abils.GetCombatScore() - loser.Abils.GetCombatScore();
        if (GD.Randf() < ((5 * combatScoreDiff) / 100.0f))
        {
            KillLoser(GetWinner(enemy), GetLoser(enemy));
        }
        else
        {
            if (loser.DesiredFood != null)
            {
                loser.Blacklist.Add(DesiredFood);
            }
            loser.DesiredFood = null;
            loser.DesiredWater = null;
            loser.State = StatesEnum.Nothing;

            // TODO: Creatures should lose energy if they draw
            // not sure how 
        }
    }

    public void KillLoser(Creature winner, Creature loser)
    {
        loser.State = StatesEnum.Nothing;
        winner.Kills++;
        winner.TeamObj.TotalKills++;
        winner.Abils.WonFight(winner.Abils.GetCombatScore(), loser.Abils.GetCombatScore());
        MainObj.CreatureDeath(loser);
        loser.TeamObj.FightDeaths++;
    }

    public Creature GetLoser(Creature enemy)
    {
        // returns the loser of a fight
        return (enemy.Abils.GetCombatScore() < Abils.GetCombatScore() ? enemy : this);
    }

    public Creature GetWinner(Creature enemy)
    {
        return (GetLoser(enemy) == this ? enemy : this);
    }


    public Creature GetNearestMate()
    {
        if (!MainObj.IsNullOrQueued(Mate)) // TODO: This shouldnt happen but has happened, fix this
        {
            GD.Print("Called GetNearestMate() to look for new mate but Mate is not null or queued");
            //LookAtFromPosition(Translation, Mate.Translation, RotationAxis);
            LookAt(Mate.Translation, this.Transform.basis.y.Normalized());
            return Mate;
        }

        List<Creature> visibleTeamMembers = MainObj.GetAllTeamMembersInSight(this);
        Creature closestMate = null;
        float closestDistance = 1000000;

        foreach (Creature teamMember in visibleTeamMembers)
        {
            if (MainObj.IsNullOrQueued(teamMember) || !(teamMember.State is StatesEnum.LookingForMate)) continue;

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
            if (!Translation.IsEqualApprox(DesiredFood.Translation))
            {
                //LookAtFromPosition(Translation, DesiredFood.Translation, RotationAxis);
                LookAt(DesiredFood.Translation, this.Transform.basis.y.Normalized());
            }
            return;
        }

        List<Food> visibleFood = MainObj.GetAllFoodInSight(this);

        float shortestTime = 1000000;
        Food closestFood = null;

        foreach (Food food in visibleFood)
        {
            if (Blacklist.Contains(food)) continue;

            if (food.Poisonous)
            {
                float sightFraction = 1 - (Translation.DistanceTo(food.Translation) / Abils.GetModifiedSight());
                float avoidChance = (Abils.GetModifiedIntelligence() * sightFraction) / 100.0f;
                if (GD.Randf() <= avoidChance)
                {
                    Blacklist.Add(food);
                    continue;
                }
            }

            float timeToFood = CalculateTimeToLocation(food.Translation);

            if (timeToFood < shortestTime)
            {
                List<Creature> seekers = food.CurrentSeekers;
                Boolean isAllyCloser = false;
                foreach (Creature seeker in seekers)
                {
                    if (!MainObj.IsNullOrQueued(seeker) && seeker.TeamObj == TeamObj)
                    {
                        //float timeAlly = (seeker.Translation.DistanceSquaredTo(food.Translation)) / seeker.Abils.GetModifiedSpeed();
                        float timeAlly = seeker.CalculateTimeToLocation(food.Translation);
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

            DesiredFood = closestFood;
            //LookAtFromPosition(Translation, closestFood.Translation, RotationAxis);
            LookAt(DesiredFood.Translation, this.Transform.basis.y.Normalized());

            if (!DesiredFood.CurrentSeekers.Contains(this))
            {
                DesiredFood.CurrentSeekers.Add(this);
            }
            else // this creature is already in desired food's current seekers list
            {
                GD.Print("Did full search to find desired food but was already in desired food's current seekers list");
            }
        }
        else
        {
            // What to do if no available food within a blobs sight distance
        }
    }

    public float CalculateTimeToLocation(Vector3 target)
    {
        Vector3 directedUnitVector = (target - Translation).Normalized();
        float distance = Translation.DistanceTo(target);
        float weightedDistance = 0;
        //float increment = 5; // sample every 5 distance units along line
        float increment = distance / 10; // 10 sample locations
        for (float i = 0; i <= distance; i += increment)
        {
            Vector3 sampleLocation = Translation + i * directedUnitVector;
            Boolean isWater = MainObj.IsInWater(target);
            weightedDistance += increment;
            if (isWater)
            {
                weightedDistance += (1 / WATER_MOVEMENT_SPEED) - increment;
            }
        }
        float time = (weightedDistance / Abils.GetModifiedSpeed());
        return time;
    }

    public void LookAtClosestWater()
    {
        if (DesiredWater != null && MainObj.IsInDrinkableWater(DesiredWater.Location, "third"))
        {
            //if (DesiredWater.Location.y != Translation.y) DesiredWater.Location.y = Translation.y;

            if (!Translation.IsEqualApprox(DesiredWater.Location))
            {
                //LookAtFromPosition(Translation, DesiredWater.Location, RotationAxis);
                Vector3 lookAtVector = DesiredWater.Location;
                CapsuleShape capsule = (CapsuleShape)GetNode<CollisionShape>("CollisionShape").Shape;
                lookAtVector.y += capsule.Height;
                LookAt(lookAtVector, this.Transform.basis.y.Normalized());
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

                if (x < 5 || x > Main.MAP_SIZE - 5 || z < 5 || z > Main.MAP_SIZE - 5) continue; // -5 to all values to avoid edges

                Tuple<bool, float> drinkabilityAndHeight = MainObj.IsInDrinkableWaterAndGetHeight(new Vector3(x, 0, z));
                if (drinkabilityAndHeight.Item1)
                {
                    Vector3 waterLocation = new Vector3(x, 0, z); // y is MainObj.WaterLevel to ensure blobs stay level while drinking
                    if (waterLocation.DistanceSquaredTo(Translation) <= Mathf.Pow(Abils.GetModifiedSight(), 2))
                    {
                        waterLocation.y = drinkabilityAndHeight.Item2;
                        waterFound = true;
                        closestWater = new Water(waterLocation);
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
            DesiredWater = closestWater;
            if (!(Mathf.IsEqualApprox(DesiredWater.Location.x, Translation.x) && Mathf.IsEqualApprox(DesiredWater.Location.z, Translation.z)))
            {
                //LookAtFromPosition(Translation, DesiredWater.Location, RotationAxis);
                Vector3 lookAtVector = DesiredWater.Location;
                CapsuleShape capsule = (CapsuleShape)GetNode<CollisionShape>("CollisionShape").Shape;
                lookAtVector.y += capsule.Height;
                LookAt(lookAtVector, this.Transform.basis.y.Normalized());
            }
        }
        else
        {
            // do nothing if cant find any water
        }
    }

    public void Eat(float delta)    // Assert that food better exist
    {
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