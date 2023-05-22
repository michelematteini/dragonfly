@echo off
setlocal
:PROMPT
SET /P AREYOUSURE=Clean Solution (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

rd /s /q "Dragonfly.Tools\bin"
rd /s /q "Dragonfly.Engine.BaseModule\bin"
rd /s /q "Dragonfly.Engine.Core\bin"
rd /s /q "Dragonfly.Engine.Test\bin"
rd /s /q "Dragonfly.Graphics\bin"
rd /s /q "Dragonfly.Graphics.Test\bin"
rd /s /q "Dragonfly.Graphics.Wrappers\bin"
rd /s /q "Dragonfly.Utils\bin"
rd /s /q "Dragonfly.Utils.Win32\bin"
rd /s /q "Dragonfly.Utils.Forms\bin"

rd /s /q "Dragonfly.Tools\obj"
rd /s /q "Dragonfly.Engine.BaseModule\obj"
rd /s /q "Dragonfly.Engine.Core\obj"
rd /s /q "Dragonfly.Engine.Test\obj"
rd /s /q "Dragonfly.Graphics\obj"
rd /s /q "Dragonfly.Graphics.Test\obj"
rd /s /q "Dragonfly.Graphics.Wrappers\obj"
rd /s /q "Dragonfly.Utils\obj"
rd /s /q "Dragonfly.Utils.Win32\obj"
rd /s /q "Dragonfly.Utils.Forms\obj"

:END
endlocal
pause
