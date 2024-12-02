using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
public partial class ConsoleManager : Control
{
	[Export]
	public LineEdit commandLine;
	[Export]
	public RichTextLabel history;
	public int moveSpeed = 1000;

	private GameManager.FuncState currentState = GameManager.FuncState.None;
	private List<string> commandSubmited = new List<string>();

	public const int lineHeight = 23;
	public bool visible = false;
	private Vector2 tempPosition;
	private Vector2 tempSize;
	private int halfHeight;
	private int totalLines = 0;
	private int focusLine = 0;
	private int lastCommand = 0;
	public void Init()
	{
		tempPosition = DisplayServer.WindowGetSize();
		halfHeight = Mathf.CeilToInt(tempPosition.Y / 2);
		tempPosition = new Vector2(0, -halfHeight);

		tempSize = new Vector2(Size.X, 0);
		SetDeferred("Size", new Vector2(Size.X, halfHeight));
		history.CustomMinimumSize = tempSize;
		Position = tempPosition;
		commandLine.TextSubmitted += CommandSubmited;
	}

	public override void _Input(InputEvent @event)
	{
		if (!GameManager.Console.visible)
			return;

		if (@event is InputEventKey)
		{
			if (Input.IsActionJustPressed("Console_Prev"))
				RecallCommand(true);
			else if (Input.IsActionJustPressed("Console_Next"))
				RecallCommand(false);
		}

	}

	public void ChangeConsole(bool forceHide = false)
	{
		if (currentState != GameManager.FuncState.End)
			return;

		visible = !visible;
		if (visible)
		{
			focusLine = totalLines;
			tempSize.Y = focusLine * lineHeight;
			history.CustomMinimumSize = tempSize;
			commandLine.FocusMode = FocusModeEnum.All;
			Show();
		}
		else
		{
			if (forceHide)
				Hide();
			commandLine.FocusMode = FocusModeEnum.None;
		}

		currentState = GameManager.FuncState.Start;
	}

	public void CommandSubmited(string command)
	{
		if (string.IsNullOrEmpty(command))
			return;
		commandLine.Text = "";
		commandSubmited.Add(command);
		lastCommand++;
		ProcessCommand(command.ToUpper());
	}

	public void RecallCommand(bool previous)
	{
		if (commandSubmited.Count == 0)
			return;

		if (previous)
		{
			lastCommand--;
			if (lastCommand < 0)
				lastCommand = 0;
		}
		else
		{
			lastCommand++;
			if (lastCommand > commandSubmited.Count)
				lastCommand = commandSubmited.Count;
		}

		if (commandSubmited.Count > lastCommand)
		{
			commandLine.Text = commandSubmited[lastCommand];
			commandLine.CaretColumn = commandSubmited[lastCommand].Length;
		}
	}

