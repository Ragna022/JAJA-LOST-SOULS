using UnityEngine;

public class MeleeWeaponDamageCollider : DamageCollider
{
    [Header("Attacking Character")]
    public CharacterManager characterCausingDamage; // WHEN CALCULATING DAMAGE, THIS IS USED TO CHECK DAMAGE MODIFIERS, EFFECTS ETC
}
