using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.TextCore.Text;

[CreateAssetMenu(menuName = "Character Effects/Instant Effects/Take Damage")]
public class TakeDamageEffect : InstantCharacterEffects
{
    [Header("Charcater Causing Damage")]
    public CharacterManager characterCausingDamage; // if the damage is caused by another characters attack it will be stored here

    [Header("Damage")]
    public float physicalDamage = 0; // WILL BE SPLIT INTO STANDARD, STRIKE, SLASH AND PIERCE
    public float magicDamage = 0;
    public float fireDamage = 0;
    public float lightningDamage = 0;
    public float holyDamage = 0;

    [Header("Final Damage")]
    private int finalDamageDealt = 0; // The Damage the character takes after all calculations have been made

    [Header("Poise")]
    public float poiseDamage = 0;
    public bool poiseBroken = false; // IF A CHARCATRE'S POISE IS BROKEN, THEY WILL BE "STUNNED" AND PLAY A DAMAGE ANIAMTION

    // TODO BUILD UPS
    // BUILD UP EFFECTS AMOUNT

    [Header("Animation")]
    public bool playDamageAnimation = true;
    public bool manuallySelectDamageAnimation = false;
    public string damageAnimation;

    [Header("Sound FX")]
    public bool willPlayDamageSFX = true;
    public AudioClip elementalDamageSoundFX; // USED ON TOP OF REGULAR SFX IF THERE IS ELEMENTAL DAMAGE PRESENT (magic/fire/light/Holy)

    [Header("Direction Damage Taken From")]
    public float angleHitFrom; // USED TO DETERMIND WHAT DAMAGE ANIMATION TO PLAY (move backwards, left or right)
    public Vector3 contactPoint; // USED TO DETERMINE WHERE THE BLOOD FX INSTANTIATE

    public override void ProcessEffect(CharacterManager character)
    {
        base.ProcessEffect(character);

        // IF THE CHARACTER IS DEAD, DO NOT PROCESS ANY ADDITIONAL DAMAGE EFFECTS
        if (character.isDead.Value)
            return;

        // CHECK FOR "INVULNERABILITY"
        // CALCULATE DAMAGE
        CalculateDamage(character);

        // CHECK WHICH DIRECTIONAL DAMAGE CAMME FROM
        // PLAY A DAMAGE ANIAMTION
        // CHECK FOR BUILD UPS (POISON, BLEED ETC)
        // PLAY DAMAGE SOUND FX
        // IF CHARACTER IS AI, CHECK FOR NEW TARGET IF CHARACTER CAUSING DAMAGE IS PRESENT
    }

    private void CalculateDamage(CharacterManager character)
    {
        if (!character.IsOwner)
            return;

        if (characterCausingDamage != null)
        {
            // CHECK FOR DAMAGE MODIFIERS AND MODIFY BASE DAMAGE (PHYSICAL/ELEMENT DAMAGE BUFF)
        }

        // CHECK CHARACTER FOR FLAT DEFENSES AND SUBTRACT THEM FROM THE DAMAGE
        // CHECK CHARACTER FOR ARMOR ABSORBTIONS, AND SUBTRACT THE PERCENTAGE FROM THR DAMAGE
        // ADD ALL DAMAGE TYPES TOGEHTER, AND APPLY FINAL DAMAGE

        finalDamageDealt = Mathf.RoundToInt(physicalDamage + magicDamage + fireDamage + lightningDamage + holyDamage);

        if (finalDamageDealt <= 0)
        {
            finalDamageDealt = 1;
        }

        Debug.Log("FINAL DAMAGE GIVEN: " + finalDamageDealt);

        character.characterNetworkManager.currentHealth.Value -= finalDamageDealt;

        // CALCULATE POISE DAMAGE TO DETERMINE IF THE CHARACTER WILL BE STUNNED 
    }
}
