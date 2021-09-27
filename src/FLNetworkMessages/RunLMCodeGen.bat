REM No NRT support in Unity, so code is generated twice, once with NRT and once without

dotnet lmc messages.lm -nrt -simpl=..\FLGrains\CodeGen\Messages.g.cs -siface=..\FLGrainInterfaces\CodeGen\Messages.g.cs

dotnet lmc messages.lm "-c=..\..\..\FLClient\Assets\Scripts\Network\ConnectionManager.cs"
