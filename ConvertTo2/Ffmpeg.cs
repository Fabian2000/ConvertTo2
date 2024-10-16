using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal class Ffmpeg
{
	private Process _ffmpegProcess;
	private CancellationTokenSource _cancellationTokenSource;

	// Event that is triggered when the process is done
	public event EventHandler<bool> ProcessCompleted; // bool indicates success (true) or failure (false)

	// Start the FFmpeg process asynchronously
	public async Task StartAsync(string args, string outputFile)
	{
		_cancellationTokenSource = new CancellationTokenSource();

		try
		{
			_ffmpegProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "ffmpeg", // Assuming ffmpeg is in the PATH
					Arguments = args,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				},
				EnableRaisingEvents = true
			};

			_ffmpegProcess.Exited += (sender, e) =>
			{
				bool success = _ffmpegProcess.ExitCode == 0;
				ProcessCompleted?.Invoke(this, success); // Fire the event when process finishes
				_ffmpegProcess.Dispose();
			};

			_ffmpegProcess.Start();

			// Optionally, capture output and error streams if needed
			_ = ReadProcessOutputAsync(_ffmpegProcess);

			// Await until the process exits or gets cancelled
			await Task.Run(() => _ffmpegProcess.WaitForExit(), _cancellationTokenSource.Token);
		}
		catch (OperationCanceledException)
		{
			// If process was cancelled, kill it and delete the output file
			Stop(outputFile);
		}
		catch (Exception ex)
		{
			// Log any other errors if necessary
			Console.WriteLine($"Error starting FFmpeg: {ex.Message}");
		}
	}

	// Stop the FFmpeg process
	public void Stop(string outputFile)
	{
		try
		{
			if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
			{
				_cancellationTokenSource.Cancel(); // Cancel the token
				_ffmpegProcess.Kill(); // Kill the process
				_ffmpegProcess.WaitForExit(); // Ensure it has exited

				// Delete the output file if the process was stopped
				if (File.Exists(outputFile))
				{
					File.Delete(outputFile);
				}

				ProcessCompleted?.Invoke(this, false); // Fire event indicating failure
			}
		}
		catch (Exception ex)
		{
			// Log errors related to stopping the process
			Console.WriteLine($"Error stopping FFmpeg: {ex.Message}");
		}
	}

	// Capture output and error streams (optional)
	private async Task ReadProcessOutputAsync(Process process)
	{
		await Task.WhenAll(
			Task.Run(() => process.StandardOutput.ReadToEnd()),
			Task.Run(() => process.StandardError.ReadToEnd())
		);
	}
}
