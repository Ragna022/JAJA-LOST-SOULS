using UnityEngine;

public class AICharacterManager : CharacterManager
{
    [Header("CUrrent States")]
    [SerializeField] AIState currentState;

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