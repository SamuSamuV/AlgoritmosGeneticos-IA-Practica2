using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTrigger : MonoBehaviour
{
    public Transform Target;

    private bool _toDestroy = false;

    public float DistanceToTarget;

    public delegate void SetResult(float result);

    public event SetResult OnHitCollider;
    // Start is called before the first frame update
    public void OnCollisionEnter(Collision col)
    {
        Debug.DrawRay(transform.position, Target.transform.position - transform.position,Color.red,10f); 
        DistanceToTarget =Vector3.Distance(transform.position, Target.transform.position);
        OnHitCollider(DistanceToTarget);
        _toDestroy = true;
    }

    public void Update()
    {
        if (_toDestroy) DestroyImmediate(this.gameObject);
    }
}
