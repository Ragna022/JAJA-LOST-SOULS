using UnityEngine;

public class CharacterSoundFXManager : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Damage Grunts")]
    [SerializeField] protected AudioClip[] damageGrunts;

    [Header("Attack Grunts")]
    [SerializeField] protected AudioClip[] attackGrunts;

    [Header("Combo Grunts")]
    [SerializeField] protected AudioClip[] comboGrunts;

    [Header("Footsteps")]
    [SerializeField] protected AudioClip[] footSteps;

    [Header("Running Footsteps")]
    [SerializeField] protected AudioClip[] RunningfootSteps;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySoundFX(AudioClip soundFX, float volume = 1, bool randomizePitch = true, float pitchRandom = 0.1f)
    {
        audioSource.PlayOneShot(soundFX, volume);
        // RESET PITCH
        audioSource.pitch = 1;

        if(randomizePitch)
        {
            audioSource.pitch += Random.Range(-pitchRandom, pitchRandom);
        }
    }

    public void PlayRollSoundFX()
    {
        audioSource.PlayOneShot(WorldSoundFXManager.instance.rollSFX);
    }

    public virtual void PlayDamageGrunt()
    {
        PlaySoundFX(WorldSoundFXManager.instance.ChooseRandomSFXFromArray(damageGrunts));
    }

    public virtual void PlayAttackGrunt()
    {
        PlaySoundFX(WorldSoundFXManager.instance.ChooseRandomSFXFromArray(attackGrunts));
    }

    public virtual void PlayComboAttackGrunt()
    {
        PlaySoundFX(WorldSoundFXManager.instance.ChooseRandomSFXFromArray(comboGrunts));
    }

    public virtual void PlayFootSteps()
    {
        PlaySoundFX(WorldSoundFXManager.instance.ChooseRandomSFXFromArray(footSteps));
    }

    public virtual void PlayRunningFootSteps()
    {
        PlaySoundFX(WorldSoundFXManager.instance.ChooseRandomSFXFromArray(RunningfootSteps));
    }
}
