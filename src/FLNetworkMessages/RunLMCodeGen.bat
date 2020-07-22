REM No NRT support in Unity, so code is generated twice, once with NRT and once without

..\..\tools\lm-compiler\LightMessage.Compiler.exe messages.lm -nrt -simpl=..\FLGrains\CodeGen\Messages.g.cs -siface=..\FLGrainInterfaces\CodeGen\Messages.g.cs

..\..\tools\lm-compiler\LightMessage.Compiler.exe messages.lm messages.lm "-c=..\..\..\FLClient\Assets\Scripts\Network\ConnectionManager.cs"
