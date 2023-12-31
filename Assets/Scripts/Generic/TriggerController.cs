using Godot;
using System;
public partial class TriggerController : Node3D
{
	public string triggerName = "";
	public bool activated = false;
	private Action<PlayerThing> OnActivate = new Action<PlayerThing>((p) => { return; });

	public bool Repeatable = false;
	public bool AutoReturn = false;
	public float AutoReturnTime = 1f;

	public Func<bool> PreReq = new Func<bool>(() => { return true; });

	public float time = 0f;

	public void SetController(string name, Action<PlayerThing> activeAction)
	{
		triggerName = name;
		OnActivate = activeAction;
	}
	public bool Activate(PlayerThing playerThing)
	{
		if (!PreReq())
		{
			GameManager.Print("TriggerController: Prereq False for: " + triggerName, GameManager.PrintType.Info);
			return false;
		}

		if ((!Repeatable) || (AutoReturn))
			if (activated)
				return false;

		if (AutoReturn)
			time = AutoReturnTime;

		activated = !activated;
		OnActivate.Invoke(playerThing);

		return true;
	}
	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		float deltaTime = (float)delta;
		if (time <= 0)
			return;
		else
		{
			time -= deltaTime;
			if (time <= 0)
				activated = !activated;
		}
	}
	public void OnBodyEntered(Node3D other)
	{
		if (GameManager.Paused)
			return;

		GameManager.Print("Someone "+ other.Name + " activated this " + Name);
		if (other is PlayerThing player)
		{
			//Dead player don't activate stuff
			if (player.Dead)
				return;

			Activate(player);
		}
	}
}
