cd ..
dotnet build -c Release -p:CodeGen=true

cd BSAG.IOCTalk.Communication.NetTcp
dotnet pack -c Release --no-build -p:NuspecFile=ioctalk-codegen-binary-tcp.nuspec