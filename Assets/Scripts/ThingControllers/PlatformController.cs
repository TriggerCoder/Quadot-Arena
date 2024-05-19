using Godot;
using System.Collections.Generic;

public partial class PlatformController : AnimatableBody3D, Crusher
{
	private float lenght;
	private float speed;

	private Vector3 startPosition;
	private Vector3 dirVector;

	public MultiAudioStream audioStream;
	private float deltaTime;
	private bool isCrusher = false;
	private List<PlayerThing> playersToCrush = new List<PlayerThing>();
	public void Init(Vector3 dir, float sp, float phase, int height, bool crush, Vector3 origin, string noise)
	{
		dirVector = dir;

		lenght = height * GameManager.sizeDividor;
		speed = sp;

		deltaTime = 1000 * sp * phase;

		startPosition = Position;
		
		audioStream = new MultiAudioStream();
		AddChild(audioStream);
		audioStream.Bus = "BKGBus";
		audioStream.Position = origin;
		if (!string.IsNullOrEmpty(noise))
		{
			audioStream.Stream = SoundManager.LoadSound(noise, true);
			audioStream.Play();
		}
		isCrusher = crush;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GameManager.Paused)
			return;

		if (isCrusher && playersToCrush.Count > 0)
		{
			for (int i = 0; i < playersToCrush.Count; i++)
				playersToCrush[i].Damage(1000, DamageType.Crusher);
			playersToCrush.Clear();
		}

		deltaTime += (float)delta;
		float newDistance = Mathf.Sin(2 * Mathf.Pi * deltaTime / speed) * lenght;
		Vector3 newPosition = startPosition + dirVector * newDistance;
		Position = newPosition;
	}
	public void Crush(PlayerThing player)
	{
		if (!isCrusher)
			return;

		//Crush on main thread
		if (!playersToCrush.Contains(player))
			playersToCrush.Add(player);
	}
}
