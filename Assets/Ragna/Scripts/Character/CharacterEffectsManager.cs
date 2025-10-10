using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterEffectsManager : MonoBehaviour
{
    CharacterManager character;
    public virtual void Awake()
    {
        character = GetComponent<CharacterManager>();
    }

    public virtual void ProcessInstantEffect(InstantCharacterEffects effect)
    {
        effect.ProcessEffect(character);
    }
}
