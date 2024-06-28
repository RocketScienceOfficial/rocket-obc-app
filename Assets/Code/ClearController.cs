using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClearController : MonoBehaviour
{
    private const float SPEED = 15.0f;

    [SerializeField] private Button m_ClearButton;
    [SerializeField] private Button m_ConfirmButton;
    [SerializeField] private Button m_CancelButton;
    [SerializeField] private Image m_ProgressFill;
    [SerializeField] private TextMeshProUGUI m_ProgressText;
    [SerializeField] private GameObject m_ConfirmPanel;
    [SerializeField] private GameObject m_InfoPanel;

    private bool _isClearing;
    private float _totalPercentage;
    private float _currentProgress;

    private void Start()
    {
        m_ClearButton.onClick.AddListener(() =>
        {
            m_ConfirmPanel.SetActive(true);
            m_InfoPanel.SetActive(false);

            PanelsManager.Instance.SetPanelActive(PanelType.Clear, true);
        });
        
        m_ConfirmButton.onClick.AddListener(() =>
        {
            m_ConfirmPanel.SetActive(false);
            m_InfoPanel.SetActive(true);

            _isClearing = true;
            _totalPercentage = 0;
            _currentProgress = 0;

            UpdateProgress();

            SerialCommunication.Instance.SerialPortWrite("\\data-clear-start");
        });

        m_CancelButton.onClick.AddListener(() =>
        {
            FinishClearing();
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isClearing)
            {
                var msg = args.Data;

                if (msg.StartsWith("\\"))
                {
                    var cmd = msg[1..];

                    if (cmd == "data-clear-finish")
                    {
                        FinishClearing();
                    }
                }
                else if (msg.StartsWith("/*") && msg.EndsWith("*/"))
                {
                    msg = msg.Remove(0, 2);
                    msg = msg.Remove(msg.Length - 2, 2);

                    var data = msg.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                    for (var i = 0; i < data.Count; i++)
                    {
                        data[i] = data[i].Replace('.', ',');
                    }

                    if (data.Count == 1)
                    {
                        _totalPercentage = float.Parse(data[0]);
                    }
                    else
                    {
                        print("Invalid data length: " + data.Count);

                        FinishClearing();
                    }
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isClearing)
            {
                print("Disconnected while clearing. Aborting...");

                FinishClearing();
            }
        };
    }

    private void Update()
    {
        if (_isClearing)
        {
            if (_currentProgress < _totalPercentage)
            {
                _currentProgress += Time.deltaTime * SPEED;
                _currentProgress = Mathf.Clamp(_currentProgress, 0, 100);

                UpdateProgress();
            }
        }
    }

    private void FinishClearing()
    {
        _isClearing = false;
        _totalPercentage = 0;
        _currentProgress = 0;

        PanelsManager.Instance.SetPanelActive(PanelType.Clear, false);
    }

    private void UpdateProgress()
    {
        m_ProgressFill.fillAmount = _currentProgress / 100.0f;
        m_ProgressText.SetText($"{(int)_currentProgress}%");
    }
}