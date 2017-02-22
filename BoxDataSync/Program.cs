/*
 * Program: BoxDataSync.exe
 * Author: Jamie Anne Harrell (jamie.harrell@emory.edu)
 * Date: 2/17/2017
 * 
 * 
 * NAME
 * BoxDataSync.exe - command line download/sync files and directories from your box account
 * 
 * SYNOPSIS
 * BoxDataSync.exe [OPTION]... [command] [argument]...
 * 
 * DESCRIPTION
 * BoxDataSync.exe is a command line utility that provides a recursive file/folder synchronization pull from a Box.com account to a local file system.
 * Operation is in concept similar to the unix ftp/scp/rsync type commands though limited (without choice of shell or remote location). Local file SHA1
 * hashes are calculated and compared with remote box SHA1 signatures in order to determine if a file will be downloaded/refreshed.
 * 
 * BoxDataSync utilizes OAUTH2 to acquire access permissions to your box account, though as a command line utility rather than a web/Windows GUI app
 * getting connected to the account initially requires launching a browser to authorize the application to your box account. In the event web authorization
 * can't be used, a command line bootstrap process exists to collect the OAUTH token. Once bootstrapped, the utility will be able to download content from a
 * box account perpetually a long as it is used at least every 60 days. Login to Box.com is done via a web browser, and access tokens are connected
 * to that account and stored in the current Windows user account user space; each Windows user can access their OWN box account and no others.
 * 
 * Box.com login access token and refresh token are stored encrypted in the application's .NET user.config file unless the --usereg flag is specified.
 * This is useful if you want to run from the Windows Scheduler and run into the issue that the profile path isn't loaded in time. See:
 * https://support.microsoft.com/en-us/help/2968540/scheduled-tasks-reference-incorrect-user-profile-paths-in-windows-server-2012
 * 
 * ***WARNING: DO NOT USE THIS UTILITY ON A SHARED LOGIN - ANYONE WHO CAN ACCESS THE MACHINE WITH YOUR USER ACCOUNT CREDENTIALS CAN USE THIS UTILITY
 * *** TO DOWNLOAD BOX.COM DATA/FILES ONCE YOU BOOTSTRAP IT TO YOUR LOGIN.
 * 
 * * TODO: RECURSIVE DIRECTORY DOWNLOADS
 *  
 * GENERAL
 * Different commands / operational modes are supported. For each command, one or more options may be supported that affect output and or functionality.
 * Commands permit one or two additional argument as noted below.
 * 
 * BoxDataSync.exe COMMANDS [PARAMETER]... [ARGUMENT]...
 * 
 *  token                               Test/debug mode. Displays the current Windows user Box.com login token that will be used for API calls. 
 *  
 *  logout                              Logs out of Box.com account / disconnects Windows user from the associated box.com account. (deletes tokens)
 *  
 *  login                               Launches web page to login your user account to Box.com, opens HttpListener on http://localhost:4000 for response.
 *          
 *  bootstrap [token]                   (Depricated) Used to bootstrap a login manually if HttpListener method won't work.
 *  
 *                                      Without a token, bootstrap displays the authentication instructions, URL and process. The instructions indicate
 *                                      how to acquire a new token from Box.com with your box account credentials.
 *                                      
 *                                      With a token (acquired during the bootstrap process), logs into the box account and acquires a boxAccessToken
 *                                      and a boxRefreshToken. These are saved to the current Windows user account for automatic access in the future.
 *                                      
 *  settings2reg                        Move settings from user.config file to registry.
 *  
 *  reg2settings                        Move settings from registry to user.config file
 *                                      
 *  ls|dir [directory]                  ls (or it's windows synonym dir) executes a directory listing by box directory ID or full path. With no directory, 
 *                                      provides a directory listing of the root box directory. With a Box directory id (or path to a Box directory
 *                                      relative to the box root directory), provides a directory listing of the named directory. 
 *
 *  get [file] [destination]            get (single file get) downloads a single file to the local destination directory. Utilizes sha1 checksums to
 *                                      determine if a file is up to date and does not need to by sync'd. The --force flag will causee the file to be
 *                                      downloaded regardless of checksums. Can be a file ID or a file path from the root box directory.
 *                                                                   
 *  mget [directory] [destination]      mget (multiple file get) downloads the contents of the remote box directory to the local destination directory.
 *                                      Utilizes sha1 checksums to determine if a file is up to date and does not need to be sync'd. The --force flag
 *                                      will cause all files in the directory to be downloaded regardless of checksums. Can be a directory ID or a 
 *                                      directory path from the root box directory.
 * 
 * 
 * PARAMETERS
 *  -v, --verbose, /v, /verbose         increase verbosity
 *  -d, --debug, /d /debug              show JSON data structures returned from Box.com calls (debug)
 *  -q, --quiet, /q, /quiet             suppress non-error messages
 *  -u, --usereg /u, /usereg            use registry storage instead of .NET application config files for tokens
 *  -f, --force, /f, /force             force download even if checksums match
 * 
 * 
 * EXAMPLES:
 * 
 *  BoxDataSync.exe ls                   
 * 
 *  BoxDataSync.exe ls "\Path\to\box\directory"
 * 
 *  BoxDataSync.exe get "\Path\to\box\file.xlsx" c:\path\to\windows\directory
 * 
 *  BoxDataSync.exe mget "\Path\to\box\folder" c:\path\to\windows\directory
 * 
 * 
 * CREDITS: Various contributions and ideas for this project were borrowed from:
 *  Karsten Januszewski     http://www.rhizohm.net/irhetoric/post/2014/08/25/A-Simple-BoxCom-C-API-Wrapper.aspx 
 *  David in Cambridge ???  https://codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server
 *  Peter Long              http://stackoverflow.com/questions/4580397/json-formatter-in-c
 *  John Smith              http://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
 *  mgbowen @ stackoverflow http://stackoverflow.com/questions/1993903/how-do-i-do-a-sha1-file-checksum-in-c
 *  MSDN, of Course         https://msdn.microsoft.com/en-us/library/2fh8203k(v=vs.110).aspx
 *
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
using System.Linq;
using BoxDataSync.Properties;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;

namespace BoxDataSync
{
    class Program
    {

        public static Boolean verbose = false;
        public static Boolean debug = false;
        public static Boolean quiet = false;
        public static Boolean force = false;
        public static Boolean usereg = false;

        public static String command = "";
        public static String argument1 = "";
        public static String argument2 = "";

        public static String boxApiUrl = "https://api.box.com/2.0/";
        public static String boxClientId = ApiSettings.boxClientId; // "_BOXCLIENTID";
        public static String boxClientSecret = ApiSettings.boxClientSecret; // "_BOXCLIENTSECRET";
        public static String boxRedirectURI = "http://localhost:4000/";
        private static String localWorkingDirectory = "C:\\temp\\";
        private static String keyname = "HKEY_CURRENT_USER\\SOFTWARE\\Goizueta Business School\\BoxDataSync";

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Formats a human readable file size to a specified number of decimals. Chooses optimal suffix (KB,MG,GB, etc)
        /// </summary>
        /// <param name="value">Integer file size to be formatted</param>
        /// <param name="decimalPlaces">Number of digits to show after the decimal</param>
        /// <returns>Returns a formatted string representing the human readable file size.</returns>
        public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        /// <summary>
        /// Displays usage information for bootstrapping when "BoxDataSync.exe login" won't work for you automatically.
        /// </summary>
        static void Usage()
        {
            Console.WriteLine("BOX DATA SYNC BOOTSTRAP INSTRUCTIONS:");
            Console.WriteLine("=====================================");
            Console.WriteLine(" 1) Copy and Paste this URL into a browser to initiate the authentication process:");
            Console.WriteLine("    https://account.box.com/api/oauth2/authorize?response_type=code&client_id={0}&redirect_uri={1}&state=oob", Program.boxClientId, Program.boxRedirectURI);
            Console.WriteLine(" 2) Log into Box at the URL provided above, and Authorize the 'GBSBIA Box DataSync App' access to your box account. The return URL will provide an access code that you'll need on the next step.");
            Console.WriteLine(" 3) Rerun this console app using: BoxDataSync.exe bootstrap [code] ");
        }

        /// <summary>
        /// This is a callback function used in the BoxDataSync.exe login process. The program creates an OAUTH2 request by opening
        /// a web browser to Box.com. Your Box.com app should use a return URL of http://localhost:4000. The app will create a simple
        /// web server during the login process. This callback function receives the listener event, collects the returned OAUTH2
        /// code, and turns that into a login and refresh token pair.
        /// </summary>
        /// <param name="request">The input HTTP Request Object to be processed. Should have a code in the query string if login is successful.</param>
        /// <returns>Content to be displayed on the web page after processing the OAUTH2 code and logging in.</returns>
        public static string SendResponse(HttpListenerRequest request)
        {
            string response = "";
            NameValueCollection data = request.QueryString;
            String code = data.Get("code");
            BoxSyncHelper.Bootstrap(code);

            String boxAccessToken = getSetting("boxAccessToken");
            if (boxAccessToken.Length == 0)
            {
                response = string.Format("<HTML><BODY>Login Failed.<br>{0}", DateTime.Now);
            }
            else
            {
                response = string.Format("<HTML><BODY>Login Successful. You can close this page now.<br>{0}", DateTime.Now);
            }

            return response;
        }

        /// <summary>
        /// Downloads a file from Box.com. Expects a Box.com formatted JSON File Object (https://docs.box.com/v2.0/reference#file-object)
        /// </summary>
        /// <param name="item"></param>
        static void downloadFile(JObject item)
        {
            if (!Program.quiet)
            {
                Console.Write(" - file ID:{0,15} {1} ... ", item.GetValue("id"), item.GetValue("name"));
            }
            string localhash = Sha1Signature.getFileSignature(Program.localWorkingDirectory + item.GetValue("name").ToString());
            if (localhash == item.GetValue("sha1").ToString() && !Program.force)
            {
                if (!Program.quiet)
                {
                    if (Program.verbose)
                    {
                        Console.Write(" Hash matches Local {0} ... ", localhash);
                    }
                    Console.WriteLine("Up to date.");
                };
            }
            else
            {
                if (!Program.quiet)
                {
                    if (Program.verbose)
                    {
                        if (Program.force && localhash == item.GetValue("sha1").ToString())
                        {
                            Console.Write(" Hashes match, force download enabled.");
                        }
                        else
                        {
                            Console.Write(" Hashes differ Local {0} != Remote {1}", localhash, item.GetValue("sha1"));
                        }
                    }
                    Console.Write("Downloading... ");
                }
                Stream fileData = BoxSyncHelper.GetFileAsStream(item.GetValue("id").ToString());
                var fileStream = File.Create(Program.localWorkingDirectory + item.GetValue("name").ToString());
                fileData.CopyTo(fileStream);
                if (!Program.quiet) { Console.WriteLine("Download complete."); }
            }
        }

        /// <summary>
        /// Retrieves application user settings either from the registry or application .NET user.config file
        /// </summary>
        /// <param name="name">Name of parameter to be read. Valid values are "boxAccessToken" or "boxRefreshToken".</param>
        /// <param name="useEncryption">Determines whether or not to use ProtectedData system calls to decrypt data storage. Defaults true.</param>
        /// <returns>The unencrypted (if applicable) setting value.</returns>
        public static String getSetting(String name, Boolean useEncryption = true)
        {
            string encrypted = "";
            string settingValue = "";
            if (usereg)
            {
                switch (name)
                {
                    case "boxAccessToken": encrypted = (string)Microsoft.Win32.Registry.GetValue(keyname, "boxAccessToken", ""); break;
                    case "boxRefreshToken": encrypted = (string)Microsoft.Win32.Registry.GetValue(keyname, "boxRefreshToken", ""); break;
                }
            }
            else
            {
                switch (name)
                {
                    case "boxAccessToken": encrypted = Settings.Default.boxAccessToken; break;
                    case "boxRefreshToken": encrypted = Settings.Default.boxRefreshToken; break;
                }

            }
            if (encrypted.Length > 0 && useEncryption)
            {
                settingValue = Encryption.Unprotect64(encrypted, DataProtectionScope.CurrentUser);
            }
            else
            {
                settingValue = encrypted;
            }
            if (debug)
            {
                Console.WriteLine("Returning setting: {0}={1}", name, settingValue);
            }
            return settingValue;
        }
        /// <summary>
        /// Stores application user settings either in the registry or application .NET user.config file.
        /// </summary>
        /// <param name="name">Name of parameter to be read. Valid values are "boxAccessToken" or "boxRefreshToken".</param>
        /// <param name="value">Unencrypted value to be stored.</param>
        /// <param name="useEncryption">Determines whether or not to use ProtectedData system calls to encrypt data storage. Defaults true.</param>
        public static void setSetting(String name, String value, Boolean useEncryption = true)
        {
            string encrypted = value;
            if (useEncryption && encrypted.Length>0)
            {
                encrypted = Encryption.Protect64(value, DataProtectionScope.CurrentUser);
            }
            if (usereg)
            {
                switch (name)
                {
                    case "boxAccessToken": Microsoft.Win32.Registry.SetValue(keyname, "boxAccessToken", encrypted); break;
                    case "boxRefreshToken": Microsoft.Win32.Registry.SetValue(keyname, "boxRefreshToken", encrypted); break;
                }
            }
            else
            {
                switch (name)
                {
                    case "boxAccessToken": Settings.Default.boxAccessToken = encrypted; break;
                    case "boxRefreshToken": Settings.Default.boxRefreshToken = encrypted; break;
                }
                Settings.Default.Save();
            }

            return;
        }
        /// <summary>
        /// Parses valid cammand line arguments and validates preconditions of those arguments.
        /// </summary>
        /// <param name="args">args string from main() to be processed</param>
        static void parseArgs(string[] args)
        {
            int count = 0;
            foreach (String arg in args)
            {
                bool isPath = arg.Split('/').Count() > 1;
                if (arg.Length > 1 && (arg.Substring(0, 1) == "-" || arg.Substring(0, 1) == "/") && !isPath) // Don't treat a /path/to/directory/ as a flag
                {
                    switch (arg)
                    {
                        case "-v":
                        case "--verbose":
                        case "/v":
                        case "/verbose":
                            verbose = true;
                            break;

                        case "-d":
                        case "--debug":
                        case "/d":
                        case "/debug":
                            debug = true;
                            break;

                        case "-q":
                        case "--quiet":
                        case "/q":
                        case "/quiet":
                            quiet = true;
                            break;

                        case "-u":
                        case "--usereg":
                        case "/u":
                        case "/usereg":
                            usereg = true;
                            break;

                        case "-f":
                        case "--force":
                        case "/f":
                        case "/force":
                            force = true;
                            break;


                        default:
                            throw (new System.ArgumentException("Unknown command flag", arg));
                    }

                }
                else
                {
                    switch (count)
                    {
                        case 0: command = arg; count++; break;
                        case 1: argument1 = arg; count++; break;
                        case 2: argument2 = arg; count++; break;
                    }
                    if (count > 3)
                    {
                        throw (new System.ArgumentException("Too many arguments"));
                    }
                }
            }

            int argcount = count - 1;
            // Check Command and argument(s) preconditions
            switch (command)
            {
                // display access token for current windows / box user
                case "token":
                    if (argcount != 0) { throw (new System.ArgumentException("Invalid argument to token command")); }
                    break;

                // directory or file listing. Empty argument = root box directory.
                case "ls":
                case "dir":
                    command = "ls";
                    break;

                case "get":
                case "mget":
                    if (argcount != 2) { throw (new System.ArgumentException("Invalid argument to {0} command, source and destination arguments required", command)); }
                    if (argument2.Last() != '\\') { argument2 = argument2 + "\\"; } // Make sure the destination ends in a directory \ character
                    break;

                default:
                    //throw (new System.ArgumentException("Unknown Command", command));
                    break;

            }
        }
        /// <summary>
        /// Main entry point of the BoxDataSync.exe app.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>0 on success, 1 on any failure.</returns>
        static int Main(string[] args)
        {
            try
            {
                parseArgs(args);
            }
            catch (System.ArgumentException e)
            {
                if (!quiet)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                    return 1;
                }
            }

            String boxAccessToken = "";
            String boxRefreshToken = "";

            switch (command)
            {
                case "test":
                    // Just a place to test and play with code
                    /*
                    Console.WriteLine("Size 1234567: {0}", SizeSuffix(1234567, 2));
                    Console.WriteLine("Press Enter key to continue...");
                    Console.ReadLine();
                    */

                    Console.WriteLine("Working.");
                    return 0;

                case "token":
                    // Displays current token for troubleshooting / determining if token is stored
                    boxAccessToken = getSetting("boxAccessToken");
                    if (Program.quiet)
                    {
                        Console.Write(boxAccessToken);
                    }
                    else
                    {
                        Console.WriteLine("Settings token check: {0}", boxAccessToken);
                    }
                    return 0;

                case "settings2reg":
                    // send settings to reg
                    usereg = false;
                    boxAccessToken = getSetting("boxAccessToken");
                    boxRefreshToken = getSetting("boxRefreshToken");
                    setSetting("boxAccessToken", "");
                    setSetting("boxRefreshToken", "");

                    usereg = true;
                    setSetting("boxAccessToken", boxAccessToken);
                    setSetting("boxRefreshToken", boxRefreshToken);
                    if (verbose)
                    {
                        Console.WriteLine("Copied boxAccessToken {0} and boxRefreshToken {1} to the Registry", boxAccessToken, boxRefreshToken);
                    }
                    else
                    {
                        if (!quiet)
                        {
                            Console.WriteLine("Setting copied from user.config to registry.");
                        }
                    }
                    return 0;

                case "reg2settings":
                    // send reg to settings
                    usereg = true;
                    boxAccessToken = getSetting("boxAccessToken");
                    boxRefreshToken = getSetting("boxRefreshToken");
                    setSetting("boxAccessToken", "");
                    setSetting("boxRefreshToken", "");
                    usereg = false;
                    setSetting("boxAccessToken", boxAccessToken);
                    setSetting("boxRefreshToken", boxRefreshToken);
                    if (verbose)
                    {
                        Console.WriteLine("Copied boxAccessToken {0} and boxRefreshToken {1} to the user.config", boxAccessToken, boxRefreshToken);
                    }
                    else
                    {
                        if (!quiet)
                        {
                            Console.WriteLine("Setting copied from registry to user.config.");
                        }
                    }
                    return 0;


                case "encrypt":
                    // This command is depricated - early test versions of the app didn't use encrypted storage. This is a
                    // one time use case when moving to encrypted tokens, read without encryption, write normally encrypted.
                    boxAccessToken = getSetting("boxAccessToken",false);
                    boxRefreshToken = getSetting("boxRefreshToken", false);
                    setSetting("boxAccessToken", boxAccessToken);
                    setSetting("boxRefreshToken", boxAccessToken);
                    return 0;


                case "logout":
                    // Clears boxAccessToken and boxRefreshToken - effectively logging out of box.com
                    if (!Program.quiet)
                    {
                        Console.WriteLine("Logging out of Box.com");
                    }
                    setSetting("boxAccessToken", "");
                    setSetting("boxRefreshToken", "");
                    return 0;

                case "login":
                    // Logs into box.com. Launches a web browser to complete OAUTH2 process. Launches a webserver listener on 
                    // http://localhost:4000 to receive the OAUTH2 code, which is then traded for access and refresh tokens by the listener

                    // Clear settings 
                    setSetting("boxAccessToken", "");
                    setSetting("boxRefreshToken", "");

                    if (!Program.quiet)
                    {
                        Console.WriteLine("Please log into Box.com in the web browser I'm opening for you.");
                    }
                    String url = String.Format("https://account.box.com/api/oauth2/authorize?response_type=code&client_id={0}&redirect_uri={1}&state=oob", Program.boxClientId, Program.boxRedirectURI);
                    System.Diagnostics.Process.Start(url);
                    WebServer ws = new WebServer(Program.SendResponse, "http://localhost:4000/");
                    ws.Run();

                    while (ws.RequestsProcessed() == 0) {; }
                    ws.Stop();
                    return 0;

                case "bootstrap":
                    // Permits command line processing of OAUTH2 login process. If sent no argument, displays a URL to initiate the login.
                    // When passed an OAUTH2 code as the only argument, it tries to exchange that code for access and refresh tokens
                    // ex: BoxDataSync.exe bootstrap
                    // ex: BoxDataSync.exe bootstrap [code_copied_from_query_string_of_reply]
                    if (argument1 == "")
                    {
                        Usage();
                    }
                    else
                    {
                        BoxSyncHelper.Bootstrap(argument1);
                    }
                    return 0;

                case "ls":
                    // lists the contents of a named Box.com directory. Also permits a numeric folder ID as returned from box.com. 0 = root folder
                    if (argument1 == "") { argument1 = "0"; }
                    try
                    {
                        long id = 0;
                        bool sentID = true;
                        String folderid = argument1;
                        if (!Int64.TryParse(argument1, out id))
                        {
                            sentID = false;
                            folderid = BoxSyncHelper.GetDirectoryIdByPath(argument1);
                        }

                        String data = BoxSyncHelper.GetFolderById(folderid);
                        JObject jObject = JObject.Parse(data);

                        String folderName = jObject["name"].ToString();
                        String folderSize = "-";

                        // Calculate a size string
                        long size = 0;
                        if (Int64.TryParse(jObject["size"].ToString(), out size))
                        {
                            folderSize = SizeSuffix(size, 3);
                        }


                        Console.WriteLine("Directory of: #{0,15} {1,10} \"{2}\"", folderid, folderSize, sentID ? folderName : argument1);


                        JArray jArray = jObject["item_collection"]["entries"] as JArray;
                        foreach (JObject item in jArray)
                        {
                            switch (item.GetValue("type").ToString())
                            {
                                case "file":
                                    String fileData = BoxSyncHelper.GetFileById(item.GetValue("id").ToString());
                                    JObject fileObject = JObject.Parse(fileData);
                                    String size1 = fileObject.GetValue("size").ToString(); //SizeSuffix(Int64.Parse(item.GetValue("size").ToString()),0)
                                    Console.WriteLine(" - {0,15} {1,10} {2} ({3})", item.GetValue("id"), SizeSuffix(Int64.Parse(size1), 2), item.GetValue("name"), fileObject["owned_by"]["name"].ToString());

                                    break;
                                case "folder":
                                    String folderData = BoxSyncHelper.GetFolderById(item.GetValue("id").ToString());
                                    JObject folderObject = JObject.Parse(folderData);
                                    String size2 = folderObject.GetValue("size").ToString();
                                    Console.WriteLine(" d {0,15} {1,10} {2} ({3})", item.GetValue("id"), SizeSuffix(Int64.Parse(size2), 2), item.GetValue("name"), folderObject["owned_by"]["name"].ToString());
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Program.quiet)
                        {
                            Console.WriteLine(e.Message);
                            return (1);
                        }
                    }

                    return 0;

                case "get": 
                    // download a single file by file ID or full path
                    try
                    {
                        Program.localWorkingDirectory = argument2;
                        long id = 0;
                        String fileid = argument1;
                        if (!Int64.TryParse(argument1, out id))
                        {
                            fileid = BoxSyncHelper.GetFileIdByPath(argument1);
                            if (fileid.Length < 1)
                            {
                                throw new Exception(String.Format("File not found: {0}", argument1));
                            }
                        }


                        String fileData = BoxSyncHelper.GetFileById(fileid);
                        JObject fileObject = JObject.Parse(fileData);
                        downloadFile(fileObject);
                    }
                    catch (Exception e)
                    {
                        if (!Program.quiet)
                        {
                            Console.WriteLine(e.Message);
                            return (1);
                        }
                    }
                    return 0;

                case "mget": 
                    // multiple file download given directory ID or full path
                    try
                    {
                        long id = 0;
                        String folderid = argument1;
                        if (!Int64.TryParse(argument1, out id))
                        {
                            folderid = BoxSyncHelper.GetDirectoryIdByPath(argument1);
                        }



                        String data = BoxSyncHelper.GetFolderById(folderid); // 18128962172
                        Program.localWorkingDirectory = argument2;
                        JObject jObject = JObject.Parse(data);
                        JArray jArray = jObject["item_collection"]["entries"] as JArray;

                        String folderName = jObject.GetValue("name").ToString();
                        if (!Program.quiet)
                        {
                            Console.WriteLine("Downloading all files in folder \"{0}\"", folderName);
                        }

                        foreach (JObject item in jArray)
                        {
                            switch (item.GetValue("type").ToString())
                            {
                                case "file":
                                    downloadFile(item);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Program.quiet)
                        {
                            Console.WriteLine(e.Message);
                            return (1);
                        }
                    }

                    return 0;

                default: return 1;
            }
        }
    }
}
