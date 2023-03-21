@ECHO OFF
Tools\NAnt\bin\nant build
IF %ERRORLEVEL% NEQ 0 PAUSE
