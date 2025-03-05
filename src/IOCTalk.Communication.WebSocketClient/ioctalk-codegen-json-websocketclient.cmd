cd ..
dotnet build -c Release -p:CodeGen=true

cd IOCTalk.Communication.WebSocketClient
dotnet pack -c Release --no-build -p:NuspecFile=ioctalk-codegen-json-websocketclient.nuspec