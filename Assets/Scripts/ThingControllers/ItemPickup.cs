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
	}
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
	public string PickupIcon;
	[Export]
	public string PickupText;
	void OnItemPickupBodyEntered(PhysicsBody3D other)
	{
		if (other is PlayerThing player)
		{
			//Dead player don't pick up stuff
			if (player.Dead)
				return;

			bool disable = false;

			switch (itemType)
			{
				default:
					break;

				case ItemType.Bullets:
					if (player.playerInfo.Ammo[0] == player.playerInfo.MaxAmmo[0])
						break;
					player.playerInfo.Ammo[0] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[0] > player.playerInfo.MaxAmmo[0])
						player.playerInfo.Ammo[0] = player.playerInfo.MaxAmmo[0];
					disable = true;
					break;

				case ItemType.Shells:
					if (player.playerInfo.Ammo[1] == player.playerInfo.MaxAmmo[1])
						break;
					player.playerInfo.Ammo[1] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[1] > player.playerInfo.MaxAmmo[1])
						player.playerInfo.Ammo[1] = player.playerInfo.MaxAmmo[1];
					disable = true;
					break;

				case ItemType.Grenades:
					if (player.playerInfo.Ammo[2] == player.playerInfo.MaxAmmo[2])
						break;
					player.playerInfo.Ammo[2] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[2] > player.playerInfo.MaxAmmo[2])
						player.playerInfo.Ammo[2] = player.playerInfo.MaxAmmo[2];
					disable = true;
					break;

				case ItemType.Rockets:
					if (player.playerInfo.Ammo[3] == player.playerInfo.MaxAmmo[3])
						break;
					player.playerInfo.Ammo[3] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[3] > player.playerInfo.MaxAmmo[3])
						player.playerInfo.Ammo[3] = player.playerInfo.MaxAmmo[3];
					disable = true;
					break;

				case ItemType.Lightning:
					if (player.playerInfo.Ammo[4] == player.playerInfo.MaxAmmo[4])
						break;
					player.playerInfo.Ammo[4] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[4] > player.playerInfo.MaxAmmo[4])
						player.playerInfo.Ammo[4] = player.playerInfo.MaxAmmo[4];
					disable = true;
					break;

				case ItemType.Slugs:
					if (player.playerInfo.Ammo[5] == player.playerInfo.MaxAmmo[5])
						break;
					player.playerInfo.Ammo[5] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[5] > player.playerInfo.MaxAmmo[5])
						player.playerInfo.Ammo[5] = player.playerInfo.MaxAmmo[5];
					disable = true;
					break;

				case ItemType.Cells:
					if (player.playerInfo.Ammo[6] == player.playerInfo.MaxAmmo[6])
						break;
					player.playerInfo.Ammo[6] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[6] > player.playerInfo.MaxAmmo[6])
						player.playerInfo.Ammo[6] = player.playerInfo.MaxAmmo[6];
					disable = true;
					break;

				case ItemType.Bfgammo:
					if (player.playerInfo.Ammo[7] == player.playerInfo.MaxAmmo[7])
						break;
					player.playerInfo.Ammo[7] += (amount * GameManager.Instance.PlayerAmmoReceive);
					if (player.playerInfo.Ammo[7] > player.playerInfo.MaxAmmo[7])
						player.playerInfo.Ammo[7] = player.playerInfo.MaxAmmo[7];
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

					disable = true;
					break;
			}

			if (givesWeapon != -1)
			{
				if (givesWeapon >= 0 && givesWeapon < player.playerInfo.Weapon.Length)
				{
					if (!player.playerInfo.Weapon[givesWeapon])
					{
						player.playerInfo.Weapon[givesWeapon] = true;
						player.playerControls.TrySwapWeapon(givesWeapon);
						player.lookTime = 1.5f;
						disable = true;
					}
				}
			}

			if (disable)
			{
//				player.playerInfo.playerHUD.pickupFlashTime = .5f;

				if (!string.IsNullOrEmpty(PickupSound))
					SoundManager.Create3DSound(Position, SoundManager.LoadSound(PickupSound));

//				player.playerInfo.playerHUD.HUDUpdateAmmoNum();
//				player.playerInfo.playerHUD.HUDUpdateHealthNum();
//				player.playerInfo.playerHUD.HUDUpdateArmorNum();
//				gameObject.SetActive(false);
			}
		}
	}
}