	public void ChangeAutoHop(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string autohop = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				autohop = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (autohop == "TRUE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.AutoHop = true;
		else if (autohop == "FALSE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.AutoHop = false;
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + autohop + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeAutoSwap(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string autoSwap = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				autoSwap = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (autoSwap == "TRUE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.AutoSwap = true;
		else if (autoSwap == "FALSE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.AutoSwap = false;
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + autoSwap + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeBloodScreen(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string bloodscreen = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				bloodscreen = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (bloodscreen == "TRUE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.BloodScreen = true;
		else if (bloodscreen == "FALSE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.BloodScreen = false;
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + bloodscreen + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeColor(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing color to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string color = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				color = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		Color modulate = Colors.White;
		bool failed = false;
		try
		{
			modulate = new Color(color);
		}
		catch (Exception)
		{
			AddToConsole("Command: " + args[0] + " failed: " + color + " is an invalid color", GameManager.PrintType.Warning);
			failed = true;
		}
		if (!failed)
		{
			modulate.R = Mathf.Max(0.1f, modulate.R);
			modulate.G = Mathf.Max(0.1f, modulate.G);
			modulate.B = Mathf.Max(0.1f, modulate.B);
			GameManager.Instance.Players[playerNum].modulate = modulate;
			AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
			GameManager.Instance.Players[playerNum].playerInfo.configData.ModulateColor = color;
			GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
		}
	}

	public void ChangeCrossHair(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing weapon and crosshair type to change", GameManager.PrintType.Warning);
			return;
		}
		if (args.Length < 3)
		{
			AddToConsole("Command: " + command + " missing crosshair type to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string weapon = args[1];
		string crosshair = args[2];
		if (args.Length > 3)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				weapon = args[2];
				crosshair = args[3];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		int weaponNum = 0;
		if (weapon == "ALL")
			weaponNum = -1;
		else if (weapon == "GAUNTLET")
			weaponNum = 0;
		else if (weapon == "MACHINEGUN")
			weaponNum = 1;
		else if (weapon == "SHOTGUN")
			weaponNum = 2;
		else if ((weapon == "GRENADE") || (weapon == "GRENADELAUNCHER"))
			weaponNum = 3;
		else if ((weapon == "ROCKET") || (weapon == "ROCKETLAUNCHER"))
			weaponNum = 4;
		else if ((weapon == "LIGHTNING") || (weapon == "LIGHTNINGGUN"))
			weaponNum = 5;
		else if ((weapon == "RAIL") || (weapon == "RAILGUN"))
			weaponNum = 6;
		else if ((weapon == "PLASMA") || (weapon == "PLASMAGUN"))
			weaponNum = 7;
		else if ((weapon == "BFG") || (weapon == "BFG10K"))
			weaponNum = 8;
		else if ((weapon == "NAIL") || (weapon == "NAILGUN"))
			weaponNum = 10;
		else if ((weapon == "CHAIN") || (weapon == "CHAINGUN"))
			weaponNum = 11;
		else if ((weapon == "PROXIMITY") || (weapon == "PROXIMITYLAUNCHER"))
			weaponNum = 12;
		else if ((weapon == "HMG") || (weapon == "HEAVYMACHINEGUN"))
			weaponNum = 13;
		else
		{
			AddToConsole("Command: " + args[0] + " was not changed. Weapon " + weapon + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		string[] type = crosshair.Split('_');
		if (type.Length < 2)
		{
			AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
			return;
		}

		int CrossHair = 0;
		if (type[0] == "SMALL")
		{
			if (int.TryParse(type[1], out CrossHair))
			{
				if (CrossHair > 50)
				{
					AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
					return;
				}
			}
			else
			{
				AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
				return;
			}
		}
		else if (type[0] == "LARGE")
		{
			if (int.TryParse(type[1], out CrossHair))
			{
				if (CrossHair > 50)
				{
					AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
					return;
				}
				else
					CrossHair += 100;
			}
			else
			{
				AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
				return;
			}
		}
		else
		{
			AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
			return;
		}

		int[] CroosHairs = GameManager.Instance.Players[playerNum].playerInfo.configData.CroosHair;
		if (weaponNum < 0)
		{
			for (int i = 0; i < CroosHairs.Length; i++)
				CroosHairs[i] = CrossHair;
			GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHair(CrossHair);
		}
		else
		{
			CroosHairs[weaponNum] = CrossHair;
			if (weaponNum == GameManager.Instance.Players[playerNum].playerControls.CurrentWeapon)
				GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHair(CrossHair);
		}
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeCrossHairAlpha(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing alpha value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string alpha = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				alpha = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		bool failed = true;
		int Alpha = 0;
		if (int.TryParse(alpha, out Alpha))
			failed = false;

		if ((failed) || (Alpha < 0) || (Alpha > 100))
		{
			AddToConsole("Command: " + command + " alpha is not in correct format [b]0-100[/b]", GameManager.PrintType.Warning);
			return;
		}

		GameManager.Instance.Players[playerNum].playerInfo.configData.CroosHairAlpha = Alpha;
		GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHairAlpha(Alpha);
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeCrossHairScale(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string scale = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				scale = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		bool failed = true;
		int Scale = 0;
		if (int.TryParse(scale, out Scale))
			failed = false;

		if ((failed) || (Scale < 10) || (Scale > 100))
		{
			AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
			return;
		}
		GameManager.Instance.Players[playerNum].playerInfo.configData.CroosHairScale = Scale;
		GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHairScale(Scale);
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeFragLimit(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing frag to set", GameManager.PrintType.Warning);
			return;
		}
		if (int.TryParse(args[1], out int value))
		{
			if (value < 1)
			{
				AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " must be positive", GameManager.PrintType.Warning);
				return;
			}
			GameManager.Instance.ChangeFragLimit(value);
			AddToConsole("Command: " + args[0] + " sucesfully changed to " + args[1], GameManager.PrintType.Success);
		}
		else
			AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " is not an integer", GameManager.PrintType.Warning);
	}

	public void ChangeFOV(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string fov = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				fov = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		bool failed = true;
		int Fov = 0;
		if (int.TryParse(fov, out Fov))
			failed = false;

		if ((failed) || (Fov < 30) || (Fov > 130))
		{
			AddToConsole("Command: " + command + " FOV is not in correct format [b]30-130[/b]", GameManager.PrintType.Warning);
			return;
		}
		GameManager.Instance.Players[playerNum].playerInfo.playerCamera.ViewCamera.Fov = Fov;
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void GiveItem(string[] args, string command)
	{
		if (!MapLoader.UseCheats)
		{
			AddToConsole("Cheats are not enabled on this server", GameManager.PrintType.Warning);
			return;
		}

		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing item to give", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string item = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " failed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " failed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				item = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " failed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		item = item.ToLower();
		bool found = false;
		if (ThingsManager.itemName.TryGetValue(item, out string foundItem))
		{
			item = foundItem;
			found = true;
		}
		else if (!item.Contains('_'))
		{
			string[] checks = new string[5] { "weapon_", "ammo_", "item_", "item_armor_", "item_health_" };
			for (int i = 0; i < checks.Length; i++) 
			{
				string newItem = checks[i] + item;
				if (ThingsManager.pickablePrefabs.ContainsKey(newItem))
				{
					item = newItem;
					found = true;
					break;
				}
			}
		}
		else if (ThingsManager.pickablePrefabs.ContainsKey(item))
			found = true;

		if (found)
		{
			switch (GameManager.Instance.gameConfig.GameSelect)
			{
				default:
					break;
				case GameManager.BasePak.TeamArena:
					if (ThingsManager.teamArenaIgnoreItems.Any(s => s == item))
						found = false;
					break;
				case GameManager.BasePak.Quake3:
					if (ThingsManager.retailIgnoreItems.Any(s => s == item))
						found = false;
				break;
				case GameManager.BasePak.Demo:
					if (ThingsManager.demoIgnoreItems.Any(s => s == item))
						found = false;
				break;
			}
		}

		if (!found)
		{
			AddToConsole("Command: " + args[0] + " failed. Item " + item + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		ThingController thingObject = (ThingController)ThingsManager.pickablePrefabs[item].Instantiate();
		if (thingObject == null)
		{
			AddToConsole("Command: " + args[0] + " failed. Item " + item + " is invalid", GameManager.PrintType.Warning);
			return;
		}
		ItemPickup itemPickup = thingObject.itemPickup;
		if (itemPickup == null)
		{
			AddToConsole("Command: " + args[0] + " failed. Item " + item + " is invalid", GameManager.PrintType.Warning);
			thingObject.QueueFree();
			return;
		}
		thingObject.initDisabled = true;
		thingObject.SpawnCheck(item);
		GameManager.Instance.TemporaryObjectsHolder.AddChild(thingObject);
		thingObject.GlobalPosition = GameManager.Instance.Players[playerNum].GlobalPosition;
		thingObject.SetRespawnTime(-1);
		if (!itemPickup.PickUp(GameManager.Instance.Players[playerNum]))
			thingObject.QueueFree();
		AddToConsole("Command: " + args[0] + " executed sucesfully for Player " + playerNum, GameManager.PrintType.Success);
	}

	public void ChangeGodMode(string[] args, string command)
	{
		if (!MapLoader.UseCheats)
		{
			AddToConsole("Cheats are not enabled on this server", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " failed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " failed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " failed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}
		bool godMode = GameManager.Instance.Players[playerNum].playerInfo.godMode;
		GameManager.Instance.Players[playerNum].playerInfo.godMode = !godMode;
		if (godMode)
			AddToConsole("Command: " + args[0] + " OFF for Player " + playerNum, GameManager.PrintType.Success);
		else
			AddToConsole("Command: " + args[0] + " ON for Player " + playerNum, GameManager.PrintType.Success);
	}
	public void ShowCommands(string command)
	{
		AddToConsole("Command: " + command, GameManager.PrintType.Success);
		AddToConsole("The following is a list of commands:", GameManager.PrintType.Log);
		AddToConsole("AUTOHOP [i]0-7[/i] [b]true/false[/b] -> Set AutoHop [b]true/false[/b] for the [i]player[/i], default: [b]false[/b]", GameManager.PrintType.Log);
		AddToConsole("AUTOSWAP [i]0-7[/i] [b]true/false[/b] -> Automatically swap to new weapon when pickedup [b]true/false[/b] for the [i]player[/i], default: [b]true[/b]", GameManager.PrintType.Log);
		AddToConsole("BLOODSCREEN [i]0-7[/i] [b]true/false[/b] -> Set pain visual feedback [b]true/false[/b] for the [i]player[/i], default: [b]true[/b]", GameManager.PrintType.Log);
		AddToConsole("CROSSHAIR [i]0-7[/i] [b]gauntlet-bfg10k/all[/b] [b]small_/large_1-50[/b] -> Set Crosshair for the [i]player[/i], default: [b]all small_5, railgun large_7[/b]", GameManager.PrintType.Log);
		AddToConsole("CROSSHAIRALPHA [i]0-7[/i] [b]0-100[/b] -> Set CrossHair [b]Transparency[/b] for the [i]player[/i], default: [b]25[/b]", GameManager.PrintType.Log);
		AddToConsole("CROSSHAIRSCALE [i]0-7[/i] [b]10-100[/b] -> Set 2D CrossHair [b]Scale[/b] for the [i]player[/i], default: [b]100[/b]", GameManager.PrintType.Log);
		AddToConsole("COLOR [i]0-7[/i] [b]color[/b] -> Change the [b]color[/b] (color can be #hex or by name) for the [i]player[/i], default: [b]#50a1cd[/b]", GameManager.PrintType.Log);
		AddToConsole("DEVMAP [b]mapName[/b] -> Load [b]map[/b] with Cheats Enabled", GameManager.PrintType.Log);
		AddToConsole("FRAGLIMIT [b]limit[/b] -> Set the [b]fraglimit[/b] per map, default: [b]15[/b]", GameManager.PrintType.Log);
		AddToConsole("FOV [i]0-7[/i] [b]30-130[/b] -> Set [b]Fov[/b] for the [i]player[/i], default: [b]90[/b]", GameManager.PrintType.Log);
		AddToConsole("GIVE [i]0-7[/i] [b]itemName[/b] -> Give [b]Item[/b] for the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("GOD [i]0-7[/i] -> Switch GOD Mode for the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("HUD2DSCALE [i]0-7[/i] [b]10-100[/b] -> Set 2D HUD Elements [b]Scale[/b] for the [i]player[/i], default: [b]100[/b]", GameManager.PrintType.Log);
		AddToConsole("HUD3DSCALE [i]0-7[/i] [b]10-100[/b] -> Set 3D HUD Elements [b]Scale[/b] for the [i]player[/i], default: [b]100[/b]", GameManager.PrintType.Log);
		AddToConsole("HUDSHOW [i]0-7[/i] [b]true/false[/b] -> Set HUD Visibility [b]true/false[/b] for the [i]player[/i], default: [b]true[/b]", GameManager.PrintType.Log);
		AddToConsole("INVERTVIEW [i]0-7[/i] [b]true/false[/b] -> Set Invert view control [b]true/false[/b] for the [i]player[/i], default: [b]false[/b]", GameManager.PrintType.Log);
		AddToConsole("KILL [i]0-7[/i] -> Kill the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("LISTMAPS -> List all the posible maps that can be played", GameManager.PrintType.Log);
		AddToConsole("LISTMODELS -> List all the posible player models that can be used", GameManager.PrintType.Log);
		AddToConsole("LISTSKINS [i]0-7[/i] -> List all the posible skins for the current [i]player[/i] model", GameManager.PrintType.Log);
		AddToConsole("MAP [b]mapName[/b] -> Load [b]map[/b]", GameManager.PrintType.Log);
		AddToConsole("MODEL [i]0-7[/i] [b]modelName[/b] -> Change the [b]model[/b] for the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("MOUSESENSITIVITY [i]0-7[/i] [b]X,Y[/b] -> Change the mouse sensitivity [b]X,Y[/b] for the [i]player[/i], default: [b].5,.5[/b]", GameManager.PrintType.Log);
		AddToConsole("NEXTMAP -> Change to the next map in the map rotation list", GameManager.PrintType.Log);
		AddToConsole("PLAYERNAME [i]0-7[/i] [b]name[/b] -> Change the [b]name[/b] for the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("QUIT -> Quits the game", GameManager.PrintType.Log);
		AddToConsole("REMOVEPLAYER [i]0-7[/i] -> Remove the [i]player[/i] from the game", GameManager.PrintType.Log);
		AddToConsole("SAFESWAP [i]0-7[/i] [b]true/false[/b] -> Swap to safe weapon when out of ammo [b]true/false[/b] for the [i]player[/i], default: [b]true[/b]", GameManager.PrintType.Log);
		AddToConsole("SKIN [i]0-7[/i] [b]skinName[/b] -> Change the [b]skin[/b] for the [i]player[/i]", GameManager.PrintType.Log);
		AddToConsole("STICKSENSITIVITY [i]0-7[/i] [b]X,Y[/b] -> Change the controller sticks sensitivity [b]X,Y[/b] for the [i]player[/i], default: [b]4,3[/b]", GameManager.PrintType.Log);
		AddToConsole("TIMELIMIT [b]limit[/b] -> Set the [b]timelimit[/b] per map, default: [b]7[/b]", GameManager.PrintType.Log);
		AddToConsole("[b]bold[/b] -> Denotes [b]Obligatory[/b]", GameManager.PrintType.Log);
		AddToConsole("[i]italic[/i] -> Denotes [i]Optional[/i]", GameManager.PrintType.Log);
	}

	public void ChangeHUD2DScale(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string scale = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				scale = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		bool failed = true;
		int Scale = 0;
		if (int.TryParse(scale, out Scale))
			failed = false;

		if ((failed) || (Scale < 10) || (Scale > 100))
		{
			AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
			return;
		}
		GameManager.Instance.Players[playerNum].playerInfo.configData.HUD2DScale = Scale;
		GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeSpriteScale(Scale);
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeHUD3DScale(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string scale = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				scale = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		bool failed = true;
		int Scale = 0;
		if (int.TryParse(scale, out Scale))
			failed = false;

		if ((failed) || (Scale < 10) || (Scale > 100))
		{
			AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
			return;
		}
		GameManager.Instance.Players[playerNum].playerInfo.configData.HUD3DScale = Scale;
		GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeModelScale(Scale);
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeHUDVisibility(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string hudshow = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				hudshow = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (hudshow == "TRUE")
		{
			GameManager.Instance.Players[playerNum].playerInfo.configData.HUDShow = true;
			GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.UpdateLayersHud(GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.UIMask);
		}
		else if (hudshow == "FALSE")
		{
			GameManager.Instance.Players[playerNum].playerInfo.configData.HUDShow = false;
			GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.UpdateLayersHud(1 << GameManager.UINotVisibleLayer);
		}
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + hudshow + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeInvertView(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string invert = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				invert = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (invert == "TRUE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.InvertView = true;
		else if (invert == "FALSE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.InvertView = false;
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + invert + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void KillPlayer(string[] args, string command)
	{
		int playerNum = 0;
		if (args.Length > 1)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not applied. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		GameManager.Instance.Players[playerNum].Damage(1000, DamageType.Telefrag);
		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
	}

	public void ListMaps(string command)
	{
		AddToConsole("Command: " + command, GameManager.PrintType.Success);
		for (int i = 0; i < PakManager.mapList.Count; i++)
		{
			string mapName = PakManager.mapList[i];
			AddToConsole(mapName, GameManager.PrintType.Log, false);
		}
	}

	public void ListModels(string command)
	{
		AddToConsole("Command: " + command, GameManager.PrintType.Success);
		for (int i = 0; i < PakManager.playerModelList.Count; i++)
		{
			string modelName = PakManager.playerModelList[i];
			AddToConsole(modelName, GameManager.PrintType.Log, false);
		}
	}

	public void ListSkins(string[] args, string command)
	{
		AddToConsole("Command: " + command, GameManager.PrintType.Success);
		int playerNum = 0;

		if (args.Length > 1)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " . Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " . Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
			}
		}

		string currentModel = "MODELS/PLAYERS/" + GameManager.Instance.defaultModels[playerNum].ToUpper() + "/";
		if (playerNum < GameManager.Instance.Players.Count)
			currentModel = "MODELS/PLAYERS/" + GameManager.Instance.Players[playerNum].modelName.ToUpper() + "/";

		for (int i = 0; i < PakManager.playerSkinList.Count; i++)
		{
			string path = PakManager.playerSkinList[i];
			if (path.Contains(currentModel))
			{
				string skinName = path.Replace(currentModel, "");
				AddToConsole(skinName, GameManager.PrintType.Log, false);
			}
		}
	}

	public void ChangeMap(string[] args, string command, bool cheats = false)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing map to change", GameManager.PrintType.Warning);
			return;
		}

		string mapName = args[1];
		if (!PakManager.mapList.Contains(mapName))
		{
			AddToConsole("Command: " + command + " map not found", GameManager.PrintType.Warning);
			return;
		}
		GameManager.Instance.ChangeMap(mapName, cheats);
	}

	public void ChangeModel(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing model to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string model = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.defaultModels.Length)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				model = args[2];
			}
		}

		if (!PakManager.playerModelList.Contains(model))
		{
			AddToConsole("Command: " + command + " model not found", GameManager.PrintType.Warning);
			return;
		}

		if (playerNum >= GameManager.Instance.Players.Count)
			GameManager.Instance.defaultModels[playerNum] = model;
		else
			GameManager.Instance.Players[playerNum].modelName = model;
		AddToConsole("Command: " + args[0] + " sucesfully changed to " + model + " for Player " + playerNum, GameManager.PrintType.Success);
	}

	public void ChangeMouseSensitivity(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing sensitivity value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string sensitivity = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				sensitivity = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (!sensitivity.Contains(','))
		{
			AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
			return;
		}


		string[] check = sensitivity.Split(',');
		if (check.Length != 2)
		{
			AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
			return;
		}

		string[] sens = new string[2];
		for (int i = 0; i < check.Length; i++)
		{
			sens[i] = "";
			for (int j = 0; j < check[i].Length; j++)
			{
				if (char.IsDigit(check[i][j]))
					sens[i] += check[i][j];
				else if (check[i][j] == '.')
					sens[i] += check[i][j];
				else
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					return;
				}
			}
		}

		Vector2 Sensitivity = new Vector2(sens[0].GetNumValue(), sens[1].GetNumValue());
		GameManager.Instance.Players[playerNum].playerInfo.configData.MouseSensitivity[0] = Sensitivity.X;
		GameManager.Instance.Players[playerNum].playerInfo.configData.MouseSensitivity[1] = Sensitivity.Y;
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangePlayerName(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing name to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string playerName = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				playerName = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		GameManager.Instance.ChangePlayerName(playerNum, playerName);
		AddToConsole("Command: " + args[0] + " sucesfully changed to " + playerName + " for Player " + playerNum, GameManager.PrintType.Success);
	}

	public void RemovePlayer(string[] args, string command)
	{
		int playerNum = 0;
		if (args.Length > 1)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not applied. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		GameManager.Instance.RemovePlayer(playerNum);
		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
	}

	public void ChangeSafeSwap(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string safeSwap = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				safeSwap = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (safeSwap == "TRUE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.SafeSwap = true;
		else if (safeSwap == "FALSE")
			GameManager.Instance.Players[playerNum].playerInfo.configData.SafeSwap = false;
		else
		{
			AddToConsole("Command: " + args[0] + " failed: " + safeSwap + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
			return;
		}

		AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeSkin(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing skin to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string skin = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.defaultSkins.Length)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				skin = args[2];
			}
		}

		string pathSkin = "MODELS/PLAYERS/" + GameManager.Instance.defaultModels[playerNum].ToUpper() + "/" + skin;
		if (playerNum < GameManager.Instance.Players.Count)
			pathSkin = "MODELS/PLAYERS/" + GameManager.Instance.Players[playerNum].modelName.ToUpper() + "/" + skin;

		if (!PakManager.playerSkinList.Contains(pathSkin))
		{
			AddToConsole("Command: " + command + " skin not found", GameManager.PrintType.Warning);
			return;
		}

		if (playerNum >= GameManager.Instance.Players.Count)
			GameManager.Instance.defaultSkins[playerNum] = skin;
		else
			GameManager.Instance.Players[playerNum].skinName = skin;
		AddToConsole("Command: " + args[0] + " sucesfully changed to " + skin + " for Player " + playerNum, GameManager.PrintType.Success);
	}

	public void ChangeStickSensitivity(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing sensitivity value to change", GameManager.PrintType.Warning);
			return;
		}

		int playerNum = 0;
		string sensitivity = args[1];
		if (args.Length > 2)
		{
			if (int.TryParse(args[1], out int value))
			{
				if (value < 0)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
					return;
				}
				if (value > GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
					return;
				}
				playerNum = value;
				sensitivity = args[2];
			}
		}

		if (playerNum == GameManager.Instance.Players.Count)
		{
			AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
			return;
		}

		if (!sensitivity.Contains(','))
		{
			AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
			return;
		}


		string[] check = sensitivity.Split(',');
		if (check.Length != 2)
		{
			AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
			return;
		}

		string[] sens = new string[2];
		for (int i = 0; i < check.Length; i++)
		{
			sens[i] = "";
			for (int j = 0; j < check[i].Length; j++)
			{
				if (char.IsDigit(check[i][j]))
					sens[i] += check[i][j];
				else if (check[i][j] == '.')
					sens[i] += check[i][j];
				else
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					return;
				}
			}
		}

		Vector2 Sensitivity = new Vector2(sens[0].GetNumValue(), sens[1].GetNumValue());
		GameManager.Instance.Players[playerNum].playerInfo.configData.StickSensitivity[0] = Sensitivity.X;
		GameManager.Instance.Players[playerNum].playerInfo.configData.StickSensitivity[1] = Sensitivity.Y;
		AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
		GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
	}

	public void ChangeTimeLimit(string[] args, string command)
	{
		if (args.Length < 2)
		{
			AddToConsole("Command: " + command + " missing time to set", GameManager.PrintType.Warning);
			return;
		}

		string limit = args[1];
		if (int.TryParse(limit, out int value))
		{
			if (value < 1)
			{
				AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " must be positive", GameManager.PrintType.Warning);
				return;
			}

			GameManager.Instance.ChangeTimeLimit(value);
			AddToConsole("Command: " + args[0] + " sucesfully changed to " + args[1], GameManager.PrintType.Success);
		}
		else
			AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " is not an integer", GameManager.PrintType.Warning);
	}
	public void ProcessCommand(string command)
	{
		string[] args = command.Split(' ');

		if (args[0] == "AUTOHOP")
			ChangeAutoHop(args, command);
		else if (args[0] == "AUTOSWAP")
			ChangeAutoSwap(args, command);
		else if (args[0] == "BLOODSCREEN")
			ChangeBloodScreen(args, command);
		else if (args[0] == "COLOR")
			ChangeColor(args, command);
		else if (args[0] == "CROSSHAIR")
			ChangeCrossHair(args, command);
		else if (args[0] == "CROSSHAIRALPHA")
			ChangeCrossHairAlpha(args, command);
		else if (args[0] == "CROSSHAIRSCALE")
			ChangeCrossHairScale(args, command);
		else if (args[0] == "DEVMAP")
			ChangeMap(args, command, true);
		else if (args[0] == "FRAGLIMIT")
			ChangeFragLimit(args, command);
		else if (args[0] == "FOV")
			ChangeFOV(args, command);
		else if (args[0] == "GIVE")
			GiveItem(args, command);
		else if (args[0] == "GOD")
			ChangeGodMode(args, command);
		else if (args[0] == "HELP")
			ShowCommands(command);
		else if (args[0] == "HUD2DSCALE")
			ChangeHUD2DScale(args, command);
		else if (args[0] == "HUD3DSCALE")
			ChangeHUD3DScale(args, command);
		else if (args[0] == "HUDSHOW")
			ChangeHUDVisibility(args, command);
		else if (args[0] == "INVERTVIEW")
			ChangeInvertView(args, command);
		else if (args[0] == "KILL")
			KillPlayer(args, command);
		else if (args[0] == "LISTMAPS")
			ListMaps(command);
		else if (args[0] == "LISTMODELS")
			ListModels(command);
		else if (args[0] == "LISTSKINS")
			ListSkins(args, command);
		else if (args[0] == "MAP")
			ChangeMap(args, command);
		else if (args[0] == "MODEL")
			ChangeModel(args, command);
		else if (args[0] == "MOUSESENSITIVITY")
			ChangeMouseSensitivity(args, command);
		else if (args[0] == "NEXTMAP")
			GameManager.Instance.ChangeMap("");
		else if (args[0] == "PLAYERNAME")
			ChangePlayerName(args, command);
		else if (args[0] == "QUIT")
			GameManager.QuitGame();
		else if (args[0] == "REMOVEPLAYER")
			RemovePlayer(args, command);
		else if (args[0] == "SAFESWAP")
			ChangeSafeSwap(args, command);
		else if (args[0] == "SKIN")
			ChangeSkin(args, command);
		else if (args[0] == "STICKSENSITIVITY")
			ChangeStickSensitivity(args, command);
		else if (args[0] == "TIMELIMIT")
			ChangeTimeLimit(args, command);
		else
			AddToConsole("Unknown Command: " + command + " type HELP for a list of commands", GameManager.PrintType.Warning);
	}

	public void AddToConsole(string String, GameManager.PrintType type, bool addNumLines = true)
	{
		if (addNumLines)
			String = totalLines + ": " + String;

		switch (type)
		{
			default:
				history.AppendText(String + "\n");
			break;
			case GameManager.PrintType.Warning:
				history.AppendText("[color=\"yellow\"]" + String + "[/color]\n");
			break;
			case GameManager.PrintType.Error:
				history.AppendText("[color=\"red\"]" + String + "[/color]\n");
			break;
			case GameManager.PrintType.Success:
				history.AppendText("[color=\"lightskyblue\"]" + String + "[/color]\n");
			break;

		}
		totalLines++;
		focusLine++;
		tempSize.Y = focusLine * lineHeight;
		history.CustomMinimumSize = tempSize;
	}

	public void ClearConsole()
	{
		history.Clear();
		history.Text = String.Empty;
		totalLines = 0;
		focusLine = 0;
	}

	public override void _Process(double delta)
	{
		float deltaTime = (float)delta;
		switch (currentState)
		{
			default:
			break;
			case GameManager.FuncState.None:
				if (MaterialManager.consoleMaterial != null)
				{
					Material = MaterialManager.consoleMaterial;
					currentState = GameManager.FuncState.End;
				}
			break;
			case GameManager.FuncState.Start:
				if (visible)
				{
					tempPosition.Y += moveSpeed * deltaTime;
					if (tempPosition.Y > 0)
					{
						tempPosition.Y = 0;
						currentState = GameManager.FuncState.End;
						commandLine.GrabFocus();
					}
					Position = tempPosition;
				}
				else
				{
					tempPosition.Y -= moveSpeed * deltaTime;
					if (tempPosition.Y  < -halfHeight)
					{
						tempPosition.Y = -halfHeight;
						Hide();
						currentState = GameManager.FuncState.End;
					}
					Position = tempPosition;
				}
				break;
			case GameManager.FuncState.End:

				if (Input.IsActionJustPressed("Action_WeaponSwitch_Down_0"))
					Scroll(false);
				else if (Input.IsActionJustPressed("Action_WeaponSwitch_Up_0"))
					Scroll(true);
				break;
		}
	}

	public void Scroll(bool up)
	{
		if ((up) && (focusLine > 0))
			focusLine--;
		else if (focusLine != totalLines)
			focusLine++;
		tempSize.Y = focusLine * lineHeight;
		history.CustomMinimumSize = tempSize;
	}
	
}
