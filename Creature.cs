using Godot;
using System;

public class Creature : KinematicBody
{
    [Export]
    public int MoveSpeed = 50;

    [Export]
    public int FallAcceleration = 75;

    [Export]
    int Energy;

    private Vector3 _velocity = Vector3.Zero;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Randomize();
        _velocity = Vector3.Forward * MoveSpeed;
    }

    public void Initialize(Vector3 spawnLoc)
    {
        Energy = 0;
        Translation = spawnLoc;
    }

    public override void _PhysicsProcess(float delta)
    {
        _velocity = Vector3.Forward * MoveSpeed;
        _velocity = _velocity.Rotated(Vector3.Up, Rotation.y);

        var direction = Vector3.Zero;

        // Vertical velocity
        _velocity.y -= FallAcceleration * delta;

        _velocity = MoveAndSlide(_velocity);

        Turn();

        for (int i = 0; i < GetSlideCount(); i++)
        {
            KinematicCollision collision = GetSlideCollision(i);
            if (!(collision.Collider is StaticBody sb && sb.IsInGroup("ground")))
            {
                Turn();
                break;
            }
        }

        if (Energy > 0) // temporary so energy isnt negative and screwing with color
        {
            Energy -= 1;
            MeshInstance meshInst = GetNode<MeshInstance>("MeshInstance");
            SpatialMaterial material = (SpatialMaterial)meshInst.GetActiveMaterial(0);
            Color color = material.AlbedoColor;
            color.g = Energy / 100f;
            color.b = (100 - Energy) / 100f;
            material.AlbedoColor = color;
        }
    }

    public void OnFoodDetectorBodyEntered(Node body)
    {
        if (!(body is Food)) return;
        Food food = (Food)body;
        if (food.Eaten) return;
        food.Eaten = true;
        Eat(food.Replenishment);
        food.QueueFree();
        Node parent = GetParent();
        Main main = (Main)parent.GetParent();
        main.SpawnFood();
    }

    public void Turn()
    {
        Node parent = GetParent();
        Main main = (Main)parent.GetParent();
        Vector3 loc = main.GetNearestFoodLocation(this);
        LookAtFromPosition(Translation, loc, Vector3.Up);
        //RotateY((float)GD.RandRange(0, 2 * Mathf.Pi));
    }

    public void Eat(int replenishment)
    {
        Energy += replenishment;
        Energy = Math.Min(Energy, 100);
    }
}