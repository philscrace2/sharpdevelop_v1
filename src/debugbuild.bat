@ECHO OFF
Tools\NAnt\bin\nant -D:debug=true -D:startuptarget=exe
IF %ERRORLEVEL% NEQ 0 PAUSE
