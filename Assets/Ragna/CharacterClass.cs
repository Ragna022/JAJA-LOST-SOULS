using System;
using UnityEngine;

[System.Serializable]
public class CharacterClass 
{
    [Header("Class Information")]
    public string className;
    public GameObject classPrefab;

    [Header("Class Stats")]
    public int vitality = 10;
    public int endurance = 10;
    //public int mind = 10;
    public int strength = 10;
    public int dexterity = 10;
    public int intelligence = 10;
    //public int faith = 10;

    [Header("Class Weapons")]
    public WeaponItem[] mainHandWeapons = new WeaponItem[3];
    public WeaponItem[] offHandWeapons = new WeaponItem[3];

    //[Header("Quick Slots Items")]
    
    

    public void SetClass(PlayerManager player)
    {
        TitleScreenManager.Instance.SetCharacterClass(player, vitality, endurance, strength, dexterity, intelligence, mainHandWeapons, offHandWeapons);
    }
}
