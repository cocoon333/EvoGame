using Godot;
using System;
using System.Collections.Generic;

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
	Creature SelectedCreature = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		scoreLabel = GetNode<ScoreLabel>("ScoreLabel");
		Label label = GetNode<Label>("CreatureLabel");
		label.MarginRight = GetViewport().Size.x;

		for (int i = 0; i < 100; i++)
		{
			SpawnFood();
		}

		for (int i = 0; i < 100; i++)
		{
			SpawnCreature();
		}
	}
	
	public void SpawnCreature(Vector3 location, int team)
	{
		Creature creature = (Creature)CreatureScene.Instance();
		Node creatureParent = GetNode<Node>("CreatureParent");
		creatureParent.AddChild(creature);

		Abilities abils = (Abilities)creature.GetNode<Node>("Abilities");
		float stats = (float)GD.RandRange(45, 50);
		abils.Initialize(stats, stats, stats, stats, stats, stats, stats);

		creature.Initialize(location, team);

		scoreLabel.Text = string.Format(scoreLabel.DisplayString, ++scoreLabel.CreatureCount, scoreLabel.FoodCount);
	}

	public void CreatureDeath(Creature blob)
	{
		if (blob.DesiredFood != null)
		{
			blob.DesiredFood.CurrentSeekers.Remove(blob);
			//blob.DesiredFood.CurrentSeeker = null;
			blob.DesiredFood.BeingAte = false;
		}

		blob.QueueFree();

		scoreLabel.Text = string.Format(scoreLabel.DisplayString, --scoreLabel.CreatureCount, scoreLabel.FoodCount);
	}

	public void SpawnCreature()
	{
		Vector3 spawnLoc = new Vector3((float)GD.RandRange(-95, 95), 1.6f, (float)GD.RandRange(-95, 95));
		SpawnCreature(spawnLoc, (int)(GD.Randf() + 0.5)); // TODO: this is random, shouldnt be random
	}

	public void SpawnFood()
	{
		Food food = (Food)FoodScene.Instance();
		Node foodParent = GetNode<Node>("FoodParent");
		foodParent.AddChild(food);
		Vector3 spawnLoc = new Vector3((float)GD.RandRange(-95, 95), 1.6f, (float)GD.RandRange(-95, 95));
		food.Initialize(25, (GD.Randf() < 0.2), spawnLoc);

		scoreLabel.Text = string.Format(scoreLabel.DisplayString, scoreLabel.CreatureCount, ++scoreLabel.FoodCount);
	}

	public void EatFood(Food food)
	{
		List<Creature> seekers = food.CurrentSeekers;
		foreach (Creature seeker in seekers)
		{
			seeker.DesiredFood = null;
			seeker.EatingTimeLeft = 0;
		}

		SpawnFood();
		food.QueueFree();
		scoreLabel.Text = string.Format(scoreLabel.DisplayString, scoreLabel.CreatureCount, --scoreLabel.FoodCount);
	}

	public void SelectCreature(Creature creature)
	{
		creature.Selected = true;
		creature.UpdateColor();

		if (!IsNullOrQueued(SelectedCreature))
		{
			SelectedCreature.Selected = false;
			SelectedCreature.UpdateColor();

			if (SelectedCreature == creature)
			{
				creature = null;
			}

			SelectedCreature = null;
		}

		SelectedCreature = creature;
		UpdateCreatureLabel(creature);
	}

	public void UpdateCreatureLabel(Creature creature)
	{
		Label label = GetNode<Label>("CreatureLabel");

		if (IsNullOrQueued(creature))
		{
			label.Text = "";
			return;
		}

		Dictionary<String, float> abils = creature.Abils.GetAllAbils();
		String labelText = "";
		foreach (KeyValuePair<String, float> kvp in abils)
		{
			// This seems like a cleaner way to do it but need to figure out how to make it work
			// String appendString = ("{0}: {1}", kvp.Key, kvp.Value);
			// labelText += appendString;

			labelText += (kvp.Key + ": " + Mathf.RoundToInt(kvp.Value) + "\n");
		}
		label.Text = labelText;
	}

	/*
	public Food GetNearestFoodLocation(Creature blob)
	{
		Node foodParent = GetNode<Node>("FoodParent");
		int foodCount = foodParent.GetChildCount();
		float shortestTime = 1000000;
		Food closestFood = null;
		List<Creature> seekers;
		for (int i = 0; i < foodCount; i++)
		{
			Food current = (Food)foodParent.GetChild(i);

			if (IsNullOrQueued(current)) continue;  // maybe try and steal currently being ate food later

			float distance = current.Translation.DistanceTo(blob.Translation);
			float timeToFood = distance / (blob.Abils.GetModifiedSpeed());
			if (timeToFood < shortestTime && distance < blob.Abils.GetModifiedSight())
			{
				seekers = current.CurrentSeekers;
				Boolean isAllyCloser = false;
				foreach (Creature seeker in seekers)
				{
					if (!IsNullOrQueued(seeker) && seeker.Team == blob.Team)
					{
						float timeTo = distance / blob.Abils.GetModifiedSpeed();
						float timeAlly = (seeker.Translation.DistanceTo(current.Translation)) / seeker.Abils.GetModifiedSpeed();
						if (timeTo > timeAlly || seeker.EatingTimeLeft > 0) // if u r eating already, u are "closer"
						{
							isAllyCloser = true;
						}
					}
				}

				if (!isAllyCloser)
				{
					shortestTime = timeToFood;
					closestFood = current;
				}
			}
		}

		if (closestFood == null) return closestFood;

		seekers = closestFood.CurrentSeekers;
		foreach (Creature seeker2 in seekers)
		{
			if (!IsNullOrQueued(seeker2) && seeker2.Team == blob.Team)
			{
				seeker2.DesiredFood = null;
				seekers.Remove(seeker2);    // concurrent modification exception just isnt a thing apparently
				break;
			}
		}

		return closestFood;
	}

	public Creature GetNearestMatingCreature(Creature blob)
	{
		Node creatureParent = GetNode<Node>("CreatureParent");
		int creatureCount = creatureParent.GetChildCount();
		float closestDistance = 1000000;
		Creature creatureMate = null;
		for (int i = 0; i < creatureCount; i++)
		{
			Creature current = (Creature)creatureParent.GetChild(i); // possible possible race condition but we will probably definitely ignore this forever
																	 // yes they do
																	 // race conditions dont exist
			if (IsNullOrQueued(current) || current == blob || current.Team != blob.Team || current.Mate != null || !current.CanMate())
			{
				continue;
			}

			float dist = blob.Translation.DistanceTo(current.Translation);
			// right now both blobs need to be within each others sight distance, maybe change this so only one of them needs to be within sight
			if (dist < closestDistance && dist < blob.Abils.GetModifiedSight() && dist < current.Abils.GetModifiedSight())
			{
				creatureMate = current;
				closestDistance = dist;
			}
		}
		return creatureMate;
	}
	*/

	public List<Food> GetAllFoodInSight(Creature creature)
	{
		Node foodParent = GetNode<Node>("FoodParent");
		int foodCount = foodParent.GetChildCount();
		List<Food> allFood = new List<Food>();
		for (int i = 0; i < foodCount; i++)
		{
			Food current = (Food)foodParent.GetChild(i);

			if (!IsNullOrQueued(current) && current.Translation.DistanceTo(creature.Translation) < creature.Abils.GetModifiedSight())
			{
				allFood.Add(current);
			}
		}
		return allFood;
	}

	public List<Creature> GetAllCreaturesInSight(Creature creature)
	{
		Node creatureParent = GetNode<Node>("CreatureParent");
		int creatureCount = creatureParent.GetChildCount();
		List<Creature> allCreatures = new List<Creature>();
		for (int i = 0; i < creatureCount; i++)
		{
			Creature current = (Creature)creatureParent.GetChild(i);

			if (!IsNullOrQueued(current) && current.Translation.DistanceTo(creature.Translation) < creature.Abils.GetModifiedSight() && current != creature)
			{
				allCreatures.Add(current);
			}
		}
		return allCreatures;
	}

	public List<Creature> GetAllTeamMembersInSight(Creature creature)
	{
		List<Creature> allCreatures = GetAllCreaturesInSight(creature);
		List<Creature> teamMembers = new List<Creature>();
		foreach (Creature teamMember in allCreatures)
		{
			if (teamMember.Team == creature.Team)
			{
				teamMembers.Add(teamMember);
			}
		}
		return teamMembers;
	}

	public Boolean IsNullOrQueued(Node node)
	{
		Boolean retValue = false;
		try
		{
			retValue = (node == null || node.IsQueuedForDeletion());
		}
		catch(ObjectDisposedException)  // the method to check if something is deleted crashes cuz turns out it was deleted
		{
			retValue = true;
		}
		return retValue;
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
					Input.MouseMode = Input.MouseModeEnum.Captured;
					Dragging = true;
				}
				// stop dragging if right click released
				if (Dragging && !mouseEvent.Pressed)
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
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
		UpdateCreatureLabel(SelectedCreature);
		
		if (Input.IsActionJustPressed("spawn_food"))
		{
			SpawnFood();
		}
		if (Input.IsActionJustPressed("spawn_blob"))
		{
			SpawnCreature();
		}

		if (Input.IsActionJustPressed("pause_game"))
		{
			GetTree().Paused = !GetTree().Paused;
		}

		if (Input.IsActionPressed("exit_game"))
		{
			GetTree().Quit();
		}
	}
}
