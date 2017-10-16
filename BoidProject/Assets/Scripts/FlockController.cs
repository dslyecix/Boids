using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This object is intended to sit on an empty GameObject
and dictate where and how to move the Flock.  This will
allow top-level movement of the general flock while still
allowing the flock to move naturally within it. */


public class FlockController : MonoBehaviour {

    [SerializeField]
    Flock flock;

    void OnDrawGizmos( )
    {
        if (flock.useBoundary) {
            Gizmos.color = Color.white * 0.1f;
            Gizmos.DrawSphere(transform.position,flock.maximumBoundRadius);
        }
    }  

}
