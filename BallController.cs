using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour {
	// Public Variables
	public float initialXPosition, initialYPosition, initialXVector, initialYVector;
	public GUIText currentPosition, currentVelocity, expectedTimeText, collisionText, statusText, hitText, totalPointsText, earnedPointsText;
	public bool DEBUG_MODE;
	// End Public Variables
	
	// Private Variables
	private float customGravity = -9.81f, clickAcceleration = 30f, weakImpactModifier = 0.97f, strongImpactModifier = 0.65f;
	private float[] targetTimeDifference = {0f, 0.25f, 0.6f, 1.0f};
	private Vector3 nullVector = new Vector3 (0,0,0), initialVector;
	private string otherObjectTag = "NULL", status;
	private string[] statusMessages = {"MEGA fPRIME!", "SUPER fPRIME!", "fPRIME!", ""};
	private bool didHit = false;
	private int hitLevel = 4, totalPoints = 0;
	
	// time calculation variables
	private float expectedTime = 0.0f, trueTime = 0.0f, newStartTime = 0.0f;
	
	// End Private Variables
	///////////////////////////////////////////////////////////////////////////////////////
	
	void Start () {
		// initialize ball with normal earth gravity, 
		Physics.gravity = new Vector3(0,customGravity, 0);
		
		transform.Translate(initialXPosition, initialYPosition, 0);
		initialVector = new Vector3 (initialXVector, initialYVector, 0);
		
		if (initialYPosition > 0) {
			rigidbody.AddRelativeForce (initialVector, ForceMode.VelocityChange);
		}
		SetExpectedTime(-initialYVector);
		UPDATE_DEBUG_TEXT();
	}

	void Update() {
		SetTrueTime();		
		if(Input.GetButtonDown("Fire1")) {
			CheckClicked();
		}
		if(DEBUG_MODE) UPDATE_DEBUG_TEXT();
	}
	
	void FixedUpdate () {
		CheckTrajectory();
	}
	
	void OnTriggerEnter(Collider other) {
		Debug.Log("OnTriggerEnter() called!");
		
		otherObjectTag = other.gameObject.tag;
		float tempX = rigidbody.velocity.x;
		float tempY = rigidbody.velocity.y;
		
		// Detect surface and handle collision.
		if (other.gameObject.tag == "Floor") {
			if(tempY < 1.0f) tempY *= 2.0f; 
			tempY = -tempY;
			// Reset expectedTime
			newStartTime = Time.time;
			SetExpectedTime(tempY);
		} else if((other.gameObject.tag == "Ceiling")) {
			tempY = -tempY;
		} else if (other.gameObject.tag == "Wall") {
			tempX = -tempX;
		}
		
		if(didHit) {
			ChangeVelocity(tempX * strongImpactModifier, tempY * strongImpactModifier);
			didHit = false;
		} else {
			ChangeVelocity(tempX * weakImpactModifier, tempY * weakImpactModifier);
		}
		// Pause to check collision/impact velocity stats
		//Debug.Break();
	}
	// END UNITY METHODS /////////////////////////////////////////////////////////////////////////
	
	string FormattedTime(float theTime) {
		if (theTime < 10) return "0" + (theTime % 60).ToString () + "s";
		else return (theTime % 60).ToString () + "s";
	}
	
	void SetTrueTime() {
		trueTime = Time.time - newStartTime;
	}
	
	void SetExpectedTime(float currentYVelocity) {
		if(currentYVelocity > 0) currentYVelocity = -currentYVelocity;
		Debug.Log("SetExpectedTime() called!");
		expectedTime = currentYVelocity/customGravity;
	}
	
	float TimeDifference() {
		return expectedTime - trueTime;
	}
	
	void ChangeVelocity(float newX, float newY) {
		rigidbody.velocity = nullVector;
		rigidbody.AddRelativeForce (new Vector3(newX, newY, 0), ForceMode.VelocityChange);
	}
	
	int CalculatePoints() {
		int pointsEarned = (2 * (3 - hitLevel) - 1) * 100;
		earnedPointsText.text = statusMessages[hitLevel]+"\n+" + pointsEarned.ToString();
		return pointsEarned;
	}
	
	void AddPoints(int points) {
		totalPoints += points;
		totalPointsText.text = totalPoints.ToString();
	}
	
	// Check Position/Trajectory methods
	void CheckTrajectory() {
		// Set an index to be the number of elements in "target time difference" array, minus one. 
		// This is purely for flexibility, in the event that I add more target levels later
		int index = targetTimeDifference.Length - 1;
		hitLevel = statusMessages.Length - 1;
		
		// Currently working!
		while((TimeDifference() < targetTimeDifference[index]) && (TimeDifference() > -targetTimeDifference[index])) {
			index--;
			hitLevel--;
		}
		status = statusMessages[hitLevel];
	}
	
	void CheckClicked() {
		float dX, dY;
		SphereCollider sc = (SphereCollider)rigidbody.collider;
		Vector2 clickPixelPosition;
		// Depending on platform... 
		
		// acquire tapped position
		if((Application.platform == RuntimePlatform.IPhonePlayer) || (Application.platform == RuntimePlatform.Android)) {
			clickPixelPosition = Input.GetTouch(0).position;
		} else {	// otherwise acquire clicked position
			clickPixelPosition = Input.mousePosition;
		}
		// Convert clicked position from pixel units to Unity's "World Position" units, z is based on camera's placed distance from ball
		Vector3 clickWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(clickPixelPosition.x, clickPixelPosition.y, 16));
		
		dX = clickWorldPosition.x - rigidbody.position.x;
		dY = clickWorldPosition.y - rigidbody.position.y;
		
		Debug.Log("Clicked (X,Y): ("+clickWorldPosition.x+", "+clickWorldPosition.y+")");
		
		if (Mathf.Sqrt((dX*dX + dY*dY)) <= sc.radius){
			didHit = true;
			ChangeVelocity(-dX * clickAcceleration,-dY * clickAcceleration);
			AddPoints(CalculatePoints());
		}
		//Debug.Break();
	}
	
	// DEBUG METHODS //////////////////////////////////////////////////////////////////////////
	void UPDATE_DEBUG_TEXT() {
		if(DEBUG_MODE) {
			// If in DEBUG_MODE, Update DEBUG GUI Text
			currentPosition.text = "Pos.: ("+rigidbody.position.x+", "+rigidbody.position.y+")";
			currentVelocity.text = "Vel.: ("+rigidbody.velocity.x+", "+rigidbody.velocity.y+")";
			expectedTimeText.text = "Ex.T.: "+FormattedTime(expectedTime)+" | Ac.T.: "+FormattedTime(trueTime);
			collisionText.text = "Last Collider Hit: "+otherObjectTag;
			statusText.text = "Status: "+status;
			hitText.text = "Hit: "+didHit;
		} else {
			Destroy(currentPosition);
			Destroy(currentVelocity);
			Destroy(expectedTimeText);
			Destroy(collisionText);
			Destroy(statusText);
			Destroy(hitText);
		}
	}
}	