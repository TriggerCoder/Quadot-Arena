using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class ScoreBoard : Node3D
{
	public static ScoreBoard Instance = null;
	[Export]
	public Node3D RootScore;
	[Export]
	public PackedScene playerScore;
	[Export]
	public Sprite3D medalImpressiveIcon;
	[Export]
	public Sprite3D medalGauntletIcon;
	[Export]
	public Sprite3D medalExcellentIcon;

	public List<PlayerScore> ScoreList = new List<PlayerScore>();
	public List<PlayerData> playerDatas = new List<PlayerData>();

	private static readonly string medalImpressive = "MENU/MEDALS/MEDAL_IMPRESSIVE";
	private static readonly string medalGauntlet = "MENU/MEDALS/MEDAL_GAUNTLET";
	private static readonly string medalExcellent = "MENU/MEDALS/MEDAL_EXCELLENT";
	//Fixed Medal Size
	private static int defaultMedalSize = 64;
	public override void _Ready()
	{
		Instance = this;
		medalImpressiveIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalImpressive, false, false);
		TextureLoader.AdjustIconSize(medalImpressiveIcon, defaultMedalSize);

		medalGauntletIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalGauntlet, false, false);
		TextureLoader.AdjustIconSize(medalGauntletIcon, defaultMedalSize);

		medalExcellentIcon.Texture = TextureLoader.GetTextureOrAddTexture(medalExcellent, false, false);
		TextureLoader.AdjustIconSize(medalExcellentIcon, defaultMedalSize);
	}

	public void AddPlayer(PlayerThing player)
	{
		PlayerScore PlayerScore = (PlayerScore)playerScore.Instantiate();
		PlayerScore.Position = Vector3.Down * ScoreList.Count * .22f;
		RootScore.AddChild(PlayerScore);
		ScoreList.Add(PlayerScore);
		PlayerData playerData = new PlayerData(player);
		playerData.Name = playerData.Name.Substring(0, Mathf.Min(14, playerData.Name.Length));
		playerDatas.Add(playerData);
		RefreshScore();
	}

	public void RemovePlayer(int playerNum)
	{
		int scoreCount = ScoreList.Count();
		ScoreList[scoreCount - 1].QueueFree();
		ScoreList.RemoveAt(scoreCount - 1);
		playerDatas.RemoveAt(playerNum);
		RefreshScore();
	}

	public void RefreshScore()
	{
		for (int i = 0; i < playerDatas.Count; i++)
		{
			playerDatas[i].Kills = playerDatas[i].player.frags;
			playerDatas[i].Deaths = playerDatas[i].player.deaths;
		}


		List<PlayerData> newData = playerDatas.OrderByDescending(x =>  x.Kills ).ToList();
		for (int i = 0; i < newData.Count; i++) 
		{
			ScoreList[i].PlayerName.Text = newData[i].Name;
			if (newData[i].Kills > 0)
				ScoreList[i].Kills.Text= "+" + newData[i].Kills;
			if (newData[i].Deaths > 0)
				ScoreList[i].Deaths.Text = "-" + newData[i].Deaths;
			ScoreList[i].Impressive.Text = "" + newData[i].Impressive;
			ScoreList[i].Gauntlet.Text = "" + newData[i].Gauntlet;
			ScoreList[i].Excellent.Text = "" + newData[i].Excellent;
		}
	}


	public class PlayerData
	{
		public PlayerThing player;
		public string Name = "Unnamed Player";
		public int Kills = 0;
		public int Deaths = 0;
		public int Impressive = 0;
		public int Gauntlet = 0;
		public int Excellent = 0;
		public int AvgLifeTime = 0;

		public PlayerData(PlayerThing player)
		{
			this.player = player;
			this.Name = player.playerName;
		}

	}
}
