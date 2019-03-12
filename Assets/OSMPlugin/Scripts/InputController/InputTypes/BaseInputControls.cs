using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InputControls {

    public delegate void OnTwistHandler(float pRotationAngle);
    public delegate void OnPinchHandler(float pPinchDistance);
    public delegate void OnTapHandler(double pX, double pY);
    public delegate void OnDragHandler(double pX, double pY);
    public delegate void OnTwoFingerVerticalSwipeHandler(float pValue);
    public delegate void OnHoldTapHandler();

    public abstract class BaseInputControls : MonoBehaviour, IInput {

        [Header("Tap Settings")]
        [SerializeField]
        float _timeUntilTapBecomesHold = 1;

        [Header("Drag Settings")]
        [SerializeField]
        float _distanceFromTouchPosToBeADrag = 50;

        public float TimeUntilTapBecomesHold {
            get { return _timeUntilTapBecomesHold; }
            set { _timeUntilTapBecomesHold = value; }
        }

        public float DistanceFromTouchPosToBeADrag {
            get { return _distanceFromTouchPosToBeADrag; }
            set { _distanceFromTouchPosToBeADrag = value; }
        }

        public int RegisteredTouchCount { get; set; }
        public GameObject InteractedObject { get; set; }

        public event OnTwistHandler OnTwistEvent;
        public event OnPinchHandler OnPinchEvent;
        public event OnTapHandler OnTapEvent;
        public event OnHoldTapHandler OnHoldTapEvent;
        public event OnDragHandler OnDragEvent;
        public event OnTwoFingerVerticalSwipeHandler OnTwoFingerVerticalSwipeEvent;

        protected abstract void OnInputTouch();

        void Update()
        {
            OnInputTouch();
        }

        #region Invoke Wrappers For Derived Class' Functions
        protected void InvokePinchEventFromDerived(float pDistance) {
            OnPinchEvent?.Invoke(pDistance);
        }

        protected void InvokeTwistEventFromDerived(float pRotation) {
            OnTwistEvent?.Invoke(pRotation);
        }

        protected void InvokeTapEventFromDerived(double pX, double pY) {
            OnTapEvent?.Invoke(pX, pY);
        }

        protected void InvokeHoldEventFromDerived()
        {
            OnHoldTapEvent?.Invoke();
        }

        protected void InvokeDragEventFromDerived(double pX, double pY)
        {
            OnDragEvent?.Invoke(pX, pY);
        }

        protected void InvokeVerticalSwipeEventFromDerived(float pValue)
        {
            OnTwoFingerVerticalSwipeEvent?.Invoke(pValue);
        }
        #endregion


    }
}
