BoxDataSync.exe                                                        2/21/2017
 by Jamie Anne Harrell
 Copyright (c) 2017 Emory Goizueta Business School
================================================================================

Welcome to BoxDataSync.exe. This command line utility provides synchronization
of your Box.com files to your local file system ON DEMAND rather than running in
the background constantly while you're logged into your PC.

WHY on earth would you want that? Well, sometimes you want files synced to your
machine when you're not logged in. This is helpful when you have scheduled tasks
that need to run and see your latest data even when you're not logged in. Simply
create a Task Scheduler job to run this periodically to keep your files sync'd
at any time - even if you're not there. Even if you're not logged in!


Requirements:
-------------
 - VC# / Visual Studio 2015
 - .NET Framework 4.5.2
 - Box.com Account with API Access (Box.com API v2.0)
 - Your own Box.com Appliation ID and Secret
 - Powershell to run pre-build/post-build event commands
 - References: Newtonsoft.Json NuGet Package ( Tools -> NuGet Package Manager)


Compiling and Installation:
---------------------------
Step 1) Create Box.com App - https://docs.box.com/docs/overview
 - Create your App: https://app.box.com/developers/services/edit/
 - Generate your Client Secret
 - Set your Redirect URL to: http://localhost:4000
 - Authentication Type: Standard Authentication (3-legged OAuth2.0)
 - Scope: Content. Read and write all files and folders stored in Box


Step 2) Put your Box App ID and Client secret in a NEW file in the
   project directory named api.user which contains only 2 lines. This
   goes in the same folder as Program.cs. The build utilities included
   in this distrubution will use the variable replacements defined in
   that file to set the application boxClientId and boxClientSecret
   variables found in ApiSettings.cs. The api.user file is NOT distributed
   intentionally - it has my credentials in it and I didn't want to
   check those into GIT. By default *.user files are in .gitignore so 
   you can't see my version. <grin>

   *Note: I think this is a pretty cool way to provide old-school
   "macro" support in VC#, and it keeps my API client secret and 
   App Client ID out of source control. We used to do these things
   in make files with user environment variables, but in the day of
   managed code, sometimes features of the build system don't have
   exactly what you want... see my post online about this, I think
   you'll find its pretty cool! Never tell an engineer it can't be
   done!

   Sample api.user file:
   ---------------------
   _BOXCLIENTID=<your_oauth2_client_id>
   _BOXCLIENTSECRET=<your_oauth2_client_secret>


Step 3) Build BoxDataSync. Build Setup. Use setup to install so you
   get the necessary registry keys. 
   
   *Note: DONE! But if you didn't want to install with the setup, 
   you need to manually create these empty registry keys:

   Registry Key:
   HKEY_CURRENT_USER\SOFTWARE\Goizueta Business School\BoxDataSync\
    Name               Type          Data
	(Default)          REG_SZ        (value not set)
	boxAccessToken     REG_SZ        <leave blank>
	boxRefreshToken    REG_SZ        <leave blank>


Usage examples:
---------------
See Program.cs for detailed usage and command line options/parameters/commands. Start with BoxDataSync.exe login
C:> "C:\Program Files (x86)\Goizueta Business School\BoxDataSync\BoxDataSync.exe" login
C:> "C:\Program Files (x86)\Goizueta Business School\BoxDataSync\BoxDataSync.exe" ls
C:> "C:\Program Files (x86)\Goizueta Business School\BoxDataSync\BoxDataSync.exe" ls "\Path\to\box\directory"
C:> "C:\Program Files (x86)\Goizueta Business School\BoxDataSync\BoxDataSync.exe" get "\Path\to\box\file.xlsx" c:\path\to\windows\directory
C:> "C:\Program Files (x86)\Goizueta Business School\BoxDataSync\BoxDataSync.exe" mget "\Path\to\box\folder" c:\path\to\windows\directory


Version History:
----------------
 - 5/9/2019:  v1.0.0.1 - TLSv1.2 enabled
 - 2/21/2017: v1.0.0.0 - initial release



 ~=-=-=-=-=-=-=-=-=-=~
~ Girls Who Code Rock ~
 ~=-=-=-=-=-=-=-=-=-=~