﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingCamera : MonoBehaviour {
	const float START_RT_SMOOTH_COEFFICIENT = 0.1f;
	const float START_ZM_SMOOTH_COEFFICIENT = 0.1f;
	const float START_MV_SMOOTH_COEFFICIENT = 0.12f;

	public Transform cam;
	public float rotationSpeed = 65;
	public float zoomSpeed = 50;
	float rotationSmoothCoefficient = START_RT_SMOOTH_COEFFICIENT;
	float rotationSmoothAcceleration = 0.05f;
	float zoomSmoothCoefficient = START_ZM_SMOOTH_COEFFICIENT;
	float zoomSmoothAcceleration = 0.05f;
	float moveSmoothCoefficient = START_MV_SMOOTH_COEFFICIENT;
	float moveSmoothAcceleration= 0.04f;
	public Vector3 deltaLimits = new Vector3 (0.1f, 0.1f, 0.1f);
	public Vector3 camPoint = new Vector3(0,5,-5);

	void Start() {
		cam.transform.position = transform.position + transform.TransformDirection(camPoint);
		cam.transform.LookAt(transform.position);
	}
	// Update is called once per frame
	void LateUpdate () {
		if (cam == null ) return;
		Vector3 mv = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		bool mv_cf_counted = false;
		if (mv != Vector3.zero) {
			//transform.Translate(mv * 30 * moveSmoothCoefficient * Time.deltaTime,Space.Self);
			transform.Translate(mv * 30 * Time.deltaTime,Space.Self);
			moveSmoothCoefficient += moveSmoothAcceleration;
			mv_cf_counted = true;
		}
		else moveSmoothCoefficient = START_MV_SMOOTH_COEFFICIENT;
		if (Input.GetKey(KeyCode.Space)) {
			//transform.Translate(Vector3.up * 15 * moveSmoothCoefficient * Time.deltaTime, Space.World);
			transform.Translate(Vector3.up * 15 * Time.deltaTime, Space.World);
			if ( !mv_cf_counted ) {moveSmoothCoefficient += moveSmoothAcceleration;mv_cf_counted = true;}
		}
		if (Input.GetKey(KeyCode.LeftControl)) {
			//transform.Translate(Vector3.down * 15 * moveSmoothCoefficient * Time.deltaTime , Space.World);
			transform.Translate(Vector3.down * 15  * Time.deltaTime , Space.World);
			if ( !mv_cf_counted ) moveSmoothCoefficient += moveSmoothAcceleration;
		}
		if (moveSmoothCoefficient > 2) moveSmoothCoefficient = 2;

		float delta = 0;
		if (Input.GetMouseButton(2)) {
			bool a = false , b = false; //rotation detectors
			float rspeed = rotationSpeed * Time.deltaTime * rotationSmoothCoefficient;
			delta = Input.GetAxis("Mouse X");
			if (delta != 0) {
				transform.RotateAround(transform.position, Vector3.up, rspeed * delta);
				rotationSmoothCoefficient += rotationSmoothAcceleration;
				a = true;
			}

			delta = Input.GetAxis("Mouse Y") ;
			if (delta != 0) {
				cam.transform.RotateAround(transform.position, transform.TransformDirection(Vector3.left), rspeed * delta);
				rotationSmoothCoefficient += rotationSmoothAcceleration;
				b = true;
			}

			if (a==false && b== false) rotationSmoothCoefficient = START_RT_SMOOTH_COEFFICIENT;
		}

		delta = Input.GetAxis("Mouse ScrollWheel");
		if (delta != 0) {
			float zspeed = zoomSpeed * Time.deltaTime * zoomSmoothCoefficient * delta * (-1);
			cam.transform.Translate((cam.transform.position - transform.position) * zspeed, Space.World );
			zoomSmoothCoefficient += zoomSmoothAcceleration;
		}
		else zoomSmoothCoefficient = START_ZM_SMOOTH_COEFFICIENT;

	}
}
