# SimpleBlock

# NOTE
Before using this program PLEASE Disable windows DNS Cache Service if you have a windows version that allows to stop or Disable the service then go ahead  
The tool does have built in Buttons in the ToolBox tab to Disable Windows DNS Cache it requires a Restart each time  
Having that enabled with a Host file with over 100k+ Lines will Slow down your network to a point making it un-usable  
If you wish to do this your self Modify  
HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Dnscache -> Start -> 4 (Set the Start key value to 4 then restart your PC)  
Note SOME applications may not Work Disabling Windows DNS Cache so if you can notify me the list I would be happy  
Some applications can be fixed like Proton VPN (Change protocal from Smart to anything else) besides that I had no issues with Applications (Windows store may be one)  

# Bugs

* Cross Thread UI issue (when an non UI thread talks with UI elements on the UI thread) I had a fix but those have their own errors since the UI is not present  
* Rare Program Lock (Program just does nothing til you terminate it) 
* Help Popup does not go away just click the Text area til it disapears that happens sometimes you will know when it does

# Features

* Update Host From Repos
* Undetected By Browser
* Duplicate Remover
* Auto Updater
* Clear Host & Make Backups
* Full Logs of Everything
* DNS Cache Disabler / Enabler (Requires Restart)
* Allowed List to allow certain Hosts / URLs to go through
* Parse DNSMasq & AdBlock Formatting as well as couple other custom Formatting
* Pause Host Blocker
* Blocks ads across all browsers and Applications on the System as it runs on the System
* Made with Love <3

# To Do

* Add Startup Feature
* Add more Performace Code
* Add 32 Bit View of Host (If needed)
* Add File Repo instead of URLs
* Make the About me more Clean and UI cleaner

# How to Use

Run the program as admin (Only programs with Admin Rights can modify the Host File)  
Go into the Repo Tab and Add a new Direct Link to a Repo List of Hosts to Block (a long text file/ list of Hosts to block)  
If you are too lazy to google them here are a few : https://github.com/0bbedCode/AntiHell  
The in the Repo Tab Select Update All or select a single repo and select Update "Start"  

Note I use Context Menu Strips so Right Clicking on the ListView will bring up the "buttons/options"

# Inspiration

I wanted to make this UI VERY Simple so stuck with default WinForms  
Simplewall from Henry++ & AdAway for Android was my Inspiration for making this tool for windows  
Also with ManifestV3 AdBlockers on Chromium Based Browsers are Nerfed

For any mispells or errors im sorry i am not the best with English I try 


