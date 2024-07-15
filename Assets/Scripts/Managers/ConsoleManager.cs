using Godot;
using System;
using ExtensionMethods;
public partial class ConsoleManager : Control
{
	[Export]
	public LineEdit commandLine;
	[Export]
	public RichTextLabel history;
	public int moveSpeed = 1000;

	private GameManager.FuncState currentState = GameManager.FuncState.None;

	public const int lineHeight = 23;
	public bool visible = false;
	private VScrollBar scrollBar;
	private Vector2 tempPosition;
	private Vector2 tempSize;
	private int halfHeight;
	private int totalLines = 0;
	private int focusLine = 0;
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
		scrollBar = history.GetVScrollBar();
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
		ProcessCommand(command.ToUpper());
	}

	public void ProcessCommand(string command)
	{
		string[] args = command.Split(' ');

		switch (args[0])
		{
			default:
				AddToConsole("Unknown Command: " + command + " type HELP for a list of commands", GameManager.PrintType.Warning);
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
					}
				}
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
			case "HELP":
			{
				AddToConsole("Command: " + command, GameManager.PrintType.Success);
				AddToConsole("The following is a list of commands:", GameManager.PrintType.Log);
				AddToConsole("COLOR [i]0-8[/i] [b]color[/b] -> Change the [b]color[/b] (color can be #hex or by name) for the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("FRAGLIMIT [b]limit[/b] -> Set the [b]fraglimit[/b] per map", GameManager.PrintType.Log);
				AddToConsole("KILL [i]0-8[/i] -> Kill the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("LISTMAPS -> List all the posible maps that can be played", GameManager.PrintType.Log);
				AddToConsole("LISTMODELS -> List all the posible player models that can be used", GameManager.PrintType.Log);
				AddToConsole("LISTSKINS [i]0-8[/i] -> List all the posible skins for the current [i]player[/i] model", GameManager.PrintType.Log);
				AddToConsole("MAP [b]mapName[/b] -> Change the map", GameManager.PrintType.Log);
				AddToConsole("MODEL [i]0-8[/i] [b]modelName[/b] -> Change the [b]model[/b] for the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("MOUSESENSITIVITY [i]0-8[/i] [b]X,Y[/b] -> Change the mouse sensitivity [b]X,Y[/b] for the [i]player[/i], default: [b].5,.5[/b]", GameManager.PrintType.Log);
				AddToConsole("NEXTMAP -> Change to the next map in the map rotation list", GameManager.PrintType.Log);
				AddToConsole("PLAYERNAME [i]0-8[/i] [b]name[/b] -> Change the [b]name[/b] for the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("QUIT -> Quits the game", GameManager.PrintType.Log);
				AddToConsole("SKIN [i]0-8[/i] [b]skinName[/b] -> Change the [b]skin[/b] for the [i]player[/i]", GameManager.PrintType.Log);
				AddToConsole("STICKSENSITIVITY [i]0-8[/i] [b]X,Y[/b] -> Change the controller sticks sensitivity [b]X,Y[/b] for the [i]player[/i], default: [b]4,3[/b]", GameManager.PrintType.Log);
				AddToConsole("TIMELIMIT [b]limit[/b] -> Set the [b]timelimit[/b] per map", GameManager.PrintType.Log);
				AddToConsole("[b]bold[/b] -> Denotes [b]Obligatory[/b]", GameManager.PrintType.Log);
				AddToConsole("[i]italic[/i] -> Denotes [i]Optional[/i]", GameManager.PrintType.Log);
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
				GameManager.Instance.Players[playerNum].playerControls.MouseSensitivity = Sensitivity;
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
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
				GameManager.Instance.Players[playerNum].playerControls.StickSensitivity = Sensitivity;
				AddToConsole("Command: " + args[0] + " sucesfully changed for Player " + playerNum, GameManager.PrintType.Success);
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
