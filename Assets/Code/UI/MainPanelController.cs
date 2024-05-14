using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private TextMeshProUGUI m_PortText;

    [Header("Firmware")]
    [SerializeField] private GameObject m_FirmwareLoadingPanel;
    [SerializeField] private TextMeshProUGUI m_FirmwareName;
    [SerializeField] private TextMeshProUGUI m_FirmwareVersion;
    [SerializeField] private TextMeshProUGUI m_FirmwareBuildDate;
    [SerializeField] private Button m_UpdateButton;
    [SerializeField] private Button m_DownloadButton;

    [Header("Config")]
    [SerializeField] private GameObject m_ConfigLoadingPanel;
    [SerializeField] private TMP_InputField m_MainParachuteHeightConfig;
    [SerializeField] private TMP_InputField m_LauncherHeightConfig;
    [SerializeField] private TMP_InputField m_SecondIgniterDelayConfig;
    [SerializeField] private TMP_InputField m_ParachuteErrorSpeedConfig;
    [SerializeField] private Button m_LoadConfigButton;
    [SerializeField] private Button m_SaveConfigButton;

    private void Start()
    {
        ConnectionController.OnConnected += (sender, args) =>
        {
            LoadPortName();
            LoadFirmwareInfo();
            LoadConfig();
        };

        ConnectionController.OnDisconnected += (sender, args) =>
        {

        };

        m_LoadConfigButton.onClick.AddListener(LoadConfig);
        m_SaveConfigButton.onClick.AddListener(SaveConfig);
    }

    private void LoadPortName()
    {
        m_PortText.SetText(CommunicationFactory.GetCommunication().GetPortName());
    }

    private void LoadFirmwareInfo()
    {
        m_FirmwareLoadingPanel.SetActive(true);
        m_FirmwareLoadingPanel.SetActive(false);

        m_FirmwareName.SetText("");
        m_FirmwareVersion.SetText("");
        m_FirmwareBuildDate.SetText("");

        var updateAvailable = true;
        var updateVersion = "Ver 1.0.0";

        m_UpdateButton.enabled = updateAvailable;
        m_UpdateButton.GetComponentInChildren<Image>().color = updateAvailable ? new Color(0.4343224f, 1.0f, 0.3915094f) : new Color(0.1886792f, 0.1886792f, 0.1886792f);
        m_UpdateButton.transform.Find("Release Info Text").GetComponent<TextMeshProUGUI>().SetText(updateAvailable ? updateVersion : "Already up to date");

        var downloadAvailable = true;

        m_DownloadButton.enabled = downloadAvailable;
        m_DownloadButton.GetComponentInChildren<Image>().color = downloadAvailable ? new Color(0.3921568f, 0.8024775f, 1.0f) : new Color(0.1886792f, 0.1886792f, 0.1886792f);
        m_DownloadButton.transform.Find("Info Text").GetComponent<TextMeshProUGUI>().SetText(downloadAvailable ? "Available" : "Unavailable");
    }

    private void LoadConfig()
    {
        m_ConfigLoadingPanel.SetActive(true);

        CommunicationFactory.GetCommunication().LoadConfig(data =>
        {
            m_MainParachuteHeightConfig.text = data.parachuteHeight.ToString();
            m_LauncherHeightConfig.text = data.launcherHeight.ToString();
            m_SecondIgniterDelayConfig.text = data.secondDelay.ToString();
            m_ParachuteErrorSpeedConfig.text = data.parachuteErrorSpeed.ToString();

            m_ConfigLoadingPanel.SetActive(false);
        });
    }
    // TODO: Disable communication when click
    private void SaveConfig()
    {
        m_ConfigLoadingPanel.SetActive(true);

        var data = new ConfigData
        {
            parachuteHeight = int.Parse(m_MainParachuteHeightConfig.text),
            launcherHeight = int.Parse(m_LauncherHeightConfig.text),
            secondDelay = int.Parse(m_SecondIgniterDelayConfig.text),
            parachuteErrorSpeed = int.Parse(m_ParachuteErrorSpeedConfig.text),
        };

        CommunicationFactory.GetCommunication().SaveConfig(data, () =>
        {
            m_ConfigLoadingPanel.SetActive(false);
        });
    }
}