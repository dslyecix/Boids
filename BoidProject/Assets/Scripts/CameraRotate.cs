using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotate : MonoBehaviour {

	public Transform camTarget;
	float camDistance;
	
	[Range(0.5f,3f)]
	public float camZoom;

	void Start () {
		camDistance = 2 * camTarget.GetComponent<Flock>().maximumBoundRadius;
	}

	void LateUpdate () {
		transform.LookAt(camTarget);
		transform.RotateAround(camTarget.position,Vector3.up, 15f * Time.deltaTime);		
		transform.position = camTarget.position - (camZoom * camDistance * transform.forward);
	}
}
