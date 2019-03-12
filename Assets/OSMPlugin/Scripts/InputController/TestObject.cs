using UnityEngine;

namespace InputControls
{ 
    public class TestObject : MonoBehaviour
	{
        void Start()
		{
            InputManager.Instance.InputControls.OnTwistEvent += OnTwist;
            InputManager.Instance.InputControls.OnPinchEvent += OnPinch;
            InputManager.Instance.InputControls.OnTapEvent += OnTap;
            InputManager.Instance.InputControls.OnHoldTapEvent += OnHold;
            InputManager.Instance.InputControls.OnTwoFingerVerticalSwipeEvent += OnTwoFingerVerticalSwipe;
            InputManager.Instance.InputControls.OnDragEvent += OnTwoFingerDrag;
        }

        Camera testCam;

        private void Awake()
        {
            testCam = Camera.main;
        }


        void OnTwist(float pRotationAngle)
		{
            Quaternion target = Quaternion.Euler(testCam.transform.eulerAngles.x, testCam.transform.eulerAngles.y, -pRotationAngle);

            testCam.transform.rotation = Quaternion.Slerp(testCam.transform.rotation, target, 5 * Time.deltaTime);
        }

        void OnPinch(float pScale)
		{
            pScale += transform.position.z;
            float scale = Mathf.Clamp(pScale, 0, 50);
            transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, scale), 5 * Time.deltaTime);
        }

        void OnTap(double pX, double pY)
        {
            
        }

        void OnHold()
        {

        }

        void OnTwoFingerDrag(double x, double y)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;

                testCam.transform.Translate(-touchDeltaPosition.x * Time.deltaTime * 3, -touchDeltaPosition.y * Time.deltaTime * 3, 0);
            }
        }

        void OnTwoFingerVerticalSwipe(float pTiltAngle)
        {
            pTiltAngle += transform.eulerAngles.x;
            float angle = Mathf.Clamp(pTiltAngle, 0, 55);

            Vector3 newEulerRot = new Vector3(angle,
                                    transform.eulerAngles.y ,
                                    transform.eulerAngles.z);

            transform.eulerAngles = Vector3.Lerp(transform.localEulerAngles, newEulerRot, 5 * Time.deltaTime);
        }
    }
}
