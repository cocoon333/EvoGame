using Godot;
using System;

public class Creature : KinematicBody
{
    public Abilities Abils;
    float EatingTimeLeft;
    public Food DesiredFood;

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
            Abils.Energy -= (Abils.EnergyLoss * delta);
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            SpatialMaterial material = (SpatialMaterial)meshInst.GetActiveMaterial(0);
            Color color = material.AlbedoColor;
            color.g = Abils.Energy / 100f;
            color.b = (100 - Abils.Energy) / 100f;
            material.AlbedoColor = color;
        }
        else
        {
            // blob is dead
            if (DesiredFood != null) DesiredFood.CurrentSeeker = null;
            DesiredFood.BeingAte = false;
            
            QueueFree();
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

            LookAtFood();

            for (int i = 0; i < GetSlideCount(); i++)
            {
                KinematicCollision collision = GetSlideCollision(i);
                if (!((collision.Collider is StaticBody sb && sb.IsInGroup("ground")) || (collision.Collider is Creature creat && creat != this)))
                {
                    if (DesiredFood != null) GD.Print(DesiredFood.Translation, " ", EatingTimeLeft);
                    //LookAtFood();
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

                Node parent = GetParent();
                Main main = (Main)parent.GetParent();
                main.SpawnFood();
            }
        }
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

        Node parent = GetParent();
        Main main = (Main)parent.GetParent();
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
            RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));  // right now just have them turn randomly
        }

        //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
    }

    public void Eat(float delta)    // Assert that food better exist
    {
        if (DesiredFood == null) GD.Print(EatingTimeLeft);
        Abils.Energy += (DesiredFood.Replenishment * (DesiredFood.Poisonous ? -1 : 1) * delta)/Abils.EatingTime;
        Abils.Energy = Math.Min(Abils.Energy, 100); // Energy capped at 100
    }
}