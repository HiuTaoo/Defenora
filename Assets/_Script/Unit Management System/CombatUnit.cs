
public abstract class CombatUnit
{
    public bool isAttacking { get; protected set; }
    public bool isInWindup { get; protected set; }

    public virtual void StartAttackSignal()
    {
        isAttacking = true;
        isInWindup = true;
    }

    public virtual void EndWindupSignal()
    {
        isInWindup = false;
    }

    public virtual void EndAttackSignal()
    {
        isAttacking = false;
        isInWindup = false;
    }
}