using UnityEngine;
using System.Collections;

public class PowerSource : EletricUnit
{
  public Port outputPort;
  public float refreshRate = 5;
  public int currentOutput;
  public int maxOutput;
  public bool isGeneratingPower;

  private void Start()
  {
      StartCoroutine(refreshCoroutine());
  } 
 

IEnumerator refreshCoroutine()
    {
        while (true)
        {
            OnPowerGenerationStateChanged(CanGeneratePower());

            if (outputPort != null)
                outputPort.SetValue(currentOutput);

            Debug.Log($"[PowerSource] generating={isGeneratingPower} output={currentOutput}");
            yield return new WaitForSeconds(refreshRate);
        }
    }


    public virtual bool CanGeneratePower()
    {
        return true;
    }

    public virtual void OnPowerGenerationStateChanged(bool newState)
    {
        if(newState == isGeneratingPower)
        {
            return;
        }
        isGeneratingPower = newState;

        currentOutput = isGeneratingPower ? maxOutput : 0;

        if (outputPort != null)
            outputPort.SetValue(currentOutput);
    }


}
