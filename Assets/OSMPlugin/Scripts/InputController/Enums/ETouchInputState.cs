using System;

namespace InputControls{
    
    [Flags]
    public enum ETouchInputState
    {
        None = 0x00,
        TwoFingerVerticalSwipe = 0x01,
        Pinch = 0x02,
        Twist = 0x04,
        Drag = 0x08,
        OneFingerHold = 0x10,
    }

}