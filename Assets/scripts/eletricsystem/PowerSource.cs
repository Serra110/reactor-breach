using UnityEngine;
using System.Collections;

public class PowerSource : EletricUnit
{
  public Port outputPort;
  public float refreshRate = 5;
  public int currentOutput;
  public int maxOutput;
  public bool isGeneratingPower;

  private WaitForSeconds _refreshWait;
  private Coroutine _refreshRoutine;

  private void Start()
  {
      // FIX: o WaitForSeconds era alocado a cada volta do while(true).
      // Hoist para um unico objeto reutilizado (WaitForSeconds e um YieldInstruction
      // sem estado, por isso pode ser reusado em seguranca).
      _refreshWait = new WaitForSeconds(Mathf.Max(0.05f, refreshRate));
      _refreshRoutine = StartCoroutine(refreshCoroutine());
  }

  private void OnDestroy()
  {
      // FIX: sem isto a coroutine continuava a correr depois do objeto ser
      // destruido, escrevendo em ports Mortos.
      if (_refreshRoutine != null)
      {
          StopCoroutine(_refreshRoutine);
          _refreshRoutine = null;
      }
  }


IEnumerator refreshCoroutine()
    {
        while (true)
        {
            OnPowerGenerationStateChanged(CanGeneratePower());

            if (outputPort != null)
                outputPort.SetValue(currentOutput);

            if (DebugFlags.electricLogs) Debug.Log($"[PowerSource] generating={isGeneratingPower} output={currentOutput}");
            yield return _refreshWait;
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
