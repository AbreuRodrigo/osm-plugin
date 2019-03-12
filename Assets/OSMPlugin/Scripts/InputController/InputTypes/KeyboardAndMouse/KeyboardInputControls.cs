using UnityEngine;

namespace InputControls {
    public class KeyboardInputControls : BaseInputControls {

        [Header("Button Setup")]
        [SerializeField]
        KeyCode _pinchOutKeyCode = KeyCode.Q;
        [SerializeField]
        KeyCode _pinchInKeyCode = KeyCode.W;
        [SerializeField]
        KeyCode _pinchZeroOutKeyCode = KeyCode.E;
        [SerializeField]
        KeyCode _twistLeftKeyCode = KeyCode.A;
        [SerializeField]
        KeyCode _twistRightKeyCode = KeyCode.S;
        [SerializeField]
        KeyCode _twistZeroOutKeyCode = KeyCode.D;
        [SerializeField]
        KeyCode _pressKeyCode = KeyCode.Mouse0;

        float _incrementPinchValue = 1;
        float _incrementTwistValue = 25;

        protected override void OnInputTouch() {
            //click down
            if (Input.GetKeyDown(_pressKeyCode)) {

            }
            //hold click down
            else if (Input.GetKey(_pressKeyCode)) {

            }

            //Twists (rotations)
            if (Input.GetKey(_twistLeftKeyCode)) {
                InvokeTwistEventFromDerived(AdjustFloatValue(-_incrementTwistValue));
            }

            else if (Input.GetKey(_twistRightKeyCode)) {
                InvokeTwistEventFromDerived(AdjustFloatValue(_incrementTwistValue));
            }

            else if (Input.GetKeyDown(_twistZeroOutKeyCode)) {
                InvokeTwistEventFromDerived(AdjustFloatValue(0));
            }

            //Pinches (Zoom in/out)
            if (Input.GetKey(_pinchOutKeyCode)) {
                InvokePinchEventFromDerived(AdjustFloatValue(_incrementPinchValue));
            }

            else if (Input.GetKey(_pinchInKeyCode)) {
                InvokePinchEventFromDerived(AdjustFloatValue(-_incrementPinchValue));
            }

            else if (Input.GetKeyDown(_pinchZeroOutKeyCode)) {
                InvokePinchEventFromDerived(AdjustFloatValue(0));
            }
        }

        float AdjustFloatValue(float pIncrementedBy) {
            return pIncrementedBy;
        }

    }
}
