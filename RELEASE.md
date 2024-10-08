# Wacom Ink SDK for Multi-Display

## Version 2.0.7

## History

### 2.0.7  27 September 2024
  - Updated the SDK to be based on .NET 8
  - Updated license structure and config
  - Reworked adding extra data to signature method
  - New app config settings:
    - SecureCommunication: Boolean value that when true, will look for a certificate
    - UseLocalCertificate: Boolean value that when true, reads a certificate from a machine/user store
    - TrustedServers: Array containing whitelisted server instances. NB: replaces old setting ServerCertificates.
  - Bug fixes including:
    - Fix to latency issue
    - Fix to button not correctly raising event

### 1.4.2  08 July 2024
  - New app config settings:
    - UseNamedPipe: boolean that doesn't utilize TcpPort when true
    - ClientCertificateThumbprint: certificate thumbprint for added security
    - WindowBgColor: global parameter that adds 
  - New IdleConfig parameter:
    - BackgroundColor: default background added for IdleConfig  
  - New Open Idle parameter WithBackGroundColor to set background color

### 1.3.5  12 April 2024
  - Added Citrix support for multi-user access 

### 1.3.3 19 January 2024
  - Added support for signature document binding 
  - Added multiplier to scroll amounts in SendMouseWheelEvent() call
  - Improved positioning and sizing of CheckBoxes & RadioButtons

### 1.3.2 24 November 2023
  - Added ISO binary encryption

### 1.3.1 30 October 2023
  - Bug fix where PDFs were unable to be deleted after opening them within the sample app

### 1.3.0 26 June 2023
  New app config settings:
  - BrowserCachePath. This value allows users to set the file directory to keep cached data. Default value = null.
  - Extended OpenWebMessage to include an Incognito mode parameter
  Added message for:
  - ClearBrowserCookiesMessage
  - BrowserNavigateToMessage is now deprecated

  ### 1.2.1  06 October 2022

  - Bug fix: Cef Browser was not displaying some Web content

  ### 1.2.0  26 April 2022
  - Added messages for:
	  - SetElementValueMEssage
	  - SetDocumentZoomMessage
	  - ShowDialogMessage
	  - DismissDialogMessage

  - Added config setting:
	  - FatalErrorMessageBox
    
  - Enhancements to thumbnails
  - Improved user navigation between fields
  - Bug fixes including issues with zooming and mirroring

### 1.0.11  19 Jan  2022
  - Added support for Creative versions of the DTK-1660
  - Added support for Citrix

### 1.0.10  27 July  2021
  - Add config option to control display of message box when the tray app closes itself down
    setting: FatalErrorMessageBox

### 1.0.9 15 July 2021
  - Bug fixes 
  - Client v1.0.9
  - Nuget v1.0.9
    
### 1.0.8 06 July 2021
  - Bug fixes
  - Client v1.0.8
  - Nuget v1.0.8

### 1.0.6   29 March 2021
  - Bug fixes
  - Client v1.0.6
  - NuGet  v1.0.6

### 1.0.5   16 March  2021
  - Bug fixes

### 1.0.4   28 January  2021
  - Repackaged zip
  - Client v1.0.1
  - NuGet  v1.0.1.5
  
### 1.0.3   January  2021
  - Bug fixes including media and keyboard layout

### 1.0.2   December 2020
  - Bug fixes including drop-down selection, touch tablet support
  
### 1.0.1   03 November 2020
  - Initial QA release

### 1.0.0   30 July 2020
  - Initial beta release
  
