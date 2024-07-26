using Godot;
using System;
using System.Collections.Generic;
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

	public void ProcessCommand(string command)
	{
		string[] args = command.Split(' ');

		switch (args[0])
		{
			default:
				AddToConsole("Unknown Command: " + command + " type HELP for a list of commands", GameManager.PrintType.Warning);
			break;
			case "AUTOHOP":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						autohop = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (autohop == "TRUE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.AutoHop = true;
					else if (autohop == "FALSE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.AutoHop = false;
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + autohop + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "AUTOSWAP":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						autoSwap = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (autoSwap == "TRUE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.AutoSwap = true;
					else if (autoSwap == "FALSE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.AutoSwap = false;
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + autoSwap + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "BLOODSCREEN":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						bloodscreen = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (bloodscreen == "TRUE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.BloodScreen = true;
					else if (bloodscreen == "FALSE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.BloodScreen = false;
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + bloodscreen + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "COLOR":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing color to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						color = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					Color modulate = Colors.White;
					bool failed = false;
					try
					{
						modulate = new Color(color);
					}
					catch (Exception e)
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
						GameManager.Instance.Players[playerNum].playerInfo.saveData.ModulateColor = color;
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "CROSSHAIR":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing weapon and crosshair type to change", GameManager.PrintType.Warning);
					break;
				}
				if (args.Length < 3)
				{
					AddToConsole("Command: " + command + " missing crosshair type to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						weapon = args[2];
						crosshair = args[3];
					}
				}

				bool failed = false;
				int weaponNum = 0;
				switch(weapon)
				{
					default:
						AddToConsole("Command: " + args[0] + " was not changed. Weapon " + weapon + " doesn't exist", GameManager.PrintType.Warning);
						failed = true;
					break;
					case "ALL":
						weaponNum = -1;
					break;
					case "GAUNTLET":
						weaponNum = 0;
					break;
					case "MACHINEGUN":
						weaponNum = 1;
					break;
					case "SHOTGUN":
						weaponNum = 2;
					break;
					case "GRENADE":
					case "GRENADELAUNCHER":
						weaponNum = 3;
					break;
					case "ROCKET":
					case "ROCKETLAUNCHER":
						weaponNum = 4;
					break;
					case "LIGHTNING":
					case "LIGHTNINGGUN":
						weaponNum = 5;
					break;
					case "RAIL":
					case "RAILGUN":
						weaponNum = 6;
					break;
					case "PLASMA":
					case "PLASMAGUN":
						weaponNum = 7;
					break;
					case "BFG":
					case "BFG10K":
						weaponNum = 8;
					break;						
				}
				if (failed)
					break;

				string[] type = crosshair.Split('_');
				if (type.Length < 2)
				{
					AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
					break;
				}
				
				failed = false;
				int CrossHair = 0;
				switch (type[0])
				{
					default:
						AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning); 
						failed = true;
					break;
					case "SMALL":
					{
						if (int.TryParse(type[1], out CrossHair))
						{
							if (CrossHair > 50)
							{
								AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
								failed = true;
							}
						}
						else
						{
							AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
							failed = true;
						}
					}
					break;
					case "LARGE":
					{
						if (int.TryParse(type[1], out CrossHair))
						{
							if (CrossHair > 50)
							{
								AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
								failed = true;
							}
							else
								CrossHair += 100;
						}
						else
						{
							AddToConsole("Command: " + command + " invalid crosshair type", GameManager.PrintType.Warning);
							failed = true;
						}
					}
					break;
				}
				if (failed)
					break;

				int[] CroosHairs = GameManager.Instance.Players[playerNum].playerInfo.saveData.CroosHair;
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
			break;
			case "CROSSHAIRALPHA":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing alpha value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						alpha = args[2];
					}
				}
				
				bool failed = true;
				int Alpha = 0;
				if (int.TryParse(alpha, out Alpha))
					failed = false;

				if ((failed) || (Alpha < 0) || (Alpha > 100))
				{
					AddToConsole("Command: " + command + " alpha is not in correct format [b]0-100[/b]", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.Players[playerNum].playerInfo.saveData.CroosHairAlpha = Alpha;
				GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHairAlpha(Alpha);
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "CROSSHAIRSCALE":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						scale = args[2];
					}
				}
				
				bool failed = true;
				int Scale = 0;
				if (int.TryParse(scale, out Scale))
					failed = false;

				if ((failed) || (Scale < 10) || (Scale > 100))
				{
					AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.Players[playerNum].playerInfo.saveData.CroosHairScale = Scale;
				GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeCrossHairScale(Scale);
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "FRAGLIMIT":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing frag to set", GameManager.PrintType.Warning);
					break;
				}
				string limit = args[1];
				if (int.TryParse(limit, out int value))
				{
					if (value < 0)
					{
						AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " must be positive", GameManager.PrintType.Warning);
						break;
					}
					GameManager.Instance.ChangeFragLimit(value);
					AddToConsole("Command: " + args[0] + " sucesfully changed to " + args[1], GameManager.PrintType.Success);
				}
				else
					AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " is not an integer", GameManager.PrintType.Warning);
			}
			break;
			case "FOV":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						fov = args[2];
					}
				}
				
				bool failed = true;
				int Fov = 0;
				if (int.TryParse(fov, out Fov))
					failed = false;

				if ((failed) || (Fov < 30) || (Fov > 130))
				{
					AddToConsole("Command: " + command + " FOV is not in correct format [b]30-130[/b]", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.Players[playerNum].playerInfo.playerCamera.ViewCamera.Fov = Fov;
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "HELP":
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
				AddToConsole("FRAGLIMIT [b]limit[/b] -> Set the [b]fraglimit[/b] per map, default: [b]15[/b]", GameManager.PrintType.Log);
				AddToConsole("FOV [i]0-7[/i] [b]30-130[/b] -> Set [b]Fov[/b] for the [i]player[/i], default: [b]90[/b]", GameManager.PrintType.Log);
				AddToConsole("HUD2DSCALE [i]0-7[/i] [b]10-100[/b] -> Set 2D HUD Elements [b]Scale[/b] for the [i]player[/i], default: [b]100[/b]", GameManager.PrintType.Log);
				AddToConsole("HUD3DSCALE [i]0-7[/i] [b]10-100[/b] -> Set 3D HUD Elements [b]Scale[/b] for the [i]player[/i], default: [b]100[/b]", GameManager.PrintType.Log);
				AddToConsole("HUDSHOW [i]0-7[/i] [b]true/false[/b] -> Set HUD Visibility [b]true/false[/b] for the [i]player[/i], default: [b]true[/b]", GameManager.PrintType.Log);
				AddToConsole("INVERTVIEW [i]0-7[/i] [b]true/false[/b] -> Set Invert view control [b]true/false[/b] for the [i]player[/i], default: [b]false[/b]", GameManager.PrintType.Log);
				AddToConsole("KILL [i]0-7[/i] -> Kill the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("LISTMAPS -> List all the posible maps that can be played", GameManager.PrintType.Log);
				AddToConsole("LISTMODELS -> List all the posible player models that can be used", GameManager.PrintType.Log);
				AddToConsole("LISTSKINS [i]0-7[/i] -> List all the posible skins for the current [i]player[/i] model", GameManager.PrintType.Log);
				AddToConsole("MAP [b]mapName[/b] -> Loads [b]map[/b]", GameManager.PrintType.Log);
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
			break;
			case "HUD2DSCALE":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						scale = args[2];
					}
				}
				
				bool failed = true;
				int Scale = 0;
				if (int.TryParse(scale, out Scale))
					failed = false;

				if ((failed) || (Scale < 10) || (Scale > 100))
				{
					AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.Players[playerNum].playerInfo.saveData.HUD2DScale = Scale;
				GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeSpriteScale(Scale);
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "HUD3DSCALE":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing scale value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						scale = args[2];
					}
				}
				
				bool failed = true;
				int Scale = 0;
				if (int.TryParse(scale, out Scale))
					failed = false;

				if ((failed) || (Scale < 10) || (Scale > 100))
				{
					AddToConsole("Command: " + command + " scale is not in correct format [b]10-100[/b]", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.Players[playerNum].playerInfo.saveData.HUD3DScale = Scale;
				GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.ChangeModelScale(Scale);
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;			
			case "HUDSHOW":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						hudshow = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (hudshow == "TRUE")
					{
						GameManager.Instance.Players[playerNum].playerInfo.saveData.HUDShow = true;
						GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.UpdateLayersHud(GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.UIMask);
					}
					else if (hudshow == "FALSE")
					{
						GameManager.Instance.Players[playerNum].playerInfo.saveData.HUDShow = false;
						GameManager.Instance.Players[playerNum].playerInfo.playerPostProcessing.playerHUD.UpdateLayersHud(1 << GameManager.UINotVisibleLayer);
					}
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + hudshow + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "INVERTVIEW":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						invert = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (invert == "TRUE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.InvertView = true;
					else if (invert == "FALSE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.InvertView = false;
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + invert + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "KILL":
			{
				int playerNum = 0;
				if (args.Length > 1)
				{
					if (int.TryParse(args[1], out int value))
					{
						if (value < 0)
						{
							AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
					}
				}

				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
					GameManager.Instance.Players[playerNum].Damage(1000, DamageType.Telefrag);
				AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
			}
			break;
			case "LISTMAPS":
			{
				AddToConsole("Command: " + command, GameManager.PrintType.Success);
				for (int i = 0; i < PakManager.mapList.Count; i++)
				{
					string mapName = PakManager.mapList[i];
					AddToConsole(mapName, GameManager.PrintType.Log, false);
				}
			}
			break;
			case "LISTMODELS":
			{
				AddToConsole("Command: " + command, GameManager.PrintType.Success);
				for (int i = 0; i < PakManager.playerModelList.Count; i++)
				{
					string modelName = PakManager.playerModelList[i];
					AddToConsole(modelName, GameManager.PrintType.Log, false);
				}
			}
			break;
			case "LISTSKINS":
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " . Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
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
			break;
			case "MAP":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing map to change", GameManager.PrintType.Warning);
					break;
				}

				string mapName = args[1];
				if (!PakManager.mapList.Contains(mapName))
				{
					AddToConsole("Command: " + command + " map not found", GameManager.PrintType.Warning);
					break;
				}
				GameManager.Instance.ChangeMap(mapName);
			}
			break;
			case "MODEL":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing model to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						model = args[2];
					}
				}
				
				if (!PakManager.playerModelList.Contains(model))
				{
					AddToConsole("Command: " + command + " model not found", GameManager.PrintType.Warning);
					break;
				}

				if (playerNum >= GameManager.Instance.Players.Count)
					GameManager.Instance.defaultModels[playerNum] = model;
				else
					GameManager.Instance.Players[playerNum].modelName = model;
				AddToConsole("Command: " + args[0] + " sucesfully changed to " + model + " for Player " + playerNum, GameManager.PrintType.Success);
			}
			break;
			case "MOUSESENSITIVITY":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing sensitivity value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						sensitivity = args[2];
					}
				}
				
				if (!sensitivity.Contains(','))
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}


				string[] check = sensitivity.Split(',');
				if (check.Length != 2)
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}

				bool failed = false;
				string[] sens = new string[2];
				for (int i = 0; i < check.Length; i++)
				{
					sens[i] = "";
					if (failed)
						break;
					for (int j = 0; j < check[i].Length; j++)
					{
						if (char.IsDigit(check[i][j]))
							sens[i] += check[i][j];
						else if (check[i][j] == '.')
							sens[i] += check[i][j];
						else
						{
							failed = true;
							break;
						}
					}
				}

				if (failed)
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}

				Vector2 Sensitivity = new Vector2(sens[0].GetNumValue(), sens[1].GetNumValue());
				GameManager.Instance.Players[playerNum].playerInfo.saveData.MouseSensitivity[0] = Sensitivity.X;
				GameManager.Instance.Players[playerNum].playerInfo.saveData.MouseSensitivity[1] = Sensitivity.Y;
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "NEXTMAP":
				GameManager.Instance.ChangeMap("");
			break;
			case "PLAYERNAME":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing name to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						playerName = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
					GameManager.Instance.Players[playerNum].playerName = playerName;
				AddToConsole("Command: " + args[0] + " sucesfully changed to " + playerName + " for Player " + playerNum, GameManager.PrintType.Success);
			}
			break;
			case "QUIT":
				GameManager.QuitGame();
			break;
			case "REMOVEPLAYER":
			{
				int playerNum = 0;
				if (args.Length > 1)
				{
					if (int.TryParse(args[1], out int value))
					{
						if (value < 0)
						{
							AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " must be positive", GameManager.PrintType.Warning);
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not applied. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
					}
				}

				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not applied. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
					GameManager.Instance.RemovePlayer(playerNum);
				AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
			}
			break;
			case "SAFESWAP":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing [b]true/false[/b] to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.Players.Count)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						safeSwap = args[2];
					}
				}
				
				if (playerNum == GameManager.Instance.Players.Count)
				{
					AddToConsole("Command: " + args[0] + " was not changed. Player " + playerNum + " doesn't exist", GameManager.PrintType.Warning);
					break;
				}
				else
				{
					bool failed = false;
					if (safeSwap == "TRUE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.SafeSwap = true;
					else if (safeSwap == "FALSE")
						GameManager.Instance.Players[playerNum].playerInfo.saveData.SafeSwap = false;
					else
						failed = true;

					if (failed)
						AddToConsole("Command: " + args[0] + " failed: " + safeSwap + " is not [b]true/false[/b]", GameManager.PrintType.Warning);
					else
					{
						AddToConsole("Command: " + command + " was succesfully applied", GameManager.PrintType.Success);
						GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
					}
				}
			}
			break;
			case "SKIN":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing skin to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
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
					break;
				}

				if (playerNum >= GameManager.Instance.Players.Count)
					GameManager.Instance.defaultSkins[playerNum] = skin;
				else
					GameManager.Instance.Players[playerNum].skinName = skin;
				AddToConsole("Command: " + args[0] + " sucesfully changed to " + skin + " for Player " + playerNum, GameManager.PrintType.Success);
			}
			break;
			case "STICKSENSITIVITY":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing sensitivity value to change", GameManager.PrintType.Warning);
					break;
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
							break;
						}
						if (value > GameManager.Instance.defaultModels.Length)
						{
							AddToConsole("Command: " + args[0] + " was not changed. Player " + args[1] + " doesn't exist", GameManager.PrintType.Warning);
							break;
						}
						playerNum = value;
						sensitivity = args[2];
					}
				}
				
				if (!sensitivity.Contains(','))
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}


				string[] check = sensitivity.Split(',');
				if (check.Length != 2)
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}

				bool failed = false;
				string[] sens = new string[2];
				for (int i = 0; i < check.Length; i++)
				{
					sens[i] = "";
					if (failed)
						break;
					for (int j = 0; j < check[i].Length; j++)
					{
						if (char.IsDigit(check[i][j]))
							sens[i] += check[i][j];
						else if (check[i][j] == '.')
							sens[i] += check[i][j];
						else
						{
							failed = true;
							break;
						}
					}
				}

				if (failed)
				{
					AddToConsole("Command: " + command + " sensitivity is not in correct format [b]X,Y[/b]", GameManager.PrintType.Warning);
					break;
				}

				Vector2 Sensitivity = new Vector2(sens[0].GetNumValue(), sens[1].GetNumValue());
				GameManager.Instance.Players[playerNum].playerInfo.saveData.StickSensitivity[0] = Sensitivity.X;
				GameManager.Instance.Players[playerNum].playerInfo.saveData.StickSensitivity[1] = Sensitivity.Y;
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
				GameManager.Instance.Players[playerNum].playerInfo.SaveConfigData();
			}
			break;
			case "TIMELIMIT":
			{
				if (args.Length < 2)
				{
					AddToConsole("Command: " + command + " missing time to set", GameManager.PrintType.Warning);
					break;
				}
				string limit = args[1];
				if (int.TryParse(limit, out int value))
				{
					if (value < 0)
					{
						AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " must be positive", GameManager.PrintType.Warning);
						break;
					}

					GameManager.Instance.ChangeTimeLimit(value);
					AddToConsole("Command: " + args[0] + " sucesfully changed to " + args[1], GameManager.PrintType.Success);
				}
				else
					AddToConsole("Command: " + args[0] + " was not changed. " + args[1] + " is not an integer", GameManager.PrintType.Warning);
			}
			break;
		}
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
