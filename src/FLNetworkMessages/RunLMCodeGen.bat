REM No NRT support in Unity, so code is generated twice, once with NRT and once without

dotnet "..\..\..\LightMessage-Core\LightMessage.Compiler\bin\Release\netcoreapp2.1\LightMessage.Compiler.dll" messages.lm -nrt -simpl=..\FLGrains\CodeGen\Messages.g.cs -siface=..\FLGrainInterfaces\CodeGen\Messages.g.cs

dotnet "..\..\..\LightMessage-Core\LightMessage.Compiler\bin\Release\netcoreapp2.1\LightMessage.Compiler.dll" messages.lm "-c=D:\UnityProjects\FLClient\Assets\Scripts\Network\ConnectionManager.cs"
