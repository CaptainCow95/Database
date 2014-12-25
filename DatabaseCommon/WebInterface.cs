using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace Database.Common
{
    /// <summary>
    /// The web interface to the node.
    /// </summary>
    public static class WebInterface
    {
        /// <summary>
        /// The lock object for the http listener.
        /// </summary>
        private static readonly object LockObject = new object();

        /// <summary>
        /// The thread the web interface runs on.
        /// </summary>
        private static Thread _interfaceThread;

        /// <summary>
        /// The port the web interface runs on.
        /// </summary>
        private static int _port;

        /// <summary>
        /// The function to call when a web request is received.
        /// </summary>
        private static RequestReceivedDelegate _requestReceived;

        /// <summary>
        /// A value indicating whether the web interface is running.
        /// </summary>
        private static bool _running;

        /// <summary>
        /// The delegate used when a web request is received.
        /// </summary>
        /// <param name="page">The page that was requested.</param>
        /// <param name="queryString">The parameters on the query string.</param>
        /// <returns>The html of the page to be returned.</returns>
        public delegate string RequestReceivedDelegate(string page, NameValueCollection queryString);

        /// <summary>
        /// Starts the web interface.
        /// </summary>
        /// <param name="port">The port the web interface is going to run on.</param>
        /// <param name="requestReceived">The function to call when a request is received.</param>
        public static void Start(int port, RequestReceivedDelegate requestReceived)
        {
            _port = port;
            _requestReceived = requestReceived;
            _running = true;
            _interfaceThread = new Thread(RunWebInterface);
            _interfaceThread.Start();
        }

        /// <summary>
        /// Stops the web interface.
        /// </summary>
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

        /// <summary>
        /// Called when a request has been received and needs to be processed.
        /// </summary>
        /// <param name="result">The result of the listener call, in this case, the request to be processed.</param>
        private static void ProcessRequest(IAsyncResult result)
        {
            var listener = (HttpListener)result.AsyncState;
            HttpListenerContext context;
            lock (LockObject)
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

            lock (LockObject)
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

        /// <summary>
        /// The run method for the web interface thread.
        /// </summary>
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
                Logger.Log("Could not initiate web interface for all incoming connections, initiating for localhost only. Please run the program as administrator for web interface access from all incoming connections.");
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:" + _port + "/");
                listener.Start();
            }

            listener.BeginGetContext(ProcessRequest, listener);
            while (_running)
            {
                Thread.Sleep(100);
            }

            lock (LockObject)
            {
                listener.Stop();
            }
        }
    }
}