using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ConvertTo2
{
    internal class LocalHttpServer
    {
        private readonly HttpListener _listener;
        private string _filePath;
        private bool _isRunning;

        public LocalHttpServer(int port = 5001)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        // Start the server and serve a single file, only if not already running
        internal void Start(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new ArgumentException("Invalid file path or file does not exist.");
            }

            _filePath = filePath;

            if (!_isRunning)
            {
                _listener.Start();
                _isRunning = true;
                Task.Run(() => HandleIncomingConnections());
            }
        }

        // Check if the server is running
        internal bool IsRunning()
        {
            return _isRunning;
        }

        // Handle incoming HTTP requests, including Range requests
        private async Task HandleIncomingConnections()
        {
            while (_isRunning && _listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                var response = context.Response;
                var request = context.Request;

                try
                {
                    if (File.Exists(_filePath))
                    {
                        long fileLength = new FileInfo(_filePath).Length;

                        // Check if the request is a Range request
                        string rangeHeader = request.Headers["Range"];
                        if (!string.IsNullOrEmpty(rangeHeader))
                        {
                            // Extract the range request information
                            string[] range = rangeHeader.Replace("bytes=", string.Empty).Split('-');
                            long start = Convert.ToInt64(range[0]);
                            long end = range.Length > 1 && !string.IsNullOrEmpty(range[1])
                                       ? Convert.ToInt64(range[1])
                                       : fileLength - 1;

                            // Set the correct content length and status code for partial content
                            response.StatusCode = (int)HttpStatusCode.PartialContent;
                            response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileLength}");
                            response.ContentLength64 = end - start + 1;

                            // Send the requested byte range
                            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                            {
                                fs.Seek(start, SeekOrigin.Begin);
                                byte[] buffer = new byte[1024 * 64]; // 64KB buffer
                                long bytesLeft = end - start + 1;
                                int bytesRead;
                                while (bytesLeft > 0 && (bytesRead = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesLeft))) > 0)
                                {
                                    await response.OutputStream.WriteAsync(buffer, 0, bytesRead);
                                    bytesLeft -= bytesRead;
                                }
                            }
                        }
                        else
                        {
                            // If no Range request, send the entire file
                            response.ContentLength64 = fileLength;
                            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                            {
                                byte[] buffer = new byte[1024 * 64]; // 64KB buffer
                                int bytesRead;
                                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    await response.OutputStream.WriteAsync(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                    else
                    {
                        // File not found
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch (Exception ex)
                {
                    // Handle server errors
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    using (StreamWriter writer = new StreamWriter(response.OutputStream))
                    {
                        await writer.WriteLineAsync($"Error: {ex.Message}");
                    }
                }
                finally
                {
                    response.Close(); // Ensure the response is closed after each request
                }
            }
        }

        // Stop the server if needed (optional, for manual stop)
        internal void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;

                if (_listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close(); // Ensure the listener is fully closed and resources are released
                }
            }
        }
    }
}
