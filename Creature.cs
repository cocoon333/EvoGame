using Godot;
using System;

public class Creature : KinematicBody
{
    public Abilities Abils;
    float EatingTimeLeft;
    public Food DesiredFood;

    public int FallAcceleration = 75;

    public Creature Mate;

    public int Team;

    private Vector3 _velocity = Vector3.Zero;

    Main main;

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

        MeshInstance hat1 = GetNode<MeshInstance>("Hat1");
        SpatialMaterial material1 = (SpatialMaterial)hat1.GetActiveMaterial(0);
        Color color1 = material1.AlbedoColor;

        MeshInstance hat2 = GetNode<MeshInstance>("Hat2");
        SpatialMaterial material2 = (SpatialMaterial)hat2.GetActiveMaterial(0);
        Color color2 = material2.AlbedoColor;

        if (team == 1)
        {
            color1.r = color1.g = color1.b = color2.r = color2.g = color2.b = 1;
        }

        material1.AlbedoColor = color1;
        material2.AlbedoColor = color2;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Abils.Energy > 0) // temporary so energy isnt negative and screwing with color
        {
            Abils.Energy -= (Abils.EnergyLoss * delta);
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            SpatialMaterial material = (SpatialMaterial)meshInst.GetActiveMaterial(0);
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
        else
        {
            // blob is dead


            main.CreatureDeath(this);
            return;
        }

        if (EatingTimeLeft <= 0)
        {
            _velocity = Vector3.Forward * Abils.GetSpeed();
            _velocity = _velocity.Rotated(Vector3.Up, Rotation.y);

            var direction = Vector3.Zero;

            // Vertical velocity
            _velocity.y -= FallAcceleration * delta;

            _velocity = MoveAndSlide(_velocity);

            //Mating = CanMate(); // assumes that mating going from true -> false doesnt have to free any variables

            if (CanMate())
            {
                if (DesiredFood != null)
                {
                    DesiredFood.BeingAte = false;
                    DesiredFood.CurrentSeeker = null;
                    DesiredFood = null;
                }

                if (Mate == null)
                {
                    Creature creature = main.GetNearestMatingCreature(this);

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
                LookAtFood();
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
                DesiredFood.QueueFree();
                DesiredFood = null;
                main.EatFood();
                main.SpawnFood();
            }
        }
    }

    public Boolean CanMate()
    {
        if (Mate != null) return true;
        if (DesiredFood != null) return false;

        float libido = Abils.GetLibido();
        float energy = Abils.GetEnergy();

        if (energy < (150 - libido)) return false; //TODO: curves needed

        return true;
    }

    public void OnFoodDetectorBodyEntered(Node body)
    {
        if (!(body is Food food) || food != DesiredFood) return;
        food.BeingAte = true;
        EatingTimeLeft = Abils.EatingTime;
    }

    public void LookAtFood()
    {
        if (DesiredFood != null && !DesiredFood.IsQueuedForDeletion() && !DesiredFood.BeingAte) return;

        //Vector3 loc = main.GetNearestFoodLocation(this);
        Food food = main.GetNearestFoodLocation(this);
        if (food != null)
        {
            LookAtFromPosition(Translation, food.Translation, Vector3.Up);
            DesiredFood = food;
            food.CurrentSeeker = this;
        }
        else
        {

            // What to do if no available food within a blobs sight distance
            //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));  // right now just have them turn randomly
        }

        //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
    }

    public void Eat(float delta)    // Assert that food better exist
    {
        if (DesiredFood == null) GD.Print(EatingTimeLeft);
        Abils.Energy += (DesiredFood.Replenishment * (DesiredFood.Poisonous ? -1 : 1) * delta) / Abils.EatingTime;
        Abils.Energy = Math.Min(Abils.Energy, 150); // Energy capped at 100
    }
}