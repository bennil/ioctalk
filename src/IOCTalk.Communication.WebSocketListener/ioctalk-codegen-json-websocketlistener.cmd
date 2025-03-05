cd ..
dotnet build -c Release -p:CodeGen=true

cd IOCTalk.Communication.WebSocketListener
dotnet pack -c Release --no-build -p:NuspecFile=ioctalk-codegen-json-websocketlistener.nuspec