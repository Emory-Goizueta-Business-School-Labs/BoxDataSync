/* 
 * Author: Jamie Anne Harrell (jamie.harrell@emory.edu)
 * Date: 2/17/2017
 * Credits: Karsten Januszewski     http://www.rhizohm.net/irhetoric/post/2014/08/25/A-Simple-BoxCom-C-API-Wrapper.aspx 
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace BoxDataSync
{
    /// <summary>
    /// This class is the primary interface to the Box.com API. Uses direct Box.com https calls rather than
    /// using an API toolkit. Why? It seemed easier at the time, and Karsten had some great sample code I could borrow quickly. :)
    /// Credits: Karsten Januszewski http://www.rhizohm.net/irhetoric/post/2014/08/25/A-Simple-BoxCom-C-API-Wrapper.aspx 
    /// </summary>
    public static class BoxSyncHelper
    {
        private static string boxApiUrl = Program.boxApiUrl; 
        private static string boxClientId = Program.boxClientId; 
        private static string boxClientSecret = Program.boxClientSecret; 
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int retryCount = 0;

        private static Boolean boxLoginStatus = false;
        /// <summary>
        /// Executes a Box.com API call direclty, waiting for and processing the return response. Handles API Login via OAUTH2
        /// access token, refreshes via refresh token if necessary, and provides a return response object or throws an exception
        /// </summary>
        /// <param name="url">The fully specified API endpoint / target of the request</param>
        /// <param name="httpMethod">HTTP Method of the API Endpoint, usually HttpMethod.Get</param>
        /// <returns>A Return Stream of data from the API endpoint</returns>
        private static Stream DoBoxCall(string url, HttpMethod httpMethod)
        {
            // Force TLS v1.2
            if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12) == false)
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
            }

            Stream stream;
            String boxAccessToken = Program.getSetting("boxAccessToken");
            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine(" -- DEBUG: DoBoxCall URL={0}", url);
            }

            var request = new HttpRequestMessage() { RequestUri = new Uri(url), Method = httpMethod };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", "Bearer " + boxAccessToken);
            if (BoxDataSync.Program.verbose)
            {
                Console.WriteLine("Connecting to box... {0}", boxAccessToken);
            }
            var response = _httpClient.SendAsync(request).Result;

            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine("response status:{0}", response.ToString());
                //Console.WriteLine("response status:{0}", response.StatusCode.ToString());
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new Exception("Please use BoxDataSync.exe login.");
            }

            if (response.IsSuccessStatusCode)
            {
                if (BoxDataSync.Program.verbose) { Console.WriteLine("Connection Successful"); }
                boxLoginStatus = true;
            }
            else
            {
                if (BoxDataSync.Program.verbose)
                {
                    Console.WriteLine("Login Failed");
                    Console.WriteLine("Response Status Code:", response.StatusCode.ToString());
                }
                boxLoginStatus = false;
            }

            if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (BoxDataSync.Program.verbose)
                {
                    Console.WriteLine("Status Code Unauthorized.");
                }
                if (retryCount < 2)
                {
                    if (BoxDataSync.Program.verbose)
                    {
                        Console.WriteLine("Attempting Token Refresh and trying again...");
                    }
                    RefreshBoxToken();
                    retryCount++;
                    stream = DoBoxCall(url, httpMethod);
                    return stream;
                }
                else
                {
                    throw new Exception("Failed to connect to Box.");
                }

            }
            retryCount = 0;
            Stream returnStream = response.Content.ReadAsStreamAsync().Result;

            if (BoxDataSync.Program.debug)
            {
                StreamReader reader = new StreamReader(returnStream);
                string data = reader.ReadToEnd();
                returnStream.Seek(0, SeekOrigin.Begin);
                Console.WriteLine(" -- DEBUG: BOX CALL RESPONSE:");
                Console.WriteLine(JsonFormatter.FormatJson(data));
            }

            if (boxLoginStatus == false)
            {
                throw new Exception("Box call failed.");
            }

            return returnStream;
        }

        /// <summary>
        /// Uses the Box refresh token to acquire a new access token when the access token has expired. Stores the updated tokens
        /// in user storage.
        /// </summary>
        private static void RefreshBoxToken()
        {

            // Force TLS v1.2
            if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12) == false)
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
            }

            String boxRefreshToken = Program.getSetting("boxRefreshToken");
            using (var request = new HttpRequestMessage() { RequestUri = new Uri("https://www.box.com/api/oauth2/token"), Method = HttpMethod.Post })
            {
                HttpContent content = new FormUrlEncodedContent(new[]
                {
                 new KeyValuePair<string, string>("grant_type", "refresh_token"),
                 new KeyValuePair<string, string>("refresh_token", boxRefreshToken),
                 new KeyValuePair<string, string>("client_id", boxClientId),
                 new KeyValuePair<string, string>("client_secret", boxClientSecret)

                }


                );
                request.Content = content;
                using (var response = _httpClient.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Box refresh token failed. A human needs to go to a browser and generate a fresh authorization code.");
                    }
                    JObject jObject = jObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    Program.setSetting("boxAccessToken", (string)jObject["access_token"]);
                    Program.setSetting("boxRefreshToken", (string)jObject["refresh_token"]);
                }
            }
        }

        /// <summary>
        /// Retrieves a JSON File Object (file meta data) for a single file stored at Box.com using the File ID 
        /// </summary>
        /// <param name="fileId">File ID to read</param>
        /// <returns>JSON File Object</returns>
        public static string GetFileById(string fileId)
        {
            string url = string.Format("{0}files/{1}", boxApiUrl, fileId);
            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine(" -- DEBUG: GetFileById - Calling DoBoxCall");
            }
            Stream stream = DoBoxCall(url, HttpMethod.Get);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Retrieves a JSON Folder Object (folder meta data and contents) for a single folder stored at Box.com using the Folder Id
        /// </summary>
        /// <param name="folderId">Folder ID to read</param>
        /// <returns>JSON Folder Object including the Item Collection in the Folder (eg, list of folder and file mini-objects)</returns>
        public static string GetFolderById(string folderId)
        {
            string url = string.Format("{0}folders/{1}", boxApiUrl, folderId);
            if (Program.debug)
            {
                Console.WriteLine(" -- DEBUG: GetFolderById - Calling DoBoxCall");
            }
            Stream stream = DoBoxCall(url, HttpMethod.Get);
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Uses the provided Box Access Code in the OAUTH2 process to acquire box access and refresh tokens
        /// </summary>
        /// <param name="boxAccessCode">The CODE returned in the query string of the successful login redirect.</param>
        public static void Bootstrap(string boxAccessCode)
        {

            // Force TLS v1.2
            if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12) == false)
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
            }

            using (var request = new HttpRequestMessage() { RequestUri = new Uri("https://api.box.com/oauth2/token"), Method = HttpMethod.Post })
            {
                HttpContent content = new FormUrlEncodedContent(new[]
                {
                 new KeyValuePair<string, string>("grant_type", "authorization_code"),
                 new KeyValuePair<string, string>("code", boxAccessCode),
                 new KeyValuePair<string, string>("client_id", boxClientId),
                 new KeyValuePair<string, string>("client_secret", boxClientSecret)

                }


                );
                request.Content = content;
                try
                {
                    var response = _httpClient.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        JObject jObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                        Program.setSetting("boxAccessToken", (string)jObject["access_token"]);
                        Program.setSetting("boxRefreshToken", (string)jObject["refresh_token"]);
                    }
                    else
                    {

                        JObject jObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                        Console.WriteLine("Error: {0}", (string)jObject["error_description"]);
                    }

                }
                catch (System.AggregateException e)
                {
                    if (Program.debug)
                    {
                        Console.WriteLine(" -- DEBUG EXCEPTION MESSAGE: {0}", e.Message);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Reconciles a fully specified file path from the Box Root directory and returns the file ID of the associated file, if found.
        /// </summary>
        /// <param name="filePath">Box.com path to the file, eg, /path/to/a/file.txt</param>
        /// <returns>The Box.com File ID for the file, if found.</returns>
        public static string GetFileIdByPath(string filePath)
        {

            String directory = Path.GetDirectoryName(filePath);
            String filename = Path.GetFileName(filePath);
            String folderid = "0";
            String returnid = "";

            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine(" -- DEBUG: FOLDER IS:{0}", directory);
                Console.WriteLine(" -- DEBUG: FILE IS:{0}", filename);
            }

            if (directory != null)
            {
                folderid = GetDirectoryIdByPath(directory);
            }


            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine(" -- DEBUG: FOLDER ID IS:{0}", folderid);
            }

            // Do a listing of the directory, if a file ID matches return it
            String data = BoxSyncHelper.GetFolderById(folderid);
            JObject jObject = JObject.Parse(data);
            JArray jArray = jObject["item_collection"]["entries"] as JArray;
            foreach (JObject item in jArray)
            {
                if (item.GetValue("type").ToString() == "file")
                {
                    if (BoxDataSync.Program.debug)
                    {
                        Console.WriteLine(" -- DEBUG: Evaluating:{0}?={1}", item.GetValue("name").ToString(), filename);
                    }
                    if (item.GetValue("name").ToString() == filename)
                    {
                        returnid = item.GetValue("id").ToString();
                        if (BoxDataSync.Program.debug)
                        {
                            Console.WriteLine(" -- DEBUG: FOUND!");
                        }
                    }
                    break;
                }
            }
            return returnid;
        }

        /// <summary>
        /// Recursively reconciles a fully specified folder path from the Box Root directory and returns the folder ID of the associated folder, if found.
        /// </summary>
        /// <param name="path">Box.com path to the folder, eg, /path/to/a/folder</param>
        /// <param name="parentid">Should be called with ID=0 when a path is fully specified from the box root (this is the default and intended use case)
        /// Used recursively to get the listing of each successive level from the ID provided.</param>
        /// <returns>The Folder ID for folder, if found.</returns>
        public static string GetDirectoryIdByPath(string path, string parentid = "0")
        {
            char[] separator = { '\\', '/' };
            string[] directories = path.Split(separator, 2);
            string search = "";
            string remainder = "";
            string returnval = "0";
            string newparentid = parentid;

            if (directories.Length == 0) { return parentid; }
            if (directories.Length == 1) { search = directories[0]; }
            if (directories.Length == 2) { search = directories[0]; remainder = directories[1]; }

            if (BoxDataSync.Program.debug)
            {
                Console.WriteLine("Original path: {0}", path);
                Console.WriteLine("Search={0} Remainder={1}", search, remainder);
            }

            if (search.Length > 0)
            {
                Boolean found = false;
                string json = BoxSyncHelper.GetFolderById(parentid);
                JObject jObject = JObject.Parse(json);
                JArray jArray = jObject["item_collection"]["entries"] as JArray;
                foreach (JObject item in jArray)
                {
                    if (item.GetValue("type").ToString() == "folder" && item.GetValue("name").ToString() == search)
                    {
                        newparentid = item.GetValue("id").ToString();
                        returnval = newparentid;
                        found = true;
                    }
                }
                if (!found)
                {
                    throw new Exception(String.Format("Invalid path: {0} not found.", search));
                }
            }

            if (remainder.Length > 0)
            {
                return GetDirectoryIdByPath(remainder, newparentid);
            }

            return returnval;


        }

        /// <summary>
        /// Downloads the contents of the Box.Com file specified (Binary file download).
        /// </summary>
        /// <param name="fileId">ID of the Box.com file to download.</param>
        /// <returns>Returns a binary data stream representing the downloaded file.</returns>
        public static Stream GetFileAsStream(string fileId)
        {
            string url = string.Format("{0}files/{1}/content", boxApiUrl, fileId);
            return DoBoxCall(url, HttpMethod.Get);
        }
    }
}
