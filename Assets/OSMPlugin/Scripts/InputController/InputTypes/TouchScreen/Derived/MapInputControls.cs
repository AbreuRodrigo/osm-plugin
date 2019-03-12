using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InputControls {

    public class MapInputControls : TouchScreenInputControls
    {
        [Header("Pinch Start State Trigger Settings")]
        [Tooltip("The distance requirement between two fingers to trigger pinch input start state. DEFAULT: 7f")]
        [SerializeField]
        [Range(1, 15)]
        private float _pinchMagnitudeMinimum = 7f;
        [SerializeField]
        [Tooltip("Helps determining input start state. Checks the direction of the 2 finger start touch to the direction of the 2 finger current touch. Helps with angles. 1 = the direction of both vectors are the same. DEFAULT: 0.95f")]
        [Range(-1, 1)]
        private float _minPinchDot = 0.95f;

        [Header("Two-Finger Vertical Swipe Start State Trigger Settings")]
        [Tooltip("The distance requirement between two Vectors (Center of current touches and center of starting touches) to trigger Vertical Swipe (Tilt) input start state. DEFAULT: 5f")]
        [Range(1, 15)]
        [SerializeField]
        private float _twoFingerVeritcalSwipeMagnitudeMinimum = 5f;
        [SerializeField]
        [Tooltip("The distance min/max leeway of horizontal positioning between fingers to allow a vertical swipe to trigger. Helps determining if desired start state is a pinch or swipe. DEFAULT: 10f")]
        [Range(0, 20)]
        private float _twoFingerXPositionLeeway = 10;
        [SerializeField]
        [Tooltip("The distance min/max leeway of vertical positioning between fingers to allow a vertical swipe to trigger. Helps determining if desired start state is a pinch or swipe. DEFAULT: 30f")]
        [Range(0, 35)]
        private float _twoFingerYPositionLeeway = 30;
        [Tooltip("Helps determining input start state. Checks the direction of the 2 finger start touch to the direction of the 2 finger current touch. Helps with angles. 1 = the direction of both vectors are the same. DEFAULT: 0.998f")]
        [Range(-1, 1)]
        [SerializeField]
        private float _minTwoFingerVerticalSwipeDot = 0.998f;

        [Header("Twist Start State Trigger Settings")]
        [SerializeField]
        [Tooltip("Minimum angle of vectors needed to trigger start state of Twist. DEFAULT: 6f")]
        [Range(0, 180)]
        private float _minTurnAngle = 6f;
        [Tooltip("Helps determining input start state. Checks the direction of the 2 finger start touch to the direction of the 2 finger current touch. Helps with angles. 1 = the direction of both vectors are the same. DEFAULT: 0.95f")]
        [Range(-1, 1)]
        [SerializeField]
        private float _maxTwistDot = 0.95f;

        [Header("Twist Input Settings")]
        [Tooltip("When our starting state is in Twist state, check on a distance magnitude to also be able to zoom in and out. DEFAULT: 5f")]
        [Range(1, 50)]
        [SerializeField]
        private float _inTwistStateZoomMagnitudeMinimum = 5;

        [Header("Drag Input Settings")]
        [Tooltip("When starting state is Twist, check on a distance magnitude from center of both finger to also be able to drag. Happens once and is activated until two finger is released. DEFAULT: 60f")]
        [Range(10, 100)]
        [SerializeField]
        private float _inTwistDragMagnitudeMinimum = 40;

        private Coroutine _setStartUserInputStateCo;

        private Vector2 _twoFingerStartingVectorDirection = new Vector2();
        private Vector2 _startingCenterOfBothTouches = new Vector2();

        //Determines what states are available and what settings they have depending on starting desired actions
        private ETouchInputState _startingInputState;

        bool isTwoFingerDragConditionMet = false;

        protected override void OnDrag(Touch pTouch)
        {
            InvokeDragEventFromDerived(pTouch.position.x, pTouch.position.y);
        }

        #region One Finger Functions
        protected override void OnOneFingerRelease(Touch pTouch)
        {
            ToggleNoneState(true);
        }

        protected override void OnTouchOneFingerBegan(Touch pTouch)
        {

            _fingersOnBeganOverUIObject[0] = IsFingerBeganOverUIObject(pTouch);
            if (_fingersOnBeganOverUIObject[0] == true)
            {
                return;
            }

            //Remove the flag for saying we are not in a state
            //passing true will clear all stats and set to none, passing flase will just unset the none state
            ToggleNoneState(false);

            //Currently, One finger defaults into pan state 
            SetFlagState(ETouchInputState.Drag);
        }
        #endregion

        #region Two Finger Functions
        protected override void OnTouchTwoFingerBegan(Touch pTouchOne, Touch pTouchTwo)
        {
            _fingersOnBeganOverUIObject[1] = IsFingerBeganOverUIObject(pTouchTwo);
            if (_fingersOnBeganOverUIObject[1] == true)
            {
                return;
            }

            StopAllOneFingerActionsOnTwoFingerOrHigherCount();

            SetTwoFingerStartStateWrapper(pTouchOne, pTouchTwo);
        }

        protected override void OnTwoFinger(Touch pTouchOne, Touch pTouchTwo)
        {

            if (CheckInputHasFlag(ETouchInputState.Drag) == true)
            {
                Vector2 currentCenterPos = (Input.GetTouch(0).position + Input.GetTouch(1).position) / 2;

                //Check for a different drag conditions depending on start state to not inturupt desired input
                switch (_startingInputState) 
                {
                     //TODO DECIDE LOGIC FOR DRAG ENABLED FOR PINCH IN OUT
                     //case ETouchInputState.Pinch:

                    //  if(Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
                    //  {
                    //
                    //
                    //      if (isTwoFingerDragConditionMet == true)
                    //      {
                    //          InvokeDragEventFromDerived(currentCenterPos.x, currentCenterPos.y);
                    //      }
                    //      else if ()
                    //      {
                    //          isTwoFingerDragConditionMet = true;
                    //      }
                    //  }
                    //break;

                    default:
                        if (isTwoFingerDragConditionMet == true)
                        {
                            InvokeDragEventFromDerived(currentCenterPos.x, currentCenterPos.y);
                        }
                        else if (Vector2.Distance(currentCenterPos, _startingCenterOfBothTouches) > _inTwistDragMagnitudeMinimum)
                        {
                            isTwoFingerDragConditionMet = true;
                        }
                        break;
                } 
            }

            if (CheckInputHasFlag(ETouchInputState.Pinch) == true)
            {

                //Check for a different pinch condition if in starting desired state twist as to not be annoyed into zoom when trying to just rotate
                if (_startingInputState == ETouchInputState.Twist)
                {
                    if (GetAbsDistanceOfTouchesFromPreviousFrame() > _inTwistStateZoomMagnitudeMinimum)
                    {
                        InvokePinchEventFromDerived(GetPinchDistance());
                    }   
                }
                else
                {
                    InvokePinchEventFromDerived(GetPinchDistance());
                }
            }

            if (CheckInputHasFlag(ETouchInputState.Twist) == true)
            {

                InvokeTwistEventFromDerived(GetCurrentTwistAngle());
            }

            if (CheckInputHasFlag(ETouchInputState.TwoFingerVerticalSwipe) == true)
            {
                InvokeVerticalSwipeEventFromDerived(GetTilt());
            }

        }

        private IEnumerator SetTwoFingerStartState(Touch pTouchOne, Touch pTouchTwo)
        {
            ETouchInputState startState = ETouchInputState.None;

            float touchOneStartingX = pTouchOne.position.x;
            float touchTwoStartingX = pTouchTwo.position.x;

            _startingCenterOfBothTouches = (pTouchOne.position + pTouchTwo.position) / 2;
            _twoFingerStartingVectorDirection = pTouchOne.position - pTouchTwo.position;
            
            while (startState == ETouchInputState.None)
            {
                //Return state NONE if no condition is met (Pinch, twist, zoom, etc)
                startState = IsStartStateConditionsMet(touchOneStartingX, touchTwoStartingX);
                yield return null;
            }

            //Reset these vectors intentiontally to our current positions
            _startingCenterOfBothTouches = (Input.GetTouch(0).position + Input.GetTouch(1).position) / 2;
            _twoFingerStartingVectorDirection = Input.GetTouch(0).position - Input.GetTouch(1).position;

            SetStartingFlagsBaseOnDesiredInput(startState);
        }

        private void SetTwoFingerStartStateWrapper(Touch pTouchOne, Touch pTouchTwo)
        {
            StopTwoFingerStartState();

            _setStartUserInputStateCo = StartCoroutine(SetTwoFingerStartState(pTouchOne, pTouchTwo));
        }

        private void StopTwoFingerStartState()
        {
            if(_setStartUserInputStateCo != null)
            {
                StopCoroutine(_setStartUserInputStateCo);
            }     
        }

        protected override void OnTwoFingerRelease(Touch pTouch)
        {
            StopTwoFingerStartState();

            //stops dragging condition bool check for dragging. This can be trigged to true only is input state drag is allowed from start state
            isTwoFingerDragConditionMet = false;

            //Default back into pan for 1 finger so we can continue to drag
            _currentInputStates = ETouchInputState.Drag;

            _twoFingerStartingVectorDirection = Vector2.zero;
            _startingCenterOfBothTouches = Vector2.zero;
        }

        private void SetStartingFlagsBaseOnDesiredInput(ETouchInputState pStartState)
        {
            _startingInputState = pStartState;
            Debug.Log("START STATE: " + pStartState);

            //REMOVES PREVIOUS STATES
            _currentInputStates = ETouchInputState.None;

            switch (pStartState)
            {
                case ETouchInputState.Twist:
                    //ADD STATES
                    SetFlagState(ETouchInputState.Twist);
                    SetFlagState(ETouchInputState.Drag);
                    SetFlagState(ETouchInputState.Pinch);
                    break;

                case ETouchInputState.TwoFingerVerticalSwipe:
                    //ADD STATES
                    SetFlagState(ETouchInputState.TwoFingerVerticalSwipe);
                    break;

                case ETouchInputState.Pinch:
                    //ADD STATES
                    SetFlagState(ETouchInputState.Pinch);
                    SetFlagState(ETouchInputState.Drag);
                    break;
            }

            Debug.Log("ACTIVE STATES: " + _currentInputStates);
        }

        #endregion

        #region Pan Map Functions
 
        #endregion

        #region Zoom Map Functions
        private float GetPinchDistance()
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            
            return deltaMagnitudeDiff;
        }
        #endregion

        #region Tilt Map Functions
        private float GetTilt()
        {
            Vector2 currentCenterPosOfBothTouched = (Input.GetTouch(0).position + Input.GetTouch(1).position) / 2;
            float mag = (_startingCenterOfBothTouches - currentCenterPosOfBothTouched).magnitude / 5;

            //Return a negtive to tilt down
            if(currentCenterPosOfBothTouched.y < _startingCenterOfBothTouches.y)
            {
                mag = -mag;
            }

            return mag;
        }

        private bool IsTwoFingerVerticalSwipeConditionMet(float x1, float x2)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
            {
                if (Input.GetTouch(0).position.y > Input.GetTouch(1).position.y - _twoFingerYPositionLeeway &&
                    Input.GetTouch(0).position.y < Input.GetTouch(1).position.y + _twoFingerYPositionLeeway)
                {
                    if (x1 > Input.GetTouch(0).position.x - _twoFingerXPositionLeeway &&
                        x1 < Input.GetTouch(0).position.x + _twoFingerXPositionLeeway &&
                        x2 > Input.GetTouch(1).position.x - _twoFingerXPositionLeeway &&
                        x2 < Input.GetTouch(1).position.x + _twoFingerXPositionLeeway)
                    {
                        Vector2 currentCenterPosOfBothTouched = (Input.GetTouch(0).position + Input.GetTouch(1).position) / 2;
                        float mag = (_startingCenterOfBothTouches - currentCenterPosOfBothTouched).magnitude;

                        if (mag >= _twoFingerVeritcalSwipeMagnitudeMinimum)
                        {
                            return true;
                        }
                    }
                }     
            }
            return false;
        }
        #endregion

        #region Rotate Map Functions
        private float GetCurrentTwistAngle()
        {
            Vector2 from = Input.GetTouch(0).position - Input.GetTouch(1).position;
            Vector2 to = new Vector2(1, 0);

            float result = Vector2.Angle(from, to);
            Vector3 cross = Vector3.Cross(from, to);

            if (cross.z > 0)
            {
                result = 360f - result;
            }

            return result;
        }
        #endregion

        private ETouchInputState IsStartStateConditionsMet(float pTouchOneStartingX, float pTouchTwoStartingX)
        {
            //Find the position in the previous frame of each touch.
            float distanceCurrentTouches = GetAbsDistanceOfTouchesFromPreviousFrame();

            //Find out angles for twist condition
            Vector2 currentTouchDir = Input.GetTouch(0).position - Input.GetTouch(1).position;
            float angle = Vector2.Angle(_twoFingerStartingVectorDirection, currentTouchDir);

            //Get the dot prod of our start touch to our current touch to better determine what state we can go into
            float dot = Vector2.Dot(_twoFingerStartingVectorDirection.normalized, currentTouchDir.normalized);

            //Bool check for two finger swipe up/down state
            bool isTwoFingerVerticalSwipeConditionMet = IsTwoFingerVerticalSwipeConditionMet(pTouchOneStartingX, pTouchTwoStartingX);

            Debug.Log("Distance: " + distanceCurrentTouches + " | New Angle: " + angle + " | Dot Prod: " + dot + " | IsTwoFingerVerticalSwipeConditionMet: "
                      + isTwoFingerVerticalSwipeConditionMet + " | Y Pos: " + Input.GetTouch(0).position.y + " " +
                      Input.GetTouch(1).position.y + " | X Pos: " + Input.GetTouch(0).position.x + " " + Input.GetTouch(1).position.x);

            //TWIST CONDITION CHECK 
            if (angle >= _minTurnAngle && isTwoFingerVerticalSwipeConditionMet == false && dot < _maxTwistDot)
            {
                return ETouchInputState.Twist;
            }
      
            //PINCH CONDITION CHECK
            else if (distanceCurrentTouches >= _pinchMagnitudeMinimum && 
                     dot > _minPinchDot && 
                     isTwoFingerVerticalSwipeConditionMet == false &&
                     angle < _minTurnAngle) 
            {
                return ETouchInputState.Pinch;
            }
      
            //TILT CONDITION CHECK
            else if (isTwoFingerVerticalSwipeConditionMet == true && dot >= _minTwoFingerVerticalSwipeDot)
            {
                return ETouchInputState.TwoFingerVerticalSwipe;
            }

            return ETouchInputState.None;
        }

        private float GetAbsDistanceOfTouchesFromPreviousFrame()
        {
            Vector2 touchZeroPrevPos = Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition;
            Vector2 touchOnePrevPos = Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;

            // Find the difference in the distances between each frame.
            float distanceCurrentTouches = prevTouchDeltaMag - touchDeltaMag;
            distanceCurrentTouches = Mathf.Abs(distanceCurrentTouches);

            return distanceCurrentTouches;
        }
    }
}
