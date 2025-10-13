using Unity.VisualScripting;
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
    public bool poiseIsBroken = false; // IF A CHARCATRE'S POISE IS BROKEN, THEY WILL BE "STUNNED" AND PLAY A DAMAGE ANIAMTION

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
        PlayDirectionalBasedDamageAnimation(character);
        // CHECK FOR BUILD UPS (POISON, BLEED ETC)
        // PLAY DAMAGE SOUND FX
        //PlayDamageSFX(character);
        //PlayDamageVFX(character);
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

    private void PlayDamageVFX(CharacterManager character)
    {
        // IF WE HAVE FIRE DAMAGE, PLAY FIRE PARTICLES
        // LIGHTNING DAMAGE, LIGHTNING PARTICLES ETC

        character.characterEffectsManager.PlayBloodSplatterVFX(contactPoint);
    }

    private void PlayDamageSFX(CharacterManager character)
    {
        AudioClip physicalDamageSFX = WorldSoundFXManager.instance.ChooseRandomSFXFromArray(WorldSoundFXManager.instance.physicalDamageSFX);

        character.characterSoundFXManager.PlaySoundFX(physicalDamageSFX);
        // IF FIRE DAMAGE IS GREATER THAN 0; PLAY BURN SFX
        // IF LIGHTNING DAMAGE IS GREATER THAN 0, PLAY ZAP SFX
    }
    
    private void PlayDirectionalBasedDamageAnimation(CharacterManager character)
{
    if (!character.IsOwner)
        return;
        
    // CRITICAL FIX: Don't play hit animation if this damage will kill the character
    int healthAfterDamage = character.characterNetworkManager.currentHealth.Value - finalDamageDealt;
    if (healthAfterDamage <= 0)
    {
        Debug.Log($"[TakeDamageEffect] Fatal damage detected, skipping hit animation for death sequence");
        return; // Skip hit animation, let death animation play directly
    }
    
    // TO: CALCULATE IF POISE IS BROKEN
    poiseIsBroken = true;
    
    if (angleHitFrom >= 145 && angleHitFrom <= 180)
    {
        // PLAY FRONT ANIMATION
        damageAnimation = character.characterAnimatorManager.hit_Forward;
    }
    else if (angleHitFrom <= -145 && angleHitFrom >= -180)
    {
        // PLAY FRONT ANIMATION
        damageAnimation = character.characterAnimatorManager.hit_Forward;
    }
    else if (angleHitFrom >= -45 && angleHitFrom <= 45)
    {
        // PLAY BACK ANIMATION
        damageAnimation = character.characterAnimatorManager.hit_Backwards;
    }
    else if (angleHitFrom >= -144 && angleHitFrom <= -45)
    {
        // PLAY LEFT ANIMATION
        damageAnimation = character.characterAnimatorManager.hit_Left;
    }
    else if (angleHitFrom >= 45 && angleHitFrom <= 144)
    {
        // PLAY RIGHT ANIMATION
        damageAnimation = character.characterAnimatorManager.hit_Right;
    }
    
    // IF POISE IS BROKEN, PLAY A STAGGERING DAMAGE ANIMATION
    if(poiseIsBroken)
    {
        character.characterAnimatorManager.PlayTargetActionAnimation(damageAnimation, true);
    }
}
}