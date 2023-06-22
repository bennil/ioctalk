cd ..
dotnet build -c Debug -p:CodeGen=true

cd BSAG.IOCTalk.Communication.NetTcp
dotnet pack -c Debug --no-build -p:NuspecFile=ioctalk-codegen-binary-json-tcp-debug.nuspec