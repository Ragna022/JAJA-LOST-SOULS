using UnityEngine;

public class CharacterCombatManager : MonoBehaviour
{
    CharacterManager character;

    [Header("Last Attack Animation Performed")]
    public string lastAttackAnimationPerformed;

    public AttackType currentAttackType;

    //public WeaponItem cuurentweaponBeingUsed;

    protected virtual void Awake()
    {

    }
    
   
}
