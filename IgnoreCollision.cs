using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory(ActionCategory.Physics2D)]
[ActionTarget(typeof(PlayMakerFSM), "gameObject,fsmName", false)]
[HutongGames.PlayMaker.Tooltip("Set collisions to be ignored between two game objects.")]
public class IgnoreCollision : FsmStateAction
{
    [RequiredField]
    [CheckForComponent(typeof(Collider2D))]
    [HutongGames.PlayMaker.Tooltip("The first game object.")]
    public FsmGameObject gameObject1;

    [RequiredField]
    [CheckForComponent(typeof(Collider2D))]
    [HutongGames.PlayMaker.Tooltip("The second game object.")]
    public FsmGameObject gameObject2;

    public override void Reset()
    {
        gameObject1 = null;
        gameObject2 = null;
    }

    public override void OnEnter()
    {
        DoIgnoreCollision();
    }

    private void DoIgnoreCollision()
    {
        GameObject value1 = gameObject1.Value;
        GameObject value2 = gameObject2.Value;
        if (value1 == null || value2 == null)
        {
            return;
        }
        
        var col1 = value1.GetComponent<Collider2D>();
        var col2 = value2.GetComponent<Collider2D>();

        if (col1 == null || col2 == null)
        {
            return;
        }

        Physics2D.IgnoreCollision(col1, col2);
        
        Finish();
    }
}