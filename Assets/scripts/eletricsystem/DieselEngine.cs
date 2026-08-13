using UnityEngine;

public class DieselEngine : PowerSource
{
    public float vibrationAmount = 0.01f;
    public float vibrationSpeed = 18f;

    private Vector3 baseLocalPosition;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!isGeneratingPower)
        {
            if (transform.localPosition != baseLocalPosition)
                transform.localPosition = baseLocalPosition;
            return;
        }

        float vibration = Mathf.Sin(Time.time * vibrationSpeed) * vibrationAmount;
        float secondaryVibration = Mathf.Sin(Time.time * (vibrationSpeed * 1.37f)) * vibrationAmount * 0.5f;
        transform.localPosition = baseLocalPosition + new Vector3(vibration, secondaryVibration, 0f);
    }

    public override bool CanGeneratePower()
    {
        return base.CanGeneratePower();
    }

    public override void OnPowerGenerationStateChanged(bool newState)
    {
        if (newState == isGeneratingPower)
        {
            return;
        }

        isGeneratingPower = newState;

        if (isGeneratingPower)
        {
            currentOutput = maxOutput;
        }
        else
        {
            currentOutput = 0;
        }

        if (outputPort != null)
            outputPort.SetValue(currentOutput);
    }
}
