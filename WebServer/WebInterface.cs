using Logging;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace WebServer
{
    public static class WebInterface
    {
        private static Thread _interfaceThread;
        private static object _lockObject = new object();
        private static int _port;
        private static RequestReceivedDelegate _requestReceived;
        private static bool _running;

        public delegate string RequestReceivedDelegate(string page, NameValueCollection queryString);

        public static void Start(int port, RequestReceivedDelegate requestReceived)
        {
            _port = port;
            _requestReceived = requestReceived;
            _running = true;
            _interfaceThread = new Thread(RunWebInterface);
            _interfaceThread.Start();
        }

        public static void Stop()
        {
            _running = false;
            try
            {
                _interfaceThread.Join();
            }
            catch
            {
                // If the thread is already dead or interrupted, don't worry about it, we are shutting down anyway.
            }
        }

        private static void ProcessRequest(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context;
            lock (_lockObject)
            {
                if (listener.IsListening)
                {
                    context = listener.EndGetContext(result);
                }
                else
                {
                    return;
                }
            }

            string page = context.Request.RawUrl;
            if (page.Contains("?"))
            {
                page = page.Substring(0, page.IndexOf('?'));
            }

            if (page.EndsWith("/"))
            {
                page = page.Substring(0, page.Length - 1);
            }

            if (page.StartsWith("/"))
            {
                page = page.Substring(1);
            }

            NameValueCollection queryString = context.Request.QueryString;

            lock (_lockObject)
            {
                if (listener.IsListening)
                {
                    listener.BeginGetContext(ProcessRequest, listener);
                }
            }

            string response = _requestReceived(page, queryString);

            byte[] buffer = Encoding.Default.GetBytes(response);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private static void RunWebInterface()
        {
            HttpListener listener;
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:" + _port + "/");
                listener.Start();
            }
            catch (HttpListenerException)
            {
                Logger.Log("Could not initiate web interface for all incoming connections, initiating for localhost only. Please run the server as administrator for web interface access from all incoming connections.");
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:" + _port + "/");
                listener.Start();
            }

            listener.BeginGetContext(ProcessRequest, listener);
            while (_running)
            {
                Thread.Sleep(100);
            }

            lock (_lockObject)
            {
                listener.Stop();
            }
        }
    }
}