using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;

public class WorldItemDatabase : MonoBehaviour
{
    public static WorldItemDatabase Instance;


    [Header("Weapons")]
    [SerializeField] List<WeaponItem> weapons = new List<WeaponItem>();

    // A LIST OF EVERY ITEM WE HAVE IN THE GAME
    [Header("Items")]
    private List<Item> items = new List<Item>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // ADD ALL OF OUR WEAPONS TO THE LIST OF IT
        foreach (var weapon in weapons)
        {
            items.Add(weapon);
        }

        // ASSIGN ALL OF OUR ITEMS WITH A UNIQUE ITEM ID
        for (int i = 0; i < items.Count; i++)
        {
            items[i].itemID = i;
        }
    }

    public WeaponItem GetWeaponByID(int ID)
    {
        return weapons.FirstOrDefault(weapon => weapon.itemID == ID);
    }
}
