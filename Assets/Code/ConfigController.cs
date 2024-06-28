using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private bool _isFetching;
    private bool _isSaving;

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
            if (_isFetching)
            {
                var msg = args.Data;

                if (msg.StartsWith("/*") && msg.EndsWith("*/"))
                {
                    msg = msg.Remove(0, 2);
                    msg = msg.Remove(msg.Length - 2, 2);

                    var data = msg.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                    for (var i = 0; i < data.Count; i++)
                    {
                        data[i] = data[i].Replace('.', ',');
                    }

                    m_LoadingPanel.SetActive(false);

                    _isFetching = false;

                    if (data.Count == 1)
                    {
                        m_MainHeightInputField.text = data[0];
                    }
                    else
                    {
                        print("Invalid data length: " + data.Count);

                        PanelsManager.Instance.SetPanelActive(PanelType.Config, false);
                    }
                }
            }
            else if (_isSaving)
            {
                var msg = args.Data;

                if (msg.StartsWith("\\"))
                {
                    var cmd = msg[1..];

                    if (cmd == "config-set-finish")
                    {
                        _isSaving = false;

                        m_LoadingPanel.SetActive(false);
                    }
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isFetching)
            {
                print("Disconnected while fetching config. Aborting...");

                _isFetching = false;

                PanelsManager.Instance.SetPanelActive(PanelType.Config, false);
            }
            else if (_isSaving)
            {
                print("Disconnected while saving config. Aborting...");

                _isSaving = false;

                PanelsManager.Instance.SetPanelActive(PanelType.Config, false);
            }
        };
    }

    private void FetchData()
    {
        m_LoadingPanel.SetActive(true);

        _isFetching = true;

        SerialCommunication.Instance.SerialPortWrite($"\\config-get");
    }

    private void SaveData()
    {
        m_LoadingPanel.SetActive(true);

        _isSaving = true;

        SerialCommunication.Instance.SerialPortWrite($"\\config-set-start {GetValue(m_MainHeightInputField)}");
    }

    private int GetValue(TMP_InputField inputField)
    {
        return !string.IsNullOrEmpty(inputField.text) ? int.Parse(inputField.text) : 0;
    }
}