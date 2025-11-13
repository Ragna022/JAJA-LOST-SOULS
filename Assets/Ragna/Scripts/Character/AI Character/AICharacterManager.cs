using UnityEngine;

public class AICharacterManager : CharacterManager
{
    public AICharacterCombatManager aICharacterCombatManager;

    [Header("CUrrent States")]
    [SerializeField] AIState currentState;

    protected override void Awake()
    {
        base.Awake();

        aICharacterCombatManager = GetComponent<AICharacterCombatManager>();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        ProcessStateMachine();
    }

    private void ProcessStateMachine()
    {
        AIState nextState = currentState?.Tick(this);

        if(nextState != null)
        {
            currentState = nextState;
        }
    }
}