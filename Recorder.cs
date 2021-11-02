using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRec
{
	public delegate void Notify();
	public delegate void NotifyRecordingEnded(bool keptFile);
	class Recorder : IDisposable
	{
		private IWaveIn? waveIn;
		//private bool waveInIsRecording = false;
		private Stream? writer;

		private readonly Queue<TimestampedValue> rmsLog;

		public Recorder()
		{
			rmsLog = new Queue<TimestampedValue>(new int[100].Select(o => new TimestampedValue()));
		}

		public static Dictionary<int, WaveInCapabilities> GetWaveInDevices()
		{
			var result = new Dictionary<int, WaveInCapabilities>();
			for (int waveInDevice = 0; waveInDevice < WaveIn.DeviceCount; waveInDevice++)
				result.Add(waveInDevice, WaveIn.GetCapabilities(waveInDevice));

			return result;
		}
		public static List<MMDevice> GetMMDevices()
		{
			return new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
		}

		public void Init(MMDevice? device = null)
		{
			if (device == null)
				device = GetMMDevices().First();

			waveIn = new WasapiCapture(device);
			waveIn.DataAvailable += WaveIn_DataAvailable;
		}

		public event Notify? DataAvailable;
		public event Notify? RecordingStarted;
		public event NotifyRecordingEnded? RecordingEnded;

		private DateTime? recordingStartTime;
		private ulong numBytesReceived = 0;

		private float triggerStartRMSThreshold = 0.2f;
		private float triggerEndRMSThreshold = 0.05f;
		private DateTime? triggerEndInitialTime = null;
		private TimeSpan triggerEndWaitTime = TimeSpan.FromSeconds(5);
		private readonly TimeSpan deleteIfShorterThan = TimeSpan.FromSeconds(5);

		public float CurrentRMS => RMSLog.Any() ? RMSLog[^1].Value : 0;

		public IReadOnlyList<TimestampedValue> RMSLog => rmsLog.ToList();

		private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
		{
			if (waveIn == null)
				return;

			numBytesReceived += (uint)e.BytesRecorded;
			var rms = AudioBufferUtils.CalcRMS(e.Buffer, e.BytesRecorded, waveIn.WaveFormat.BitsPerSample, waveIn.WaveFormat.Channels);
			rmsLog.Enqueue(new TimestampedValue { Value = rms, Timestamp = numBytesReceived });
			rmsLog.Dequeue();

			if (IsRecording)
			{
				if (rms >= triggerStartRMSThreshold)
				{
					triggerEndInitialTime = null;
				}
				else
				{
					if (rms < triggerEndRMSThreshold)
					{
						if (triggerEndInitialTime.HasValue == false)
						{
							triggerEndInitialTime = DateTime.Now;
						}
					}
					if ((DateTime.Now - triggerEndInitialTime) > triggerEndWaitTime)
					{
						StopRecording();
					}
				}
			}
			else if (rms >= triggerStartRMSThreshold)
			{
				StartRecording();
			}

			writer?.Write(e.Buffer, 0, e.BytesRecorded);
			DataAvailable?.Invoke();
		}

		public void StartListening(float triggerStartRMSThreshold = 1.1f, float triggerEndRMSThreshold = 0.0f, TimeSpan? triggerEndWaitTime = null)
		{
			this.triggerStartRMSThreshold = triggerStartRMSThreshold;
			this.triggerEndRMSThreshold = triggerEndRMSThreshold;
			this.triggerEndWaitTime = triggerEndWaitTime ?? TimeSpan.FromSeconds(1);

			if (!IsListening)
			{
				IsListening = true;
				waveIn?.StartRecording();
			}
		}

		private string? currentTargetFilePath;
		public void StartRecording()
		{
			if (waveIn == null)
				return;
			if (writer != null)
				return;
			RecordingStarted?.Invoke();
			recordingStartTime = DateTime.Now;
			currentTargetFilePath = $"test_{recordingStartTime.Value:yyMMdd_HH_mm_ss}";

			writer = new NAudio.Lame.LameMP3FileWriter($"{currentTargetFilePath}.mp3", waveIn.WaveFormat, 128);
			//writer = new WaveFileWriter($"{currentTargetFilePath}.wav", waveIn.WaveFormat);
		}

		public bool IsRecording => writer != null;
		public bool IsListening { get; private set; }

		public void StopRecording()
		{
			if (writer == null)
				return;
			writer.Dispose();
			writer = null;

			var keepFile = (DateTime.Now - recordingStartTime) < deleteIfShorterThan;
			RecordingEnded?.Invoke(keepFile);

			if (keepFile && currentTargetFilePath != null)
			{
				File.Delete(currentTargetFilePath);
				currentTargetFilePath = null;
			}
		}

		public void Stop()
		{
			IsListening = false;
			waveIn?.StopRecording();
			writer?.Dispose();
			writer = null;
		}

		public void Dispose()
		{
			Stop();
			waveIn?.Dispose();
			waveIn = null;
		}

		public class TimestampedValue
		{
			public float Value { get; set; }
			public ulong Timestamp { get; set; }
		}
	}
}
