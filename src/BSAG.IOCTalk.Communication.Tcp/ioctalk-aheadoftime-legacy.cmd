cd ..
dotnet build -c Release -p:AheadOfTimeOnly=true

cd BSAG.IOCTalk.Communication.Tcp
dotnet pack -c Release --no-build -p:NuspecFile=ioctalk-aheadoftime-legacy.nuspec