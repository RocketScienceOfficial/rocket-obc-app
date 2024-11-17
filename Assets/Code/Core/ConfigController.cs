using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigController : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_MainHeightInputField;
    [SerializeField] private Button m_OpenButton;
    [SerializeField] private Button m_ExitButton;
    [SerializeField] private Button m_SaveButton;
    [SerializeField] private GameObject m_LoadingPanel;

    private bool _inConfig;

    private void Start()
    {
        m_OpenButton.onClick.AddListener(() =>
        {
            FetchData();

            PanelsManager.Instance.SetPanelActive(PanelType.Config, true);
        });

        m_ExitButton.onClick.AddListener(() =>
        {
            PanelsManager.Instance.SetPanelActive(PanelType.Config, false);
        });

        m_SaveButton.onClick.AddListener(() =>
        {
            SaveData();
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            var msg = args.Frame;

            if (_inConfig)
            {
                if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_CONFIG_GET_ACK)
                {
                    var payload = BytesConverter.FromBytes<DataLinkFrameConfigGet>(msg.payload);

                    m_LoadingPanel.SetActive(false);

                    m_MainHeightInputField.text = payload.mainHeight.ToString();

                    _inConfig = false;
                }
                else if (msg.msgId == DataLinkMessageType.DATALINK_MESSAGE_CONFIG_SET_ACK)
                {
                    m_LoadingPanel.SetActive(false);

                    _inConfig = false;
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_inConfig)
            {
                print("Disconnected while doing config. Aborting...");

                _inConfig = false;

                PanelsManager.Instance.SetPanelActive(PanelType.Config, false);
            }
        };
    }

    private void FetchData()
    {
        m_LoadingPanel.SetActive(true);

        _inConfig = true;

        SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
        {
            msgId = DataLinkMessageType.DATALINK_MESSAGE_CONFIG_GET,
        });
    }

    private void SaveData()
    {
        m_LoadingPanel.SetActive(true);

        _inConfig = true;

        SerialCommunication.Instance.SerialPortWrite(new DataLinkFrame
        {
            msgId = DataLinkMessageType.DATALINK_MESSAGE_CONFIG_SET,
            payload = BytesConverter.GetBytes(new DataLinkFrameConfigSet
            {
                mainHeight = (ushort)GetValue(m_MainHeightInputField),
            })
        });
    }

    private int GetValue(TMP_InputField inputField)
    {
        return !string.IsNullOrEmpty(inputField.text) ? int.Parse(inputField.text) : 0;
    }
}