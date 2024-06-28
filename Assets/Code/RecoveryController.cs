using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecoveryController : MonoBehaviour
{
    [SerializeField] private Button m_RecoveryButton;
    [SerializeField] private TextMeshProUGUI m_ProgressText;

    private readonly CSVFile _file = new();
    private bool _isRecovering;
    private int _currentCount;

    private void Start()
    {
        m_RecoveryButton.onClick.AddListener(() =>
        {
            _isRecovering = true;

            _file.Open($"Recovery/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");

            UpdateProgress();

            PanelsManager.Instance.SetPanelActive(PanelType.Recovery, true);

            SerialCommunication.Instance.SerialPortWrite("\\data-recovery-start");
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isRecovering)
            {
                var msg = args.Data;
                var data = new List<string>();

                if (!string.IsNullOrEmpty(msg))
                {
                    if (msg.StartsWith("/*") && msg.EndsWith("*/"))
                    {
                        msg = msg.Remove(0, 2);
                        msg = msg.Remove(msg.Length - 2, 2);

                        data = msg.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                        for (var i = 0; i < data.Count; i++)
                        {
                            data[i] = data[i].Replace('.', ',');
                        }
                    }
                    else
                    {
                        if (msg.StartsWith("\\"))
                        {
                            var cmd = args.Data[1..];

                            if (cmd == "data-recovery-finish")
                            {
                                FinishRecovering();
                            }
                        }

                        return;
                    }
                }
                else
                {
                    return;
                }

                if (data.Count == 26)
                {
                    _file.WriteFileValue(uint.Parse(data[0]));
                    _file.WriteFileValue(float.Parse(data[1]));
                    _file.WriteFileValue(float.Parse(data[2]));
                    _file.WriteFileValue(float.Parse(data[3]));
                    _file.WriteFileValue(float.Parse(data[4]));
                    _file.WriteFileValue(float.Parse(data[5]));
                    _file.WriteFileValue(float.Parse(data[6]));
                    _file.WriteFileValue(float.Parse(data[7]));
                    _file.WriteFileValue(float.Parse(data[8]));
                    _file.WriteFileValue(float.Parse(data[9]));
                    _file.WriteFileValue(float.Parse(data[10]));
                    _file.WriteFileValue(float.Parse(data[11]));
                    _file.WriteFileValue(float.Parse(data[12]));
                    _file.WriteFileValue(float.Parse(data[13]));
                    _file.WriteFileValue(float.Parse(data[14]));
                    _file.WriteFileValue(float.Parse(data[15]));
                    _file.WriteFileValue(float.Parse(data[16]));
                    _file.WriteFileValue(float.Parse(data[17]));
                    _file.WriteFileValue(float.Parse(data[18]));
                    _file.WriteFileValue(int.Parse(data[19]));
                    _file.WriteFileValue(float.Parse(data[20]));
                    _file.WriteFileValue(double.Parse(data[21]));
                    _file.WriteFileValue(double.Parse(data[22]));
                    _file.WriteFileValue(float.Parse(data[23]));
                    _file.WriteFileValue(int.Parse(data[24]));

                    var ignFlags = int.Parse(data[25]);

                    for (int i = 0; i < 8; i++)
                    {
                        var flag = (ignFlags & (1 << i)) >> i;

                        _file.WriteFileValue(flag);
                    }

                    _file.EndLine();

                    _currentCount++;

                    UpdateProgress();
                }
                else
                {
                    print("Invalid data length: " + data.Count);

                    FinishRecovering();
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isRecovering)
            {
                print("Disconnected while recovering data. Aborting...");

                FinishRecovering();
            }
        };
    }

    private void FinishRecovering()
    {
        _file.Close();

        _isRecovering = false;
        _currentCount = 0;

        PanelsManager.Instance.SetPanelActive(PanelType.Recovery, false);

        print("Finished recovering data!");
    }

    private void UpdateProgress()
    {
        m_ProgressText.SetText($"{_currentCount} frames recovered");
    }
}