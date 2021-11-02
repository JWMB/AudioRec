// See https://aka.ms/new-console-template for more information

using AudioRec;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

var trigStartRMS = float.Parse(GetArgument(args, 0, "0.2").Replace(",", "."), CultureInfo.InvariantCulture);
var trigStopRMS = float.Parse(GetArgument(args, 0, "0.05").Replace(",", "."), CultureInfo.InvariantCulture);


var recorder = new Recorder();

var devices = Recorder.GetMMDevices();
var device = devices.First();
Console.WriteLine($"{devices.Count} devices available:");
Console.WriteLine($"{string.Join("\n", devices.Select((device, i) => $"{i}: {device.DeviceFriendlyName}"))}");

recorder.Init(device);

Console.CursorVisible = false;
var cursorTopLine = Console.CursorTop;

var rms = new RMSMeter(Console.CursorTop);
var history = new TimeChart(Console.CursorTop + 1, 10, 5, 0.25f, trigStartRMS);
var calibrateRMS = new List<float>();

recorder.DataAvailable += () => {
	calibrateRMS?.Add(recorder.CurrentRMS);
	rms?.Render(recorder.CurrentRMS, recorder.IsRecording);
	history?.Update(recorder.CurrentRMS, recorder.IsRecording);
};

recorder.RecordingStarted += () =>
{
	//Console.CursorTop = cursorTopLine + 1;
	//Console.Write("Started!");
};
recorder.RecordingEnded += (keptFile) =>
{
	//Console.CursorTop = cursorTopLine + 1;
	//Console.Write($"Ended - kept file? {keptFile}");
};

// TODO: first calibrate N seconds. Max RMS / 4 will be used as start threshold, and Min RMS end threshold
recorder.StartListening(float.MaxValue, float.MaxValue);
await Task.Delay(TimeSpan.FromSeconds(5));
var min = calibrateRMS.Min();
var max = calibrateRMS.Max();
calibrateRMS = null;

history = new TimeChart(history.CursorTopLine, history.MaxHeight, 5, max * 2, max);

recorder.StartListening(max * 0.25f, min, TimeSpan.FromSeconds(20));


while (true)
{
	if (Console.KeyAvailable)
		if (Console.ReadKey().Key == ConsoleKey.Q)
			break;
	await Task.Delay(TimeSpan.FromMilliseconds(100));
}

recorder.Stop();
recorder.Dispose();

string GetArgument(string[] args, int index, string defaultVal)
{
	return args == null || index >= args.Length ? defaultVal : args[index];
}