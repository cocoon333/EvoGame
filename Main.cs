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

    public Boolean Dragging = false;

    ScoreLabel scoreLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        for (int i = 0; i < 3; i++)
        {
            SpawnFood();
        }

        for (int i = 0; i < 3; i++)
        {
            SpawnCreature();
        }

        scoreLabel = GetNode<ScoreLabel>("ScoreLabel");
    }

    public void SpawnCreature(Vector3 location)
    {
        Creature creature = (Creature)CreatureScene.Instance();
        Node creatureParent = GetNode<Node>("CreatureParent");
        creatureParent.AddChild(creature);

        Abilities abils = (Abilities)creature.GetNode<Node>("Abilities");
        abils.Initialize(50, 10, 10, 10, 50, 50, 10, 10);

        creature.Initialize(location);

        scoreLabel.Text = string.Format(scoreLabel.DisplayString, (++scoreLabel.CreatureCount, scoreLabel.FoodCount));
    }

    public void CreatureDeath(Creature blob)
    {
        if (blob.DesiredFood != null)
        {
            blob.DesiredFood.CurrentSeeker = null;
            blob.DesiredFood.BeingAte = false;
        }

        blob.QueueFree();

        scoreLabel.Text = string.Format(scoreLabel.DisplayString, (--scoreLabel.CreatureCount, scoreLabel.FoodCount));
    }

    public void SpawnCreature()
    {
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-45, 45), 1.6f, (float)GD.RandRange(-45, 45));
        SpawnCreature(spawnLoc);
    }

    public void SpawnFood()
    {
        Food food = (Food)FoodScene.Instance();
        Node foodParent = GetNode<Node>("FoodParent");
        foodParent.AddChild(food);
        Vector3 spawnLoc = new Vector3((float)GD.RandRange(-45, 45), 1.6f, (float)GD.RandRange(-45, 45));
        food.Initialize(25, (GD.Randf() < 0.2), spawnLoc);

        scoreLabel.Text = string.Format(scoreLabel.DisplayString, (scoreLabel.CreatureCount, ++scoreLabel.FoodCount));
    }

    public void EatFood()
    {
        scoreLabel.Text = string.Format(scoreLabel.DisplayString, (scoreLabel.CreatureCount, --scoreLabel.FoodCount));
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

            // testing code below
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

    // TODO: this sucks probably change
    public Creature GetNearestMatingCreature(Creature blob)
    {
        // if (blob.Mate != null)
        // {
        //     blob.Mate.LookAtFromPosition(blob.Mate.Translation, blob.Translation, Vector3.Up);
        //     blob.LookAtFromPosition(blob.Translation, blob.Mate.Translation, Vector3.Up);
        //     return blob.Mate;
        // }
        Node creatureParent = GetNode<Node>("CreatureParent");
        int creatureCount = creatureParent.GetChildCount();
        for (int i = 0; i < creatureCount; i++)
        {
            Creature current = (Creature)creatureParent.GetChild(i); // possible possible race condition but we will probably definitely ignore this forever
                                                                     // yes they do
                                                                     // race conditions dont exist
            if (IsNullOrQueued(current) || current == blob || current.Mate != null || !current.CanMate())
            {
                return null;
            }

            //current.Mate = blob;
            //blob.Mate = current;

            //current.LookAtFromPosition(current.Translation, blob.Translation, Vector3.Up);
            //blob.LookAtFromPosition(blob.Translation, current.Translation, Vector3.Up);
            return current;
        }
        return null;
    }

    public Boolean IsNullOrQueued(Node node)
    {
        return (node == null || node.IsQueuedForDeletion());
    }

    public override void _UnhandledInput(InputEvent @event) // weird architecture
    {
        //base._UnhandledInput(@event);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.Right)
            {
                // Start dragging if right click pressed
                if (!Dragging && mouseEvent.Pressed)
                {
                    Dragging = true;
                }
                // stop dragging if right click released
                if (Dragging && !mouseEvent.Pressed)
                {
                    Dragging = false;
                }
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelUp)
            {
                Camera cam = GetNode<Camera>("CameraPivot/Camera");
                Vector3 direction = cam.Translation;
                direction.z -= 2;
                if (direction.z <= 5) direction.z = 5;
                cam.Translation = direction;
            }
            else if ((ButtonList)mouseEvent.ButtonIndex == ButtonList.WheelDown)
            {
                Camera cam = GetNode<Camera>("CameraPivot/Camera");
                Vector3 direction = cam.Translation;
                direction.z += 2;
                cam.Translation = direction;
            }
        }
        else if (@event is InputEventMouseMotion motionEvent && Dragging)
        {
            Vector2 relative = motionEvent.Relative;
            Position3D camPivot = GetNode<Position3D>("CameraPivot");
            Camera cam = GetNode<Camera>("CameraPivot/Camera");
            camPivot.RotateY(relative.x / (-1000));
            cam.RotateX(relative.y / (-1000));
            Vector3 rotation = cam.GlobalRotation;
            rotation.x = Math.Max(rotation.x, -1.5f);
            rotation.x = Math.Min(rotation.x, 0.5f);
            cam.GlobalRotation = rotation;
        }
    }

    public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("spawn_food"))
        {
            SpawnFood();
        }
        if (Input.IsActionJustPressed("spawn_blob"))
        {
            SpawnCreature();
        }

        if (Input.IsActionPressed("exit_game"))
        {
            GetTree().Quit();
        }
    }
}
