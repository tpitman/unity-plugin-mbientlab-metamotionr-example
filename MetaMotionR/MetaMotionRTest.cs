using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MbientLab.MetaWear.Impl;

public class MetawearRTest : MonoBehaviour
{
	public string DeviceName = "MetawearR";

	public Text AccelerometerText;
	public Text StatusText;

	public GameObject TopPanel;
	public GameObject MiddlePanel;

	public class Characteristic
	{
		public string ServiceUUID;
		public string CharacteristicUUID;
		public bool Found;
	}

	public static List<Characteristic> Characteristics = new List<Characteristic>
	{
		new Characteristic { ServiceUUID = "326A9000-85CB-9195-D9DD-464CFBBAE75A", CharacteristicUUID = "326A9001-85CB-9195-D9DD-464CFBBAE75A", Found = false },
		new Characteristic { ServiceUUID = "326A9000-85CB-9195-D9DD-464CFBBAE75A", CharacteristicUUID = "326A9006-85CB-9195-D9DD-464CFBBAE75A", Found = false },
	};

	public Characteristic ReadWriteCharacteristic = Characteristics[0];
	public Characteristic SubscribeCharacteristic = Characteristics[1];

	public bool AllCharacteristicsFound { get { return !(Characteristics.Where (c => c.Found == false).Any ()); } }
	public Characteristic GetCharacteristic (string serviceUUID, string characteristicsUUID)
	{
		return Characteristics.Where (c => IsEqual (serviceUUID, c.ServiceUUID) && IsEqual (characteristicsUUID, c.CharacteristicUUID)).FirstOrDefault ();
	}

	private static List<byte[]> CommandBytesList = new List<byte[]>
	{
		AccelerometerBmi160.Configure(),
		AccelerometerBmi160.DataInterruptStart(),
		AccelerometerBmi160.DataStart(),
		AccelerometerBmi160.Start(),
		AccelerometerBmi160.Stop(),
		AccelerometerBmi160.DataStop(),
		AccelerometerBmi160.DataInterruptStop(),

		//new byte[] { (byte)Module.GYRO, 0x03, 0x26, 0x00 },
		AccelerometerBmi160.Configure(Module.GYRO),
		new byte[] { (byte)Module.GYRO, 0x05, 0x01 },
		new byte[] { (byte)Module.GYRO, 0x02, 0x01, 0x00 },
		new byte[] { (byte)Module.GYRO, 0x01, 0x01 },
		new byte[] { (byte)Module.GYRO, 0x01, 0x00 },
		new byte[] { (byte)Module.GYRO, 0x02, 0x00, 0x00 },
		new byte[] { (byte)Module.GYRO, 0x05, 0x00 },
	};

	private byte[] CommandAccelerometerConfigure = CommandBytesList[0];
	private byte[] CommandAccelerometerEnableDataOutput = CommandBytesList[1];
	private byte[] CommandAccelerometerEnableDataInterrupt = CommandBytesList[2];
	private byte[] CommandAccelerometerEnablePower = CommandBytesList[3];
	private byte[] CommandAccelerometerDisablePower = CommandBytesList[4];
	private byte[] CommandAccelerometerDisableDataInterrupt = CommandBytesList[5];
	private byte[] CommandAccelerometerDisableDataOutput = CommandBytesList[6];

	private byte[] CommandGyroConfigure = CommandBytesList[7];
	private byte[] CommandGyroEnableDataOutput = CommandBytesList[8];
	private byte[] CommandGyroEnableDataInterrupt = CommandBytesList[9];
	private byte[] CommandGyroEnablePower = CommandBytesList[10];
	private byte[] CommandGyroDisablePower = CommandBytesList[11];
	private byte[] CommandGyroDisableDataInterrupt = CommandBytesList[12];
	private byte[] CommandGyroDisableDataOutput = CommandBytesList[13];

	enum States
	{
		None,
		Scan,
		Connect,
		ConfigureAccelerometer,
		SubscribeToAccelerometer,
		SubscribingToAccelerometer,
		Disconnect,
		Disconnecting,
	}

	private bool _connected = false;
	private float _timeout = 0f;
	private States _state = States.None;
	private string _deviceAddress;
	private bool _pairing = false;

	string StatusMessage
	{
		set
		{
			if (!string.IsNullOrEmpty(value))
				BluetoothLEHardwareInterface.Log (value);
			if (StatusText != null)
				StatusText.text = value;
		}
	}

	void Reset ()
	{
		_connected = false;
		_timeout = 0f;
		_state = States.None;
		_deviceAddress = null;
		TopPanel.SetActive (false);
		MiddlePanel.SetActive (false);

		StatusMessage = "";
	}

	void SetState (States newState, float timeout)
	{
		_state = newState;
		_timeout = timeout;
	}

	void StartProcess ()
	{
		Reset ();
		BluetoothLEHardwareInterface.Initialize (true, false, () => {

			SetState (States.Scan, 0.1f);

		}, (error) => {

			BluetoothLEHardwareInterface.Log ("Error: " + error);
		});
	}

