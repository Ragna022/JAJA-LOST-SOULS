using UnityEngine;

public class AI_Enemy_DamageCollider : DamageCollider
{
    [SerializeField] AICharacterManager aiCharacterCausingDamage;

    protected override void Awake()
    {
        base.Awake();

        damageCollider = GetComponent<Collider>();
        aiCharacterCausingDamage = GetComponentInParent<AICharacterManager>();
    }
    protected override void DamageTarget(CharacterManager damageTarget)
    {
        // SO WE ADD THEM TO A LIST THAT CHECKS BEFORE APPLYING DAMAGE

        if (charactersDamaged.Contains(damageTarget)) // HERE, JUST INCASE THE OPEN AND CLOSE COLLIDER DOES NOT WORK 
            return;

        charactersDamaged.Add(damageTarget);

        TakeDamageEffect damageEffect = Instantiate(WorldCharacterEffectsManager.instance.takeDamageEffect);
        damageEffect.physicalDamage = physicalDamage;
        damageEffect.magicDamage = magicDamage;
        damageEffect.fireDamage = fireDamage;
        damageEffect.holyDamage = holyDamage;
        damageEffect.contactPoint = contactPoint;
        damageEffect.angleHitFrom = Vector3.SignedAngle(aiCharacterCausingDamage.transform.forward, damageTarget.transform.forward, Vector3.up);

        if(damageTarget.IsOwner)
        {
            // SEND A DAMAGE REQUEST TO THE SERVER
            damageTarget.characterNetworkManager.NotifyTheServerOfCharacterDamageServerRpc(
                damageTarget.NetworkObjectId,
                aiCharacterCausingDamage.NetworkObjectId,
                damageEffect.physicalDamage,
                damageEffect.magicDamage,
                damageEffect.fireDamage,
                damageEffect.holyDamage,
                damageEffect.poiseDamage,
                damageEffect.angleHitFrom,
                damageEffect.contactPoint.x,
                damageEffect.contactPoint.y,
                damageEffect.contactPoint.z);
        }
    }
}
