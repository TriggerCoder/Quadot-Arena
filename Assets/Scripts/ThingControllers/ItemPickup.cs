using Godot;
using System;
public partial class ItemPickup : Area3D
{
	public enum ItemType
	{
		Health,
		Armor,
		Weapon,
		Bullets,
		Shells,
		Grenades,
		Rockets,
		Lightning,
		Slugs,
		Cells,
		Bfgammo,
		Quad,
		Haste,
		Regen,
		Invis,
		Enviro,
		Flight,
		Teleporter
	}
	[Export]
	public ThingController thingController;
	[Export]
	public ItemType itemType;
	[Export]
	public int amount;
	[Export]
	public bool bonus;
	[Export]
	public int givesWeapon = -1;
	[Export]
	public string PickupSound;
	[Export]
	public string SecondaryPickupSound;
	[Export]
	public string PickupIcon;
	[Export]
	public string PickupText;

	public override void _Ready()
	{
		PickupIcon = PickupIcon.ToUpper();
		BodyEntered += OnBodyEntered;
	}

	public void PickUp(PlayerThing player, bool disableCollider = true)
	{
		bool disable = false;
		switch (itemType)
		{
			default:
				break;

			case ItemType.Bullets:
				if (player.playerInfo.Ammo[PlayerInfo.bulletsAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.bulletsAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.bulletsAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.bulletsAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.bulletsAmmo])
					player.playerInfo.Ammo[PlayerInfo.bulletsAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.bulletsAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.bulletsAmmo], PlayerInfo.bulletsAmmo);
				disable = true;
				break;

			case ItemType.Shells:
				if (player.playerInfo.Ammo[PlayerInfo.shellsAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.shellsAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.shellsAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.shellsAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.shellsAmmo])
					player.playerInfo.Ammo[PlayerInfo.shellsAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.shellsAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.shellsAmmo], PlayerInfo.shellsAmmo);
				disable = true;
				break;

