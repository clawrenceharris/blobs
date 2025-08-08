using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyBlobVisuals))]
public class EnemyBlobView : ColorBlobView
{
    public override IEnumerator Merge()
    {

        EnemyBlobVisuals visuals = (EnemyBlobVisuals)Visuals;
        visuals.Spikes.sprite = null;
        return base.Merge();
    }
}