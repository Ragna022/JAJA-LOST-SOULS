using UnityEngine;

public class ResetActionFlag : StateMachineBehaviour
{
    CharacterManager character;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (character == null)
        {
            character = animator.GetComponent<CharacterManager>();
            // Add a null check in case GetComponent fails (though unlikely here)
            if (character == null)
            {
                Debug.LogError("ResetActionFlag: Could not find CharacterManager component on Animator GameObject!");
                return;
            }
        }

        // ✅ ================== THE FIX ==================
        // Do NOT reset flags if the character is already dead!
        // Check the NetworkVariable directly.
        if (character.isDead.Value)
        {
            return; // Exit early, don't reset anything
        }
        // ✅ ================= END OF FIX =================

        // THIS IS CALLED WHEN AN ACTION ENDS, AND THE STATE RETURNS TO "EMPTY"
        // (Only run if character is NOT dead)
        character.isPerformingAction = false;
        character.applyRootMotion = false;
        character.canRotate = true;
        character.canMove = true;

        // Ensure characterAnimatorManager is not null before accessing it
        if (character.characterAnimatorManager != null)
        {
             character.characterAnimatorManager.DisableCanDoCombo();
        }

        if (character.IsOwner)
        {
            // Ensure characterNetworkManager is not null
            if (character.characterNetworkManager != null)
            {
                 character.characterNetworkManager.isJumping.Value = false;
            }
        }
    }

    // ... (rest of the script is fine) ...
}