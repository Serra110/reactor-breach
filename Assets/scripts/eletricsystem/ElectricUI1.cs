using UnityEngine;

public class ElectricUI1 : MonoBehaviour
{

    public static ElectricUI1 instance;

    private void Awake()
    {
        instance = this;
    }
    [Header("Device Name Panel Properties")]
    public GameObject deviceNamePanel;
    public TMPro.TextMeshProUGUI deviceNameText;


    [Header("Port Data Panel")]
    public GameObject portDataPanelPanel;
    public TMPro.TextMeshProUGUI portNameText;
    public TMPro.TextMeshProUGUI portValueText;


    [Header("Storage Data Panel")]
    public GameObject storageDataPanel;
    public TMPro.TextMeshProUGUI storageNameText;
    public TMPro.TextMeshProUGUI storageCapacityText;
    public TMPro.TextMeshProUGUI storageMaxOutputText;




    public void HideAll()
    {
        HideDeviceNamePanel();
        HidePortDataPanel();
        HideStorageDataPanel();
    }


    public void ShowDeviceName(string deviceName)
    {
        HideAll();
        deviceNamePanel.SetActive(true);
        deviceNameText.text = deviceName;
    }

    public void HideDeviceNamePanel()
    {
        deviceNamePanel.SetActive(false);
    }

    public void ShowPortData(string portName, int portValue)
    {
        HideAll();
        portDataPanelPanel.SetActive(true);
        portNameText.text = portName;
        portValueText.text = portValue.ToString();
    }

    public void UpdatePortValue(int portValue)
    {
        if (portDataPanelPanel != null && portDataPanelPanel.activeSelf && portValueText != null)
            portValueText.text = portValue.ToString();
    }

    public void HidePortDataPanel()
    {
        portDataPanelPanel.SetActive(false);
    }

    public void ShowStorageDataPanel(string storageName, string currentStorageAmount, string maxStorageCapacity, string storageMaxOutput)
    {
        HideAll();
        storageDataPanel.SetActive(true);
        storageNameText.text = storageName;
        storageCapacityText.text = $"{currentStorageAmount}/{maxStorageCapacity}";
        storageMaxOutputText.gameObject.SetActive(true);
        storageMaxOutputText.text = $"Max Output: {storageMaxOutput}";
    }

    public void ShowBatteryDataPanel(string storageName, string currentStorageAmount, string maxStorageCapacity)
    {
        HideAll();
        storageDataPanel.SetActive(true);
        storageNameText.text = storageName;
        storageCapacityText.text = $"{currentStorageAmount}/{maxStorageCapacity}";
        storageMaxOutputText.gameObject.SetActive(false);
    }

    public void HideStorageDataPanel()
    {
        storageDataPanel.SetActive(false);
    }
}
