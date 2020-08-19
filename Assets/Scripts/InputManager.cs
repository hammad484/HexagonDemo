using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : CellManager {
	private bool isTouch;
	private GridUpdater Gridinstance;
	private Vector2 touch_startPos;
	private HexaCell selectedHexaCell;


	
	void Start () {
		Gridinstance = GridUpdater.instance;
	}
	
	void Update () {
		if (Gridinstance.InputAvailabile() && Input.touchCount > zero) {
			/* Taking collider of touched object (if exists) to a variable */
			Vector3 wp = Camera.main.ScreenToWorldPoint(Input.GetTouch(zero).position);
			Vector2 touchPos = new Vector2(wp.x, wp.y);
			Collider2D collider = Physics2D.OverlapPoint(touchPos);
			selectedHexaCell = Gridinstance.GetSelectedHexagon();
			
			/* Processing input */
			TouchDetection();
			CheckSelection(collider);
			CheckRotation();
		}
	}



	/* Checks if first touch arrived */
	private void TouchDetection() {
		/* Set start poisiton at the beginning of touch[0] */
		if (Input.GetTouch(zero).phase == TouchPhase.Began) {
			isTouch = true;
			touch_startPos = Input.GetTouch(zero).position;
		}
	}



	/* Checks if selection condition provided and calls grid manager to handle selection */
	private void CheckSelection(Collider2D collider) {
		/* If there is a collider and its tag match with any Hexagon continue operate */
		if (collider != null && collider.transform.tag == Tag) {
			/* Select hexagon if touch ended */
			if (Input.GetTouch(zero).phase == TouchPhase.Ended && isTouch) {
				isTouch = false;
				Gridinstance.Select(collider);
			}
		}
	}



	/* Checks if rotation condition provided and calls grid manager to handle rotation */
	private void CheckRotation() {
		if (Input.GetTouch(zero).phase == TouchPhase.Moved && isTouch) {
			Vector2 touchCurrentPosition = Input.GetTouch(zero).position;
			float distanceX = touchCurrentPosition.x - touch_startPos.x;
			float distanceY = touchCurrentPosition.y - touch_startPos.y;
			

			/* Check if rotation triggered by comparing distance between first touch position and current touch position */
			if ((Mathf.Abs(distanceX) > Cell_slide_distance || Mathf.Abs(distanceY) > Cell_slide_distance) && selectedHexaCell != null) {
				Vector3 screenPosition = Camera.main.WorldToScreenPoint(selectedHexaCell.transform.position);

				/* Simplifying long boolean expression thanks to ternary condition
					* triggerOnX specifies if rotate action triggered from a horizontal or vertical swipe 
					* swipeRightUp specifies if swipe direction was right or up
					* touchThanHex specifies if touch position value is bigger than hexagon position on triggered axis
					* If X axis triggered rotation with same direction swipe, then rotate clockwise else rotate counter clockwise
					* If Y axis triggered rotation with opposite direction swipe, then rotate clockwise else rotate counter clocwise
					*/
				bool triggerOnX = Mathf.Abs(distanceX) > Mathf.Abs(distanceY);
				bool swipeRightUp = triggerOnX ? distanceX > zero : distanceY > zero;
				bool touchThanHex = triggerOnX ? touchCurrentPosition.y > screenPosition.y : touchCurrentPosition.x > screenPosition.x;
				bool clockWise = triggerOnX ? swipeRightUp == touchThanHex : swipeRightUp != touchThanHex;

				isTouch = false;
				Gridinstance.Rotate(clockWise);
			}
		}
	}
}
