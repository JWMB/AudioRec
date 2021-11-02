using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRec
{
	class AudioBufferUtils
	{
		public static float ConvertBufferValue(byte[] buffer, int index, int bitsPerSample)
		{
			switch (bitsPerSample)
			{
				case 8:
					return 2f * buffer[index] / 255 - 1;
				case 16:
					return 2f * (buffer[index] << 8 + buffer[index + 1]) / 65535 - 1;
				case 24:
					return 1f * (buffer[index] << 16 + buffer[index + 1] << 8 + buffer[index + 2]) / 16777215;
				case 32:
					return BitConverter.ToSingle(buffer, index);
				default:
					throw new ArgumentException("Unhandled format");
			}
		}

		public static float CalcRMS(byte[] buffer, int lastBufferIndex, int bitsPerSample, int channels)
		{
			float squaredsum = 0;
			int numSamples = 0;
			var index = 0;
			var bytesPerSample = bitsPerSample / 8;
			while (index < lastBufferIndex - bytesPerSample)
			{
				var val = ConvertBufferValue(buffer, index, bitsPerSample);

				squaredsum += val * val;
				numSamples++;

				index += channels * bytesPerSample;
			}
			return (float)Math.Sqrt(squaredsum / numSamples);
		}
	}
}
