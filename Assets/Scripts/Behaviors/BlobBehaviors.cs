using UnityEngine;

public class NormalBlobMoveBehavior : IMoveBehavior
{
    public void OnMove(ref MovePlan plan)
    {
        plan.Path.Add(plan.CurrentPosition);
    }
}

public class RockBlobMoveBehavior : IMoveBehavior
{
    public void OnMove(ref MovePlan plan)
    {
        // Do not add the rock cell to the path; moving blob stops on the cell before the rock.
        plan.ShouldTerminate = true;
    }
}