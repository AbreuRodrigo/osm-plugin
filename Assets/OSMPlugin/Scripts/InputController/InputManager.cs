using UnityEngine;

namespace InputControls
{
    public class InputManager : MonoBehaviour
	{
        [Header("Test Settings")]
        [SerializeField]
        bool _isUsingKeyboardAndMouse = false;

        public IInput InputControls { get; private set; }

        public static InputManager Instance {
            get {
                if(_instance == null) {
                    _instance = FindObjectOfType<InputManager>();
                }
                return _instance;
            }
        }

        static InputManager _instance;

        void Awake()
		{
            if(Instance != this)
			{
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);

            //Testing only with component already on object
            InputControls = GetComponent<BaseInputControls>();

            //Attach script if not already on object
            if(GetComponent<DeviceRotation>() == null)
            {
                gameObject.AddComponent<DeviceRotation>();
            }

           // SetInputType(EInputType.MapTouchScreen);
        }

        void SetInputType(EInputType inputType)
		{
            switch (inputType)
			{
                case EInputType.MapTouchScreen:
                    InputControls = gameObject.AddComponent<MapInputControls>();
                    break;

                case EInputType.TouchScreenGeneric:
                    InputControls = gameObject.AddComponent<TouchScreenInputControls>();
                    break;

                case EInputType.KeyboardAndMouse:
                    InputControls = gameObject.AddComponent<KeyboardInputControls>();
                    break;

                default:
                    InputControls = gameObject.AddComponent<TouchScreenInputControls>();
                    break;
            }
        }
    }
}