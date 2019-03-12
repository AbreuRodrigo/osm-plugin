using EventManager = TSG.Core.EventSystem.EventManager;
using UnityEngine;

namespace InputControls
{ 
    public class DeviceRotation : MonoBehaviour
	{

        public static DeviceRotation Instance
		{
            get
			{
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DeviceRotation>();
                }
                return _instance;
            }
        }

        protected static DeviceRotation _instance;

        private void Awake()
        {
            if (Instance != this)
            {
                Destroy(this);
            }
        }

        private static bool gyroInitialized = false;

        private void Update()
        {
            EventManager.instance.SendEvent<Quaternion>(UserInputEventIds.GYROSCOPE_ROTATION_EVENT_ID, GetRotation());
        }

        public static bool HasGyroscope
		{
            get
			{
                return SystemInfo.supportsGyroscope;
            }
        }

        public static Quaternion GetAttitudeRaw()
        {
            return Input.gyro.attitude;
        }

        public static Quaternion GetRotation()
        {
            if (!gyroInitialized)
            {
                InitGyro();
            }


            return HasGyroscope ? ReadGyroscopeRotation() : Quaternion.identity;
        }

        private static void InitGyro()
        {
            if (HasGyroscope)
            {
                Input.gyro.enabled = true;                // enable the gyroscope
            }
            gyroInitialized = true;
        }


        private static Quaternion ReadGyroscopeRotation()
        {
            Quaternion gyro = Input.gyro.attitude;
            Vector3 rot = gyro. eulerAngles;

            rot.x = 0; //is X up while the phone is flat? Z is up in THIS game world.
            rot.y = rot.z - 50;
            rot.z = 0;

            return Quaternion.Euler(-rot);
        }
    }
}
