using UnityEngine;
using UnityEngine.EventSystems;

namespace InputControls {

    public interface IInput {
        event OnTwistHandler OnTwistEvent;
        event OnPinchHandler OnPinchEvent;
        event OnTapHandler OnTapEvent;
        event OnDragHandler OnDragEvent;
        event OnHoldTapHandler OnHoldTapEvent;
        event OnTwoFingerVerticalSwipeHandler OnTwoFingerVerticalSwipeEvent;

        float TimeUntilTapBecomesHold { get; }
        float DistanceFromTouchPosToBeADrag { get; }
        int RegisteredTouchCount { get; }

        GameObject InteractedObject { get; }
    }
}