using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioRec
{
	public static class ConsoleHelpers
	{
		public static string CreateString(char c, int length) => string.Join("", new List<int>(new int[length]).Select(o => c));
	}

	public class TimeChart
	{
		private readonly int cursorTopLine;
		private readonly int maxHeight;
		private readonly int numXPerUpdate;
		private readonly float maxValue;
		private readonly float? drawLineAt;
		private readonly int maxWidth;

		private int currentX = 0;
		private float currentMaxValue = 0;
		private int currentStep = 0;

		private float windowMaxValue = 0;

		public TimeChart(int cursorTopLine, int maxHeight, int numXPerUpdate = 1, float maxValue = 1, float? drawLineAt = null)
		{
			this.cursorTopLine = cursorTopLine;
			this.maxHeight = maxHeight;
			this.numXPerUpdate = numXPerUpdate;
			this.maxValue = maxValue;
			this.drawLineAt = drawLineAt;
			maxWidth = Console.WindowWidth - 1;

			Clear();
		}

		public int MaxHeight => maxHeight;
		public int CursorTopLine => cursorTopLine;


		public void Update(float value, bool isRecording)
		{
			currentMaxValue = Math.Max(value, currentMaxValue);

			if (++currentStep == numXPerUpdate)
			{
				Render(currentMaxValue, isRecording);
				currentStep = 0;
				currentMaxValue = 0;
			}
		}

		private float ValueToY(float value) => Math.Max(Math.Min(value / maxValue, 1), 0) * maxHeight;

		private void Render(float value, bool isRecording)
		{
			windowMaxValue = Math.Max(value, windowMaxValue);

			if (++currentX >= maxWidth)
			{
				Clear();
				windowMaxValue = 0;
				currentX = 0;
			}

			Console.CursorTop = cursorTopLine;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write($"{windowMaxValue:#.00}");

			var height = ValueToY(value);
			var intHeight = (int)Math.Floor(height);

			ConsoleColor clr;
			if (height < 1)
			{
				height = 1;
				clr = isRecording ? ConsoleColor.DarkRed : ConsoleColor.DarkBlue;
			}
			else
			{
				clr = isRecording ? ConsoleColor.Red : ConsoleColor.Blue;
			}

			Console.CursorTop = cursorTopLine + (maxHeight - intHeight);
			Console.ForegroundColor = clr;
			Console.CursorLeft = currentX;
			var fract = height - intHeight;
			char c = '-';
			if (fract < 0.3f)
				c = '.';
			else if (fract > 0.66f)
				c = '\'';
			Console.Write(c);

			for (int i = 1; i < height - 1; i++)
			{
				Console.BackgroundColor = clr;
				Console.CursorTop++;
				Console.CursorLeft = currentX;
				Console.Write(' ');
			}
		}

		private void Clear()
		{
			Console.BackgroundColor = ConsoleColor.Black;
			for (int i = 0; i <= maxHeight + 1; i++)
			{
				Console.CursorTop = cursorTopLine + i;
				Console.CursorLeft = 0;
				Console.Write(ConsoleHelpers.CreateString(' ', maxWidth));
			}
			if (drawLineAt.HasValue)
			{
				Console.CursorLeft = 0;
				Console.CursorTop = cursorTopLine + (int)Math.Floor(ValueToY(drawLineAt.Value));
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.Write(ConsoleHelpers.CreateString('_', maxWidth));
			}
		}
	}

	public class RMSMeter
	{
		private readonly int cursorLine;
		private float peakIndicator;

		public RMSMeter(int cursorLine)
		{
			this.cursorLine = cursorLine;
		}

		public void Render(float rms, bool isRecording)
		{
			var steps = 100;
			rms = 2f * rms;

			Console.CursorTop = cursorLine;

			Console.CursorLeft = 0;
			var str = ConsoleHelpers.CreateString(' ', steps);
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Write(str);

			Console.CursorLeft = 0;
			Console.BackgroundColor = ConsoleColor.Green;
			if (rms > peakIndicator) peakIndicator = rms;
			else peakIndicator = Math.Max(peakIndicator - 0.01f, 0);

			str = ConsoleHelpers.CreateString(' ', (int)(steps * rms));
			Console.Write(str);

			Console.CursorLeft = (int)(steps * peakIndicator);
			Console.BackgroundColor = ConsoleColor.Blue;
			Console.Write(" ");

			Console.BackgroundColor = isRecording ? ConsoleColor.Red : ConsoleColor.Black;
			Console.CursorLeft = 0;
			Console.Write(" ");

			Console.BackgroundColor = ConsoleColor.Black;
			Console.CursorLeft = 0;
		}
	}
}
