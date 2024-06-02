using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelController : MonoBehaviour
{
    private const string FILES_DIR = "Downloaded/";

    [Header("General")]
    [SerializeField] private TextMeshProUGUI m_PortText;

    [Header("Firmware")]
    [SerializeField] private Button m_DownloadButton;

    private readonly StringBuilder _csvBuilder = new();
    private StreamWriter _writer;
    private bool _isDownloading = false;
    private int _count = -1;
    private int _currentCount = 0;


    private void Start()
    {
        SerialCommunication.Instance.OnConnected += (sender, args) =>
        {
            LoadPortName();
            UpdateDownloadButton();
        };

        SerialCommunication.Instance.OnDisconnected += (sender, args) =>
        {
            if (_isDownloading)
            {
                FinishDownloading();
            }
        };

        SerialCommunication.Instance.OnRead += (sender, args) =>
        {
            try
            {
                if (_isDownloading)
                {
                    var data = ParseData(args.Data);

                    if (data == null)
                    {
                        return;
                    }

                    if (_count == -1)
                    {
                        if (data.Length != 1)
                        {
                            FinishDownloading();

                            return;
                        }

                        _count = int.Parse(data[0]);
                    }
                    else
                    {
                        if (data.Length != 25)
                        {
                            FinishDownloading();

                            return;
                        }

                        _currentCount++;

                        WriteFileValue(uint.Parse(data[0]));
                        WriteFileValue(float.Parse(data[1]));
                        WriteFileValue(float.Parse(data[2]));
                        WriteFileValue(float.Parse(data[3]));
                        WriteFileValue(float.Parse(data[4]));
                        WriteFileValue(float.Parse(data[5]));
                        WriteFileValue(float.Parse(data[6]));
                        WriteFileValue(float.Parse(data[7]));
                        WriteFileValue(float.Parse(data[8]));
                        WriteFileValue(float.Parse(data[9]));
                        WriteFileValue(float.Parse(data[10]));
                        WriteFileValue(float.Parse(data[11]));
                        WriteFileValue(float.Parse(data[12]));
                        WriteFileValue(float.Parse(data[13]));
                        WriteFileValue(float.Parse(data[14]));
                        WriteFileValue(float.Parse(data[15]));
                        WriteFileValue(float.Parse(data[16]));
                        WriteFileValue(float.Parse(data[17]));
                        WriteFileValue(float.Parse(data[18]));
                        WriteFileValue(int.Parse(data[19]));
                        WriteFileValue(float.Parse(data[20]));
                        WriteFileValue(double.Parse(data[21]));
                        WriteFileValue(double.Parse(data[22]));
                        WriteFileValue(float.Parse(data[23]));
                        WriteFileValue(int.Parse(data[24]));

                        _writer.WriteLine(_csvBuilder);
                        _csvBuilder.Clear();

                        if (_count == _currentCount)
                        {
                            FinishDownloading();
                        }

                        UpdateDownloadButton();
                    }
                }
            }
            catch (Exception ex)
            {
                print(ex.Message);

                FinishDownloading();
            }
        };


        m_DownloadButton.onClick.AddListener(() =>
        {
            _isDownloading = true;

            UpdateDownloadButton();

            var path = FILES_DIR + $"FlightLog_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.csv";

            EnsureDirectoryExists(path);

            _writer = new StreamWriter(path);

            SerialCommunication.Instance.SerialPortWrite("data-read");
        });
    }


    private void LoadPortName()
    {
        m_PortText.SetText(SerialCommunication.Instance.PortName);
    }

    private void UpdateDownloadButton()
    {
        m_DownloadButton.enabled = !_isDownloading;
        m_DownloadButton.transform.Find("Fill").GetComponent<Image>().color = !_isDownloading ? new Color(0.3921568f, 1.0f, 0.4287225f) : new Color(0.3921568f, 0.8024775f, 1.0f);
        m_DownloadButton.transform.Find("Fill").GetComponent<Image>().fillAmount = !_isDownloading ? 1 : (float)_currentCount / _count;
    }

    private void FinishDownloading()
    {
        _writer.Close();
        _writer = null;

        _isDownloading = false;
        _count = -1;
        _currentCount = 0;

        print("Finished downloading data!");

        UpdateDownloadButton();
    }

    private void WriteFileValue<T>(T value) where T : IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
    {
        _csvBuilder.Append(value.ToString().Replace(',', '.'));
        _csvBuilder.Append(',');
    }


    private static void EnsureDirectoryExists(string filePath)
    {
        var fi = new FileInfo(filePath);

        if (!fi.Directory.Exists)
        {
            Directory.CreateDirectory(fi.DirectoryName);
        }
    }

    private static string[] ParseData(string msg)
    {
        if (string.IsNullOrEmpty(msg))
        {
            return null;
        }

        try
        {
            var result = new List<string>();

            if (msg.StartsWith("/*") && msg.EndsWith("*/"))
            {
                msg = msg.Remove(0, 2);
                msg = msg.Remove(msg.Length - 2, 2);

                var data = msg.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = data[i].Replace('.', ',');
                }

                return data;
            }
        }
        catch (Exception ex)
        {
            print(ex.Message);

            return null;
        }

        return null;
    }
}