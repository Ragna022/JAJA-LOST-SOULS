using System;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public MeleeWeaponDamageCollider meleeDamageCollider;

    private void Awake()
    {
        meleeDamageCollider = GetComponentInChildren<MeleeWeaponDamageCollider>();
    }

    public void SetWeaponDamage(CharacterManager characterWieldingWeapon, WeaponItem weapon)
    {
        meleeDamageCollider.characterCausingDamage = characterWieldingWeapon;
        meleeDamageCollider.physicalDamage = weapon.physicalDamage;
        meleeDamageCollider.magicDamage = weapon.magicDamage;
        meleeDamageCollider.fireDamage = weapon.fireDamage;
        meleeDamageCollider.lightningDamage = weapon.lightningDamage;
        meleeDamageCollider.holyDamage = weapon.holyDamage;

        meleeDamageCollider.lightAttackModifier = weapon.light_Attack_Modifier;
        meleeDamageCollider.lightAttackComboModifier = weapon.light_Attack_Combo_Modifier;
        meleeDamageCollider.heavyAttackModifier = weapon.heavy_Attack_Modifier;
        meleeDamageCollider.heavyAttackComboModifier = weapon.heavy_Attack_Combo_Modifier;
        meleeDamageCollider.chargedAttackModifier = weapon.charged_Attack_Modifier;
        meleeDamageCollider.chargedAttackComboModifier = weapon.charged_Attack_Combo_Modifier;
    }
}
