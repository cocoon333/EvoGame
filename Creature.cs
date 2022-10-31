using Godot;
using System;

public class Creature : KinematicBody
{
    public Abilities Abils;
    float EatingTimeLeft;
    Food DesiredFood;

    public int FallAcceleration = 75;

    private Vector3 _velocity = Vector3.Zero;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Randomize();
    }

    public void Initialize(Vector3 spawnLoc)
    {
        Translation = spawnLoc;
        Abils = GetNode<Abilities>("Abilities");
        _velocity = Vector3.Forward * Abils.GetSpeed();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Abils.Energy > 0) // temporary so energy isnt negative and screwing with color
        {
            Abils.Energy -= 1;
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            SpatialMaterial material = (SpatialMaterial)meshInst.GetActiveMaterial(0);
            Color color = material.AlbedoColor;
            color.g = Abils.Energy / 100f;
            color.b = (100 - Abils.Energy) / 100f;
            material.AlbedoColor = color;
        }

        if (EatingTimeLeft <= 0)
        {
            _velocity = Vector3.Forward * Abils.GetSpeed();
            _velocity = _velocity.Rotated(Vector3.Up, Rotation.y);

            var direction = Vector3.Zero;

            // Vertical velocity
            _velocity.y -= FallAcceleration * delta;

            _velocity = MoveAndSlide(_velocity);

            LookAtFood();

            // for (int i = 0; i < GetSlideCount(); i++)
            // {
            //     KinematicCollision collision = GetSlideCollision(i);
            //     if (!(collision.Collider is StaticBody sb && sb.IsInGroup("ground")))
            //     {
            //         LookAtFood();
            //         break;
            //     }
            // }
        }
        else
        {
            EatingTimeLeft -= delta;
            Eat(DesiredFood.Replenishment * delta / Abils.EatingTime);
            if (EatingTimeLeft < 0)
            {
                DesiredFood.QueueFree();
                DesiredFood = null;

                Node parent = GetParent();
                Main main = (Main)parent.GetParent();
                main.SpawnFood();
            }
        }
    }

    public void OnFoodDetectorBodyEntered(Node body)
    {
        if (!(body is Food)) return;
        Food food = (Food)body;
        if (food.IsQueuedForDeletion() || food.Eating) return; // be careful of this
        food.Eating = true;
        DesiredFood = food;
        EatingTimeLeft = Abils.EatingTime;
        //food.QueueFree();
    }

    public void LookAtFood()
    {
        if (DesiredFood != null && !DesiredFood.Eating) return;

        Node parent = GetParent();
        Main main = (Main)parent.GetParent();
        //Vector3 loc = main.GetNearestFoodLocation(this);
        Food food = main.GetNearestFoodLocation(this);
        if (food != null)
        {
            LookAtFromPosition(Translation, food.Translation, Vector3.Up);
            food.CurrentSeeker = this;
        }
        else
        {
            // What to do if no available food within a blobs sight distance
            RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));  // right now just have them turn randomly
        }

        //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
    }

    public void Eat(float replenishment)
    {
        Abils.Energy += replenishment;
        Abils.Energy = Math.Min(Abils.Energy, 100);
    }
}