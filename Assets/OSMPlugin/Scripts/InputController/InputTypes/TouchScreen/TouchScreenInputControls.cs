using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InputControls {

    public class TouchScreenInputControls : BaseInputControls {

        //Contains flags of all the states we are in
        protected ETouchInputState _currentInputStates;

        //Reference the coroutine so we can stop it at anytime without errors
        Coroutine _convertTapToHoldCo;

        protected Vector2 _onTapBeganFingerStartingPosition = Vector2.zero;

        protected bool[] _fingersOnBeganOverUIObject = new bool[2] { false, false };

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            //DO SOMETHING
        }

        protected override void OnInputTouch()
        {
            for (int i = 0; i < Input.touchCount; i++) {

                Touch touch = Input.GetTouch(i);

                switch (touch.phase) {
                    case TouchPhase.Began:
                        if (Input.touchCount == 2) {
                            OnTouchTwoFingerBegan(Input.GetTouch(0), Input.GetTouch(1));
                        }
                        else if(Input.touchCount == 1) {
                            OnTouchOneFingerBegan(touch);
                        }   
                        break;

                    case TouchPhase.Ended:
                        if (Input.touchCount == 2 && IsEtherFingerOnBeganOverUIObject() == false) {
                            OnTwoFingerRelease(touch);
                        }
                        else if(Input.touchCount == 1 && _fingersOnBeganOverUIObject[0] == false)
                        {
                            OnOneFingerRelease(touch);
                        }
                        break;

                    case TouchPhase.Moved:
                        if (Input.touchCount == 2 && IsEtherFingerOnBeganOverUIObject() == false) {
                            OnTwoFinger(Input.GetTouch(0), Input.GetTouch(1));
                        }
                        else if(Input.touchCount == 1 && _fingersOnBeganOverUIObject[0] == false)
                        {
                            OnDrag(touch);
                        }
                        break;

                    case TouchPhase.Stationary:
                        if (Input.touchCount == 2 && IsEtherFingerOnBeganOverUIObject() == false)
                        {
                            OnTwoFinger(Input.GetTouch(0), Input.GetTouch(1));
                        }
                        else if (Input.touchCount == 1 && _fingersOnBeganOverUIObject[0] == false)
                        {
                            OnTouchHold(touch);
                        }
                        break;
                }
            }
        }

        #region Convert Tap To Hold
        //Safety wrapper
        protected void ConvertTapToHoldWrapper()
        {
            StopConvertTapToHoldCo();
            _convertTapToHoldCo = StartCoroutine(ConvertTapToHold());
        }

        protected void StopConvertTapToHoldCo()
        {
            RemoveFlagState(ETouchInputState.OneFingerHold);

            if (_convertTapToHoldCo != null)
            {
                StopCoroutine(_convertTapToHoldCo);
            }
        }

        IEnumerator ConvertTapToHold()
        {
            yield return new WaitForSeconds(TimeUntilTapBecomesHold);
            SetFlagState(ETouchInputState.OneFingerHold);
        }
        #endregion

        #region One Finger Touches
        protected virtual void OnTouchOneFingerBegan(Touch pTouch)
        {
            _fingersOnBeganOverUIObject[0] = IsFingerBeganOverUIObject(pTouch);
            if (_fingersOnBeganOverUIObject[0] == true)
            {
                return;
            }

            //Register this value to be used for functions such as dragging, etc
            RegisterFirstTouchPosition(pTouch.position);

            //Screen Position values
            InvokeTapEventFromDerived(pTouch.position.x, pTouch.position.y);

            //Start a coroutine that will convert Tap to Hold if time condition is met
            ConvertTapToHoldWrapper();
        }

        void RegisterFirstTouchPosition(Vector2 pPos)
        {
            _onTapBeganFingerStartingPosition = pPos;
        }

        protected virtual void OnTouchHold(Touch pTouch)
        {
            if (CheckInputHasFlag(ETouchInputState.OneFingerHold) == true)
            {
                InvokeHoldEventFromDerived();
            }
        }

        protected virtual void OnOneFingerRelease(Touch pTouch)
        {
            StopConvertTapToHoldCo();
            RemoveFlagState(ETouchInputState.OneFingerHold);
        }

        protected virtual void OnDrag(Touch pTouch)
        {
            //First time we enter this function the drag will be false, stop hold and set to drag bool value
            if(CheckInputHasFlag(ETouchInputState.Drag) == false)
            {
                Vector2 pos = pTouch.position;

                //We only switch from hold to drag if the magnitude of finger press is far enough from touchbegan pos
                if ((_onTapBeganFingerStartingPosition - pos).magnitude >= DistanceFromTouchPosToBeADrag)
                {
                    //Make sure to stop Hold logic from flowing if we change into dragging
                    StopConvertTapToHoldCo();

                    SetFlagState(ETouchInputState.Drag);
                }
            }
            else
            {
                InvokeDragEventFromDerived(pTouch.position.x, pTouch.position.y);
            } 
        }
        #endregion

        #region Two Finger Functions
        protected virtual void OnTouchTwoFingerBegan(Touch pTouchOne, Touch pTouchTwo)
        {
            _fingersOnBeganOverUIObject[1] = IsFingerBeganOverUIObject(pTouchTwo);
            if (_fingersOnBeganOverUIObject[1] == true)
            {
                return;
            }

            StopAllOneFingerActionsOnTwoFingerOrHigherCount();
        }

        protected virtual void OnTwoFinger(Touch pTouchOne, Touch pTouchTwo) {
            InvokePinchEventFromDerived(CalculateDistanceBetweenVectors(pTouchOne.position, pTouchTwo.position));

            float rotAngle = Vector2.Angle(pTouchOne.position, pTouchTwo.position);
            InvokeTwistEventFromDerived(rotAngle);
        }

        protected virtual void OnTwoFingerRelease(Touch pTouch)
        {
            //Recalculate remaining finger start pos for drag if a finger remains
            //If finger 1 is the end, calculate finger 2 for start pos
            if (pTouch.fingerId == 0)
            {
                RegisterFirstTouchPosition(Input.GetTouch(1).position);
            }
            else
            {
                RegisterFirstTouchPosition(Input.GetTouch(0).position);
            }
        }

        protected void StopAllOneFingerActionsOnTwoFingerOrHigherCount()
        {
            StopConvertTapToHoldCo();
            RemoveFlagState(ETouchInputState.Drag);
        }

        protected float CalculateDistanceBetweenVectors(Vector2 pPosOne, Vector2 pPosTwo)
        {
            float distance = Vector2.Distance(pPosOne, pPosTwo);
            return distance;
        }
        #endregion

        #region Input Flag State Enum Functions (Checks, Toggles, Clear, etc)
        protected bool CheckInputHasFlag(ETouchInputState pRequiredInputState)
        {
            return (_currentInputStates & pRequiredInputState) == pRequiredInputState;
        }

        protected void SetFlagState(ETouchInputState pInputState)
        {
            _currentInputStates |= pInputState;
        }

        protected void RemoveFlagState(ETouchInputState pInputState)
        {
            _currentInputStates = _currentInputStates & (~pInputState);
        }

        protected void ToggleNoneState(bool pIsInNoState)
        {
            //If we are not in any state (Not touching the screen that interacts with this map input) we'll set ourself to only none flag
            //Otherwise, we'll remove the none flag
            if (pIsInNoState == true)
            {
                _currentInputStates = ETouchInputState.None;
            }
            else
            {
                RemoveFlagState(ETouchInputState.None);
            }
        }
        #endregion

        #region Bool Checks for Fingers Over UI On Began Click
        protected bool IsFingerBeganOverUIObject(Touch pTouch)
        {
            // Check if the mouse was clicked over a UI element
            if (EventSystem.current.IsPointerOverGameObject(pTouch.fingerId))
            {
                Debug.Log("Clicked on the UI");
                return true;
            }

            return false;
        }

        protected bool IsEtherFingerOnBeganOverUIObject()
        {
            if(_fingersOnBeganOverUIObject[0] == true || _fingersOnBeganOverUIObject[1] == true)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
