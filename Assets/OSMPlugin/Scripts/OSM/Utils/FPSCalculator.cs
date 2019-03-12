using UnityEngine;
using UnityEngine.UI;

public class FPSCalculator : MonoBehaviourSingleton<FPSCalculator>
{
	private const int UPDATE_RATE = 30;

	public Text fpsText;

	private float deltaTime = 0.0f;
	private float fps = 0;

	private int frequency = UPDATE_RATE;

	public bool isEnabled = false;

	void Update()
	{
		if (isEnabled)
		{
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
			fps = 1.0f / deltaTime;

			if (frequency >= UPDATE_RATE)
			{
				frequency = 0;
				fpsText.text = string.Format("{0:0.} fps", fps);
			}

			frequency++;
		}
	}
}