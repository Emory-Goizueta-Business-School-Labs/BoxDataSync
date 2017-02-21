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

using BoxDataSync.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace BoxDataSync
{

    public static class BoxSyncHelper
    {
        private static string boxApiUrl = Program.boxApiUrl; 
        private static string boxClientId = Program.boxClientId; 
        private static string boxClientSecret = Program.boxClientSecret; 
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int retryCount = 0;

        private static Boolean boxLoginStatus = false;

        private static Stream DoBoxCall(string url, HttpMethod httpMethod)
        {
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
        private static void RefreshBoxToken()
        {
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
                    //Settings.Default.boxAccessToken = (string)jObject["access_token"];
                    //Settings.Default.boxRefreshToken = (string)jObject["refresh_token"];
                    //Settings.Default.Save();
                }
            }
        }

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
        public static void Bootstrap(string boxAccessCode)
        {
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
                var response = _httpClient.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    JObject jObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    Program.setSetting("boxAccessToken", (string)jObject["access_token"]);
                    Program.setSetting("boxRefreshToken", (string)jObject["refresh_token"]);

                    //Settings.Default.boxAccessToken = (string)jObject["access_token"];
                    //Settings.Default.boxRefreshToken = (string)jObject["refresh_token"];
                    //Settings.Default.Save();

                }
                else
                {

                    JObject jObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine("Error: {0}", (string)jObject["error_description"]);
                }

            }

        }

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

        public static Stream GetFileAsStream(string fileId)
        {
            string url = string.Format("{0}files/{1}/content", boxApiUrl, fileId);
            return DoBoxCall(url, HttpMethod.Get);
        }
    }
}
