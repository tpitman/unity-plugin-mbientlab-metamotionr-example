using static MbientLab.MetaWear.Impl.Module;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MbientLab.MetaWear.Impl
{
	public class AccelerometerBmi160
	{
		public enum StepDetectorMode
		{
			Normal,
			Sensitive,
			Robust,
		}

		public enum FilterMode
		{
			Osr4,
			Osr2,
			Normal
		}

		/// <summary>
		/// Operating frequencies of the BMI160 accelerometer
		/// </summary>
		public enum OutputDataRate
		{
			_0_78125Hz,
			_1_5625Hz,
			_3_125Hz,
			_6_25Hz,
			_12_5Hz,
			_25Hz,
			_50Hz,
			_100Hz,
			_200Hz,
			_400Hz,
			_800Hz,
			_1600Hz
		}

		/// <summary>
		/// Available data ranges for Bosch accelerometers
		/// </summary>
		public enum DataRange
		{
			_2g,
			_4g,
			_8g,
			_16g
		}

		public const byte POWER_MODE = 1,
			DATA_INTERRUPT_ENABLE = 2, DATA_CONFIG = 3, DATA_INTERRUPT = 4, DATA_INTERRUPT_CONFIG = 5,
			LOW_HIGH_G_INTERRUPT_ENABLE = 0x6, LOW_HIGH_G_CONFIG = 0x7, LOW_HIGH_G_INTERRUPT = 0x8,
			MOTION_INTERRUPT_ENABLE = 0x9, MOTION_CONFIG = 0xa, MOTION_INTERRUPT = 0xb,
			TAP_INTERRUPT_ENABLE = 0xc, TAP_CONFIG = 0xd, TAP_INTERRUPT = 0xe,
			ORIENT_INTERRUPT_ENABLE = 0xf, ORIENT_CONFIG = 0x10, ORIENT_INTERRUPT = 0x11,
			FLAT_INTERRUPT_ENABLE = 0x12, FLAT_CONFIG = 0x13, FLAT_INTERRUPT = 0x14,
			PACKED_ACC_DATA = 0x1c;

		public const byte STEP_DETECTOR_INTERRUPT_ENABLE = 0x17,
			STEP_DETECTOR_CONFIG = 0x18,
			STEP_DETECTOR_INTERRUPT = 0x19,
			STEP_COUNTER_DATA = 0x1a,
			STEP_COUNTER_RESET = 0x1b;
		public static readonly float[] FREQUENCIES = new float[] { 0.078125f, 1.5625f, 3.125f, 6.25f, 12.5f, 25f, 50f, 100f, 200f, 400f, 800f, 1600f };

		public static byte[] sendCommand (Module module, byte command, byte[] config = null)
		{
			var commandBytes = new byte[config == null ? 2 : 2 + config.Length];
			commandBytes[0] = (byte)module;
			commandBytes[1] = command;

			if (config != null)
				Array.Copy (config, 0, commandBytes, 2, config.Length);

			return commandBytes;
		}

		protected static readonly byte[] RANGE_BIT_MASKS = new byte[] { 0x3, 0x5, 0x8, 0xc };

		protected static byte[] accDataConfig = new byte[2];

		public static byte[] Configure (Module module = ACCELEROMETER, OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g, FilterMode filter = FilterMode.Normal)
		{
			accDataConfig[0] &= 0xf0;
			accDataConfig[0] |= (byte)(((int)odr + 1) | ((FREQUENCIES[(int)odr] < 12.5f) ? 0x80 : (int)filter << 4));
			accDataConfig[1] &= 0xf0;
			accDataConfig[1] |= RANGE_BIT_MASKS[(int)range];

			Scale = DataScale;

			return sendCommand (module, DATA_CONFIG, accDataConfig);
		}

		public static byte[] DataInterruptStart ()
		{
			return sendCommand (ACCELEROMETER, DATA_INTERRUPT_ENABLE, new byte[] { 0x01 });
		}

		public static byte[] DataInterruptStop ()
		{
			return sendCommand (ACCELEROMETER, DATA_INTERRUPT_ENABLE, new byte[] { 0x00 });
		}

		public static byte[] DataStart()
		{
			return sendCommand (ACCELEROMETER, DATA_INTERRUPT, new byte[] { 0x01, 0x00 });
		}

		public static byte[] DataStop()
		{
			return sendCommand (ACCELEROMETER, DATA_INTERRUPT, new byte[] { 0x00, 0x01 });
		}

		public static byte[] Start ()
		{
			return sendCommand (ACCELEROMETER, POWER_MODE, new byte[] { 0x01 });
		}

		public static byte[] Stop ()
		{
			return sendCommand (ACCELEROMETER, POWER_MODE, new byte[] { 0x00 });
		}

		protected static float Scale = 1f;
		protected static float DataScale
		{
			get
			{
				switch (accDataConfig[1] & 0x0f)
				{
				case 0x3:
					return 16384f;
				case 0x5:
					return 8192f;
				case 0x8:
					return 4096f;
				case 0xc:
					return 2048f;
				default:
					return 1f;
				}
			}
		}

		public static Vector3 GetVector3(byte[] bytes)
		{
			// first 2 bytes are always 0x13 0x05 indicating what value it is
			return new Vector3 (BitConverter.ToInt16 (bytes, 2) / Scale, BitConverter.ToInt16 (bytes, 4) / Scale, BitConverter.ToInt16 (bytes, 6) / Scale);
		}
	}
}
