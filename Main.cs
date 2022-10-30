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
        for (int i = 0; i < 10; i++)
        {
            SpawnFood();
        }

        for(int i = 0; i < 3; i++)
        {
            SpawnCreature();
        }
    }

    public void SpawnCreature()
    {
        Creature creature = (Creature)CreatureScene.Instance();
        Node creatureParent = GetNode<Node>("CreatureParent");
        creatureParent.AddChild(creature);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-48, 48), 1.6f, (float)GD.RandRange(-48, 48));
        creature.Initialize(spawnLoc);
    }

    public void SpawnFood()
    {
        Food food = (Food)FoodScene.Instance();
        Node foodParent = GetNode<Node>("FoodParent");
        foodParent.AddChild(food);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-48, 48), 1.6f, (float)GD.RandRange(-48, 48));
        food.Initialize(20, false, spawnLoc);
    }

    public Vector3 GetNearestFoodLocation(KinematicBody blob)
    {
        Node foodParent = GetNode<Node>("FoodParent");
        int foodCount = foodParent.GetChildCount();
        float closestDistance = 1000000;
        Food closestFood = null;
        for (int i = 0; i < foodCount; i++)
        {
            Food current = (Food)foodParent.GetChild(i);
            if (current.Eaten) continue;
            float distance = current.Translation.DistanceTo(blob.Translation);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFood = current;
            }
        }

        return closestFood.Translation;
    }

    public override void _Process(float delta)
    {
        Camera cam = GetNode<Camera>("CameraPivot/Camera");
        Vector3 direction = cam.Translation;

        if(Input.IsActionPressed("move_forward"))
        {
            direction.z += 1;
        }
        if(Input.IsActionPressed("move_back"))
        {
            direction.z -= 1;
        }
        if(Input.IsActionPressed("move_left"))
        {
            direction.x += 1;
        }
        if(Input.IsActionPressed("move_right"))
        {
            direction.x -= 1;
        }
        if(Input.IsActionPressed("ascend"))
        {
            direction.y -= 1;
        }
        if(Input.IsActionPressed("descend"))
        {
            direction.y += 1;
        }
        cam.Translation = direction;

        if(Input.IsActionPressed("rotate_up"))
        {
            cam.RotateX((float)(-(Math.PI/180.0)));
        }
        if(Input.IsActionPressed("rotate_down"))
        {
            cam.RotateX((float)(Math.PI/180.0));
        }
        if(Input.IsActionPressed("rotate_right"))
        {
            cam.RotateY((float)(-(Math.PI/180.0)));
        }
        if(Input.IsActionPressed("rotate_left"))
        {
            cam.RotateY((float)(Math.PI/180.0));
        }
        if(Input.IsActionJustPressed("spawn_food"))
        {
            SpawnFood();
        }
        if(Input.IsActionJustPressed("spawn_blob"))
        {
            SpawnCreature();
        }
    }
}
