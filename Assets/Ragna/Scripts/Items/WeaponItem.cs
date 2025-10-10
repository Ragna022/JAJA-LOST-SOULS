using UnityEngine;

public class WeaponItem : Item
{
    // ANIMATOR CONTROLLER OVERRIDE (change attack animations based on weapon you are currently using)

    [Header("Weapon Model")]
    public GameObject weaponModel;

    [Header("WeaponItem Requirements")]
    public int strengthREQ = 0;
    public int dexREQ = 0;
    public int intREQ = 0;
    public int faithREQ = 0;

    [Header("Weapon Base Damage")]
    public int physicalDamage = 0;
    public int magicDamage = 0;
    public int fireDamage = 0;
    public int holyDamage = 0;
    public int lightningDamage = 0;

    // WEAPON GUARD ABSORPTION (BLOCKING POWER)

    [Header("Weapon Poise")]
    public float poiseDamage = 10;
    // OFFENSIVE POISE BONUS WHEN ATTACKING

    // WEAPON MODIFIERS
    // LIGHT ATTACK MODIFIERS
    // HEAVY ATTACK MODIFIERS
    // CRITICAL ATTACK MODIFIERS

    [Header("Stamina Costs")]
    public int baseStaminaCost = 20;
    // RUNNING ATTCAK STAMINA COST MODIFIER
    // LIGHT ATTACK STAMINA COST
    // HEAVY ATTACK SRAMINA COST ETC

    // ITEM BASED ACTIONS (RB, RT, LB, LT)
    [Header("Actions")]
    public WeaponItemAction oh_RB_Actions; //ONE HAND RIGHT BUMPER ACTION

    // ASH OF WAR

    // BLOCKING SOUNDS
}
