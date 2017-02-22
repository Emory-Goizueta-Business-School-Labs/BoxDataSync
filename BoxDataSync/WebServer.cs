/* 
 * Author: Jamie Anne Harrell (jamie.harrell@emory.edu)
 * Date: 2/17/2017
 * Credits: David in Cambridge ???  https://codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server 
 * 
 * License: MIT / Open Source
 * 
 * Copyright (c) 2017 Emory Goizueta Business School
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// https://codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server

namespace BoxDataSync
{
    /// <summary>
    /// Implements a simple web server class
    /// </summary>
    public class WebServer
    {

        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;
        private static int _requestsProcessed = 0;

        /// <summary>
        /// WebServer Constructor.
        /// </summary>
        /// <param name="prefixes">URL of Webserver Path to listen on.</param>
        /// <param name="method">HTTP Listener Callback Function</param>
        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }
            if (prefixes == null) { throw new ArgumentException("method"); }

            foreach (string s in prefixes)
            {
                _listener.Prefixes.Add(s);
            }

            _responderMethod = method;
            _listener.Start();
        }

        /// <summary>
        /// WebServer Constructor
        /// </summary>
        /// <param name="method">HTTP Listener Callback function</param>
        /// <param name="prefixes">URL of Webserver Path to listen on.</param>
        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes) : this(prefixes, method) { }

        /// <summary>
        /// Starts the Web Server
        /// </summary>
        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                                _requestsProcessed++;
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        /// <summary>
        /// Returns number of requests processed.
        /// </summary>
        /// <returns>Number of requests processed.</returns>
        public int RequestsProcessed()
        {
            return _requestsProcessed;
        }
               
        /// <summary>
        /// Stops the web server and closes the listener.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

    }

}