	// Use this for initialization
	void Start ()
	{
		StartProcess ();
	}

	// Update is called once per frame
	void Update ()
	{
		if (_timeout > 0f)
		{
			_timeout -= Time.deltaTime;
			if (_timeout <= 0f)
			{
				_timeout = 0f;

				switch (_state)
				{
				case States.None:
					break;

				case States.Scan:
					BluetoothLEHardwareInterface.ScanForPeripheralsWithServices (null, (address, deviceName) => {

						if (deviceName.Contains (DeviceName))
						{
							StatusMessage = "Found a MetaMotion";

							BluetoothLEHardwareInterface.StopScan ();

							TopPanel.SetActive (true);

							// found a device with the name we want
							// this example does not deal with finding more than one
							_deviceAddress = address;
							SetState (States.Connect, 0.5f);
						}

					}, null, true);
					break;

				case States.Connect:
					StatusMessage = "Connecting to MetaMotion...";

					BluetoothLEHardwareInterface.ConnectToPeripheral (_deviceAddress, null, null, (address, serviceUUID, characteristicUUID) => {

						var characteristic = GetCharacteristic (serviceUUID, characteristicUUID);
						if (characteristic != null)
						{
							BluetoothLEHardwareInterface.Log (string.Format ("Found {0}, {1}", serviceUUID, characteristicUUID));

							characteristic.Found = true;

							if (AllCharacteristicsFound)
							{
								_connected = true;
								SetState (States.ConfigureAccelerometer, 0.1f);
							}
						}
					}, (disconnectAddress) => {
						StatusMessage = "Disconnected from MetaMotion";
						Reset ();
						SetState (States.Scan, 1f);
					});
					break;

				case States.ConfigureAccelerometer:

					StatusMessage = "Configuring Accelerometer...";
					BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandAccelerometerConfigure, CommandAccelerometerConfigure.Length, true, (____) => {
						StatusMessage = "Enable data output";
						BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandAccelerometerEnableDataInterrupt, CommandAccelerometerEnableDataInterrupt.Length, true, (_____) => {
							StatusMessage = "Enable data interrupt";
							BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandAccelerometerEnableDataOutput, CommandAccelerometerEnableDataOutput.Length, true, (______) => {
								StatusMessage = "Enable power";
								BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandAccelerometerEnablePower, CommandAccelerometerEnablePower.Length, true, (_______) => {
									StatusMessage = "Accelerometer configured";
									SetState (States.SubscribeToAccelerometer, 0.1f);
								});
							});
						});
					});
					/*
					StatusMessage = "Configuring Gyro...";
					BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandGyroConfigure, CommandGyroConfigure.Length, true, (_) => {
						StatusMessage = "Enable data output";
						BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandGyroEnableDataOutput, CommandGyroEnableDataOutput.Length, true, (__) => {
							StatusMessage = "Enable data interrupt";
							BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandGyroEnableDataInterrupt, CommandGyroEnableDataInterrupt.Length, true, (___) => {
								StatusMessage = "Enable power";
								BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ReadWriteCharacteristic.ServiceUUID, ReadWriteCharacteristic.CharacteristicUUID, CommandGyroEnablePower, CommandGyroEnablePower.Length, true, (____) => {
									StatusMessage = "Accelerometer configured";
									SetState (States.SubscribeToAccelerometer, 0.1f);
								});
							});
						});
					});
					*/
					break;

				case States.SubscribeToAccelerometer:
					SetState (States.SubscribingToAccelerometer, 5f);
					StatusMessage = "Subscribing to MetaMotion Accelerometer...";
					BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress (_deviceAddress, SubscribeCharacteristic.ServiceUUID, SubscribeCharacteristic.CharacteristicUUID, null, (deviceAddress, characteristric, bytes) => {

						_state = States.None;
						MiddlePanel.SetActive (true);

						var vector = AccelerometerBmi160.GetVector3 (bytes);
						AccelerometerText.text = string.Format("Accelerometer: x:{0}, y:{1}, z:{2}", vector.x, vector.y, vector.z);
					});
					break;

				case States.SubscribingToAccelerometer:
					// if we got here it means we timed out subscribing to the accelerometer
					SetState (States.Disconnect, 0.5f);
					break;

				case States.Disconnect:
					SetState (States.Disconnecting, 5f);
					if (_connected)
					{
						BluetoothLEHardwareInterface.DisconnectPeripheral (_deviceAddress, (address) => {
							// since we have a callback for disconnect in the connect method above, we don't
							// need to process the callback here.
						});
					}
					else
					{
						Reset ();
						SetState (States.Scan, 1f);
					}
					break;

				case States.Disconnecting:
					// if we got here we timed out disconnecting, so just go to disconnected state
					Reset ();
					SetState (States.Scan, 1f);
					break;
				}
			}
		}
	}

	bool IsEqual (string uuid1, string uuid2)
	{
		return (uuid1.ToUpper ().CompareTo (uuid2.ToUpper ()) == 0);
	}
}