			case ItemType.Grenades:
				if (player.playerInfo.Ammo[PlayerInfo.grenadesAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.grenadesAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.grenadesAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.grenadesAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.grenadesAmmo])
					player.playerInfo.Ammo[PlayerInfo.grenadesAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.grenadesAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.grenadesAmmo], PlayerInfo.grenadesAmmo);
				disable = true;
				break;

			case ItemType.Rockets:
				if (player.playerInfo.Ammo[PlayerInfo.rocketsAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.rocketsAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.rocketsAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.rocketsAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.rocketsAmmo])
					player.playerInfo.Ammo[PlayerInfo.rocketsAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.rocketsAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.rocketsAmmo], PlayerInfo.rocketsAmmo);
				disable = true;
				break;

			case ItemType.Lightning:
				if (player.playerInfo.Ammo[PlayerInfo.lightningAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.lightningAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.lightningAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.lightningAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.lightningAmmo])
					player.playerInfo.Ammo[PlayerInfo.lightningAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.lightningAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.lightningAmmo], PlayerInfo.lightningAmmo);
				disable = true;
				break;

			case ItemType.Slugs:
				if (player.playerInfo.Ammo[PlayerInfo.slugAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.slugAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.slugAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.slugAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.slugAmmo])
					player.playerInfo.Ammo[PlayerInfo.slugAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.slugAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.slugAmmo], PlayerInfo.slugAmmo);
				disable = true;
				break;

			case ItemType.Cells:
				if (player.playerInfo.Ammo[PlayerInfo.cellsAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.cellsAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.cellsAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.cellsAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.cellsAmmo])
					player.playerInfo.Ammo[PlayerInfo.cellsAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.cellsAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.cellsAmmo], PlayerInfo.cellsAmmo);
				disable = true;
				break;

			case ItemType.Bfgammo:
				if (player.playerInfo.Ammo[PlayerInfo.bfgAmmo] == player.playerInfo.MaxAmmo[PlayerInfo.bfgAmmo])
					break;

				player.playerInfo.Ammo[PlayerInfo.bfgAmmo] += (amount * GameManager.Instance.PlayerAmmoReceive);
				if (player.playerInfo.Ammo[PlayerInfo.bfgAmmo] > player.playerInfo.MaxAmmo[PlayerInfo.bfgAmmo])
					player.playerInfo.Ammo[PlayerInfo.bfgAmmo] = player.playerInfo.MaxAmmo[PlayerInfo.bfgAmmo];
				player.playerInfo.playerPostProcessing.playerHUD.UpdateAmmo(player.playerInfo.Ammo[PlayerInfo.bfgAmmo], PlayerInfo.bfgAmmo);
				disable = true;
				break;

			case ItemType.Health:
				if (bonus)
				{
					if (player.hitpoints == player.playerInfo.MaxBonusHealth)
						break;

					player.hitpoints += amount;
					if (player.hitpoints > player.playerInfo.MaxBonusHealth)
						player.hitpoints = player.playerInfo.MaxBonusHealth;
				}
				else
				{
					if (player.hitpoints >= player.playerInfo.MaxHealth)
						break;

					player.hitpoints += amount;
					if (player.hitpoints > player.playerInfo.MaxHealth)
						player.hitpoints = player.playerInfo.MaxHealth;
				}
				player.playerInfo.playerPostProcessing.playerHUD.UpdateHealth(player.hitpoints);
				disable = true;
				break;

			case ItemType.Armor:
				if (bonus)
				{
					if (player.armor == player.playerInfo.MaxBonusArmor)
						break;

					player.armor += amount;
					if (player.armor > player.playerInfo.MaxBonusArmor)
						player.armor = player.playerInfo.MaxBonusArmor;
				}
				else
				{
					if (player.armor >= player.playerInfo.MaxArmor)
						break;

					player.armor += amount;
					if (player.armor > player.playerInfo.MaxArmor)
						player.armor = player.playerInfo.MaxArmor;
				}
				player.playerInfo.playerPostProcessing.playerHUD.UpdateArmor(player.armor);
				disable = true;
				break;

			case ItemType.Quad:
				player.playerInfo.quadDamage = true;
				player.quadTime += amount;
				player.playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Quad, Mathf.CeilToInt(player.quadTime));
				disable = true;
				break;

			case ItemType.Haste:
				player.playerInfo.haste = true;
				player.hasteTime += amount;
				player.playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Haste, Mathf.CeilToInt(player.hasteTime));
				disable = true;
				break;

			case ItemType.Regen:
				player.regenTime += amount;
				player.playerInfo.playerPostProcessing.playerHUD.UpdatePowerUpTime(PlayerHUD.PowerUpType.Regen, Mathf.CeilToInt(player.regenTime));
				disable = true;
				break;

			case ItemType.Teleporter:
				if (player.holdableItem != PlayerThing.HoldableItem.None)
					break;
				player.holdableItem = PlayerThing.HoldableItem.Teleporter;
				player.playerInfo.playerPostProcessing.playerHUD.AddHoldableItem(PlayerThing.HoldableItem.Teleporter);
				disable = true;
				break;
		}

		bool showItemPickUp = true;
		if (givesWeapon != -1)
		{
			if (givesWeapon >= 0 && givesWeapon < player.playerInfo.Weapon.Length)
			{
				if (!player.playerInfo.Weapon[givesWeapon])
				{
					player.playerInfo.Weapon[givesWeapon] = true;

					if (player.playerInfo.configData.AutoSwap)
					{
						player.playerInfo.playerPostProcessing.playerHUD.AddWeapon(givesWeapon);
						player.playerControls.TrySwapWeapon(givesWeapon);
					}
					else
						player.playerInfo.playerPostProcessing.playerHUD.AddWeapon(givesWeapon, player.playerControls.CurrentWeapon);

					showItemPickUp = false;
					disable = true;
				}
			}
		}

		if (disable)
		{
			player.playerInfo.playerPostProcessing.playerHUD.pickupFlashTime(.3f);
			if (disableCollider)
				thingController.DisableThing();
			if (!string.IsNullOrEmpty(PickupSound))
			{
				AudioStream sound = SoundManager.LoadSound(PickupSound);
				if ((sound == null) && (!string.IsNullOrEmpty(SecondaryPickupSound)))
					sound = SoundManager.LoadSound(GameManager.Instance.announcer + SecondaryPickupSound);
				if (sound != null)
					SoundManager.Create3DSound(GlobalPosition, sound);
			}
			if (!string.IsNullOrEmpty(PickupText))
				if (showItemPickUp)
					player.playerInfo.playerPostProcessing.playerHUD.ItemPickUp(PickupIcon, PickupText);
		}
	}

	void OnBodyEntered(Node3D other)
	{
		if (thingController.disabled)
			return;

		if (other is PlayerThing player)
		{
			if (!player.ready)
				return;

			PickUp(player);
		}
	}
}
