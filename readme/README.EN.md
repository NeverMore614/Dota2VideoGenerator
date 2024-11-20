## Dota2 Kill Highlights Generator Instructions for Use
## Thank for the video analysis project: https://github.com/dotabuff/manta
### you need to download
- PC generation tool + server：MetaDota.zip
- Android mobile application：MetaDota.apk
### you need to prepare
- windows 7 and above operating system (required)
- dota2 client (required)
- Steam account with verification method (email or double-ended), it seems ok without it (required)
- Android device mobile phone (not necessary)
- network
### Deployment instructions for initial startup on the computer
- 1.Unzip the MetaDota.zip compressed package and place it in any directory
- 2.The first step is to install the simulated keyboard driver, which is used to simulate the keyboard pressing operation in the game. Open the command prompt as an administrator, enter "{MetaDota installation path}\install-interception.exe /install", and press Enter to install , please restart the computer after successful installation, as shown in the figure below
![](/img/shot1.PNG "")
- 3.If steam has been started and you have logged in to the steam account we prepared, be sure to close steam, otherwise you will not be able to log in.
- 4.Double-click to open MetaDota.exe. When opening it for the first time, you need to fill in the following information in order.
  
     **1.dota 2 beta path(Right click on dota2 on steam ->Properties->Local file view)<br>2.your steam account<br>3.Your steam account password<br>4.LAN IP address, open the command line and enter ipconfig to view<br>5.Port number (enter it casually, as long as it is not an existing port number. I use 8885 myself)**

     ![](/img/shot2.PNG "")
- 6.Activate the simulation driver, enter any letter and press Enter<br>     ![](/img/shot3.PNG "")
- 7.At this time, hfs will open and click on the first Real folder.<br>     ![](/img/shot6.PNG "")
- 8.At this time, steam will verify your account. If it is email verification, please get the verification code, then copy it and press Enter.（Ignore "waitlogin" uh...）<br>     ![](/img/shot4.PNG "")
- 9.Wait for the connection to the steam network. When the following prompt appears, the startup is successful.<br>     ![](/img/shot5.PNG "")
- The above operations are only required for the first startup. If you find that the filled-in information is incorrect, you can modify the configuration file under {MetaDota path}/config.
- The default key input interval is 500, which is to prevent the client from lagging and causing simulated input failure. You can modify the input interval in {MetaDota path}/config/keyInputDelay.txt
### Precautions
- Please do not do any operations on the computer while the Dota2 client is open to record the screen, as it may interrupt the recording process.
- If you feel that recording video is too slow, you can enter the dota2 video settings to reduce the resolution.
- Keep the mouse in the middle of the screen as much as possible
### Mobile App User Instructions
- 1.Install MetaDota.apk and open
- 2.Enter the setting interface in the upper right corner, enter the IP address and port number configured on the computer in the first line, and enter the IP address and port number of hfs in the second line, as shown in the figure below, so you should enter 192.168.100.123 and 80<br>     ![](/img/shot6.PNG "")
- 3.Enter the requested match ID and your own player ID below, and click Generate
- 4.Go take a shower and wait for the video to be generated
