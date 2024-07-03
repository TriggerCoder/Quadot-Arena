using Godot;
using System;
using System.Linq;

public partial class SpriteController : Node3D
{
	[Export]
	public string spriteName = "";
	[Export]
	public float spriteRadius = 2;
	[Export]
	public Vector2 spriteSize = Vector2.Zero;
	[Export]
	public BaseMaterial3D.BillboardModeEnum billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
	[Export]
	public bool isTransparent = false;
	[Export]
	public bool castShadows = false;
	[Export]
	public MultiMeshType useMultiMesh = MultiMeshType.LowCount;
	[Export]
	public SpriteData spriteData;
	[Export]
	public Node3D referenceNode = null;

	public enum MultiMeshType
	{
		NoMultiMesh,
		LowCount,
		HighCount
	}
	private MeshProcessed sprite;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	public override void _Ready()
	{
		Init();
	}

	public void Init()
	{
		currentState = GameManager.FuncState.Ready;
		if (string.IsNullOrEmpty(spriteName))
			return;

		spriteName = spriteName.ToUpper();

		if (billboard != BaseMaterial3D.BillboardModeEnum.Disabled)
			MaterialManager.AddBillBoard(spriteName);

		if (!TextureLoader.HasTexture(spriteName))
			TextureLoader.AddNewTexture(spriteName, isTransparent);

		if (spriteSize.X == 0)
			spriteSize.X = spriteRadius;
		if (spriteSize.Y == 0)
			spriteSize.Y = spriteRadius;

		sprite = Mesher.GenerateSprite(spriteName + "_" + spriteRadius, spriteName, spriteSize.X, spriteSize.Y, GameManager.AllPlayerViewMask, castShadows, spriteData.destroyTimer, this, isTransparent, ((useMultiMesh != MultiMeshType.NoMultiMesh) && !isTransparent), (useMultiMesh == MultiMeshType.LowCount));

		spriteData.baseTime = spriteData.destroyTimer;
	}

	public void Start()
	{
		currentState = GameManager.FuncState.Start;
		if (string.IsNullOrEmpty(spriteName))
		{
			QueueFree();
			return;
		}

		if (sprite.data[0] == null)
			return;
		if (sprite.data[0].isTransparent)
			return;

		if (useMultiMesh == MultiMeshType.NoMultiMesh)
			return;

		if (Mesher.MultiMeshSprites.ContainsKey(sprite.data[0].multiMesh))
		{
			currentState = GameManager.FuncState.None;
			MultiMeshData multiMeshData = new MultiMeshData();
			multiMeshData.multiMesh = sprite.data[0].multiMesh;
			spriteData.GlobalTransform = GlobalTransform;
			spriteData.GlobalPosition = GlobalPosition;
			spriteData.GlobalBasis = GlobalBasis;
			spriteData.SetReferenceNode(referenceNode);
			spriteData.SetMultiMeshData(multiMeshData);

			Mesher.AddSpriteToMultiMeshes(sprite.data[0].multiMesh, spriteData, spriteData.Modulate);
			//As Node is going to be deleted, children need to be reparented
			var Parent = GetParent();
			spriteData.destroyNodes = GetChildren().ToList();
			foreach (var child in spriteData.destroyNodes)
				child.Reparent(Parent);
			QueueFree();
		}
	}

	public override void _Process(double delta)
	{
		if (GameManager.Paused)
			return;

		switch (currentState)
		{
			default:
				break;
			case GameManager.FuncState.Ready:
				Start();
			break;
			case GameManager.FuncState.Start:
				spriteData.Process((float)delta);
				if (spriteData.readyToDestroy)
					QueueFree();
			break;
		}
	}
}
