using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory(ActionCategory.StateMachine)]
[ActionTarget(typeof(PlayMakerFSM), "gameObject,fsmName", false)]
[HutongGames.PlayMaker.Tooltip("Set the current state of an FSM.")]
public class SetFsmState : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("The GameObject that owns the FSM.")]
    public FsmGameObject gameObject;

    [UIHint(UIHint.FsmName)]
    [HutongGames.PlayMaker.Tooltip("Optional name of FSM on GameObject. Useful if there is more than one FSM on the GameObject.")]
    public FsmString fsmName;

    [HutongGames.PlayMaker.Tooltip("The name of the state to set the FSM to.")]
    public FsmString stateName;

    private PlayMakerFSM fsm;

    public override void Reset()
    {
        gameObject = null;
        fsmName = null;
    }

    public override void OnEnter()
    {
        DoSetFsmState();
    }

    private void DoSetFsmState()
    {
        GameObject value = gameObject.Value;
        if (value == null)
        {
            return;
        }
        fsm = ActionHelpers.GetGameObjectFsm(value, fsmName.Value);
        if (fsm == null)
        {
            return;
        }
        fsm.SetState(stateName.Value);
        Finish();
    }
}