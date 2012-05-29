@ECHO OFF
CLS

IF "%1%"=="H2" GOTO H2Viewer

REM This is an example script to show how to use the Help Library Manager
REM Launcher to remove an MS Help Viewer file.  You can use this as an example
REM for creating a script to run from your product's uninstaller.

REM NOTE: If not executed from within the same folder as the executable, a
REM full path is required on the executable.

HelpLibraryManagerLauncher.exe /product "VS" /version "100" /locale en-us /uninstall /silent /vendor "Vendor Name" /mediaBookList "A Sandcastle Documented Class Library" /productName "A Sandcastle Documented Class Library"

GOTO Exit

:H2Viewer

REM The Help Library Manager Launcher tool does not support MS Help Viewer 2 yet so this calls the tool directly
REM for temporary support.

"%SYSTEMDRIVE%\Program Files\Microsoft Help Viewer\v2.0\HlpCtntMgr.exe" /operation uninstall /catalogName VisualStudio11 /locale en-us /vendor "Vendor Name"  /productName "A Sandcastle Documented Class Library" /bookList "A Sandcastle Documented Class Library"

:Exit
