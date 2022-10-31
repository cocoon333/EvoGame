using Godot;
using System;

public class Main : Node
{
#pragma warning disable 649
    // We assign this in the editor, so we don't need the warning about not being assigned.
    [Export]
    public PackedScene FoodScene;

    [Export]
    public PackedScene CreatureScene;
#pragma warning restore 649

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnFood();
        }

        for (int i = 0; i < 3; i++)
        {
            SpawnCreature();
        }
    }

    public void SpawnCreature()
    {
        Creature creature = (Creature)CreatureScene.Instance();
        Node creatureParent = GetNode<Node>("CreatureParent");
        creatureParent.AddChild(creature);

        Abilities abils = (Abilities)creature.GetNode<Node>("Abilities");
        abils.Initialize(50, 10, 10, 10, 50, 50, 10, 10);

        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-45, 45), 1.6f, (float)GD.RandRange(-45, 45));
        creature.Initialize(spawnLoc);
    }

    public void SpawnFood()
    {
        Food food = (Food)FoodScene.Instance();
        Node foodParent = GetNode<Node>("FoodParent");
        foodParent.AddChild(food);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-45, 45), 1.6f, (float)GD.RandRange(-45, 45));
        food.Initialize(25, false, spawnLoc);
    }

    public Food GetNearestFoodLocation(Creature blob)
    {
        Node foodParent = GetNode<Node>("FoodParent");
        int foodCount = foodParent.GetChildCount();
        float closestDistance = 1000000;
        Food closestFood = null;
        for (int i = 0; i < foodCount; i++)
        {
            Food current = (Food)foodParent.GetChild(i);


            if (current.CurrentSeeker != null && !current.IsQueuedForDeletion())
            {
                Creature blob2 = current.CurrentSeeker;
                if (blob2.DesiredFood != current) GD.Print("terrible error has occurred ", blob2.DesiredFood, " ", current);
            }

            if (IsNullOrQueued(current) || current.BeingAte ||
                (!IsNullOrQueued(current.CurrentSeeker) && current.CurrentSeeker != blob)) continue;


            float distance = current.Translation.DistanceTo(blob.Translation);
            if (distance < closestDistance && distance < blob.Abils.GetSight())
            {
                closestDistance = distance;
                closestFood = current;
            }
        }
        return closestFood;
    }

    public Boolean IsNullOrQueued(Node node)
    {
        return (node == null || node.IsQueuedForDeletion());
    }

    public override void _Process(float delta)
    {
        Camera cam = GetNode<Camera>("CameraPivot/Camera");
        Vector3 direction = cam.Translation;

        if (Input.IsActionPressed("move_forward"))
        {
            direction.z -= 1;
        }
        if (Input.IsActionPressed("move_back"))
        {
            direction.z += 1;
        }
        if (Input.IsActionPressed("move_left"))
        {
            direction.x -= 1;
        }
        if (Input.IsActionPressed("move_right"))
        {
            direction.x += 1;
        }
        if (Input.IsActionPressed("ascend"))
        {
            direction.y -= 1;
        }
        if (Input.IsActionPressed("descend"))
        {
            direction.y += 1;
        }
        cam.Translation = direction;

        if (Input.IsActionPressed("rotate_up"))
        {
            cam.RotateX((float)(-(Math.PI / 180.0)));
        }
        if (Input.IsActionPressed("rotate_down"))
        {
            cam.RotateX((float)(Math.PI / 180.0));
        }
        if (Input.IsActionPressed("rotate_right"))
        {
            cam.RotateY((float)(-(Math.PI / 180.0)));
        }
        if (Input.IsActionPressed("rotate_left"))
        {
            cam.RotateY((float)(Math.PI / 180.0));
        }
        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnFood();
        }
        if (Input.IsActionJustPressed("spawn_blob"))
        {
            SpawnCreature();
        }
    }
}
