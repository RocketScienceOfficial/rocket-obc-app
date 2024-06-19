using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadController : MonoBehaviour
{
    [SerializeField] private Button m_DownloadButton;
    [SerializeField] private Image m_ProgressFill;
    [SerializeField] private TextMeshProUGUI m_ProgressText;

    private readonly CSVFile _file = new();
    private bool _isDownloading;
    private int _currentCount;
    private int _totalCount;

    private void Start()
    {
        m_DownloadButton.onClick.AddListener(() =>
        {
            _isDownloading = true;

            _file.Open($"Downloads/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");

            UpdateProgress();

            PanelsManager.Instance.SetPanelActive(PanelType.Download, true);

            SerialCommunication.Instance.SerialPortWrite("data-read-start");
        });

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            if (_isDownloading)
            {
                var msg = args.Data;
                var data = new List<string>();

                if (!string.IsNullOrEmpty(msg) && msg.StartsWith("/*") && msg.EndsWith("*/"))
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
                    return;
                }

                if (data.Count == 1)
                {
                    _totalCount = int.Parse(data[0]);

                    print("New total count is: " + _totalCount);

                    if (_totalCount == 0)
                    {
                        print("Received count is 0. Aborting...");

                        FinishDownloading();
                    }
                    else
                    {
                        UpdateProgress();
                    }
                }
                else if (data.Count == 26)
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

                    for (int i = 7; i >= 0; i--)
                    {
                        var flag = (ignFlags & (1 << i)) >> i;

                        _file.WriteFileValue(flag);
                    }

                    _file.EndLine();

                    _currentCount++;

                    if (_totalCount == _currentCount)
                    {
                        print("Received all frames!");

                        FinishDownloading();
                    }
                    else
                    {
                        UpdateProgress();
                    }
                }
                else
                {
                    print("Invalid data length: " + data.Count);

                    FinishDownloading();
                }
            }
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isDownloading)
            {
                print("Disconnected while downloading. Aborting...");

                FinishDownloading();
            }
        };
    }

    private void FinishDownloading()
    {
        _file.Close();

        _isDownloading = false;
        _currentCount = 0;
        _totalCount = 0;

        PanelsManager.Instance.SetPanelActive(PanelType.Download, false);

        print("Finished downloading data!");
    }

    private void UpdateProgress()
    {
        m_ProgressFill.fillAmount = _isDownloading ? (float)_currentCount / Mathf.Max(_totalCount, 1) : 0;
        m_ProgressText.SetText(_totalCount != 0 ? $"{_currentCount} / {_totalCount}" : "Loading...");
    }
}