using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadController : MonoBehaviour
{
    private const string DOWNLOADS_DIR = "Downloads";

    [SerializeField] private Button m_DownloadButton;
    [SerializeField] private Image m_ProgressFill;
    [SerializeField] private TextMeshProUGUI m_ProgressText;

    private readonly CSVFile _file = new();
    private readonly List<KMLData> _kmlData = new();
    private bool _isDownloading;
    private int _currentCount;
    private int _totalCount;

    private void Start()
    {
        m_DownloadButton.onClick.AddListener(() =>
        {
            _isDownloading = true;

            _file.Open($"{DOWNLOADS_DIR}/FlightLog_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.csv");

            UpdateProgress();

            PanelsManager.Instance.SetPanelActive(PanelType.Download, true);

            SerialCommunication.Instance.SerialPortWrite("\\data-read-start");
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
                    _file.WriteFileValue(float.Parse(data[21]));
                    _file.WriteFileValue(double.Parse(data[22]));
                    _file.WriteFileValue(double.Parse(data[23]));
                    _file.WriteFileValue(float.Parse(data[24]));
                    _file.WriteFileValue(int.Parse(data[25]));

                    var ignFlags = int.Parse(data[26]);

                    for (int i = 0; i < 8; i++)
                    {
                        var flag = (ignFlags & (1 << i)) >> i;

                        _file.WriteFileValue(flag);
                    }

                    _file.EndLine();

                    var currentKMLData = new KMLData()
                    {
                        lat = double.Parse(data[21]),
                        lon = double.Parse(data[22]),
                        alt = float.Parse(data[23]),
                        altDiv = 1,
                    };

                    if (_kmlData.Count > 0)
                    {
                        var lastData = _kmlData[^1];

                        if (lastData.lat == currentKMLData.lat && lastData.lon == currentKMLData.lon)
                        {
                            lastData.alt = (lastData.alt * lastData.altDiv + currentKMLData.alt) / (lastData.altDiv + 1);
                            lastData.altDiv++;

                            _kmlData[^1] = lastData;
                        }
                        else
                        {
                            _kmlData.Add(currentKMLData);
                        }
                    }
                    else
                    {
                        _kmlData.Add(currentKMLData);
                    }

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

        var kmlStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<kml xmlns=\"http://www.opengis.net/kml/2.2\">\r\n  <Document>\r\n    <Style id=\"s1\">\r\n      <LineStyle>\r\n        <color>ffffffff</color>\r\n        <width>2.5</width>\r\n      </LineStyle>\r\n    </Style>\r\n    <Style id=\"s2\">\r\n      <LineStyle>\r\n        <color>00000000</color>\r\n        <width>2.5</width>\r\n      </LineStyle>\r\n      <PolyStyle>\r\n        <color>7f171717</color>\r\n      </PolyStyle>\r\n    </Style>\r\n    <Placemark>\r\n      <styleUrl>#s1</styleUrl>\r\n      <LineString>\r\n        <extrude>0</extrude>\r\n        <altitudeMode>absolute</altitudeMode>\r\n        <coordinates>{DATA}</coordinates>\r\n      </LineString>\r\n    </Placemark>\r\n    <Placemark>\r\n      <styleUrl>#s2</styleUrl>\r\n      <LineString>\r\n        <extrude>1</extrude>\r\n        <altitudeMode>absolute</altitudeMode>\r\n        <coordinates>{DATA}</coordinates>\r\n      </LineString>\r\n    </Placemark>\r\n  </Document>\r\n</kml>";
        var newKml = kmlStr.Replace("{DATA}", string.Join("", _kmlData.Select(d => $"\r\n            {d.lon.ToString().Replace(',', '.')},{d.lat.ToString().Replace(',', '.')},{d.alt.ToString().Replace(',', '.')}")));

        using (var kmlFile = new StreamWriter($"{DOWNLOADS_DIR}/FlightKML_{DateTime.Now:yyyy-dd-MM--HH-mm-ss}.kml"))
        {
            kmlFile.Write(newKml);
        }

        _kmlData.Clear();

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

    private struct KMLData
    {
        public double lat;
        public double lon;
        public float alt;
        public int altDiv;
    }
}