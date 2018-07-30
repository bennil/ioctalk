# IOC-Talk
Nuget Package: [ioctalk](https://www.nuget.org/packages/ioctalk-standard/)

More info: https://www.codeproject.com/Articles/1095181/Invisible-Interprocess-Communication
(Service registration outdated - new examples/article pending!)

Short example:
```
            compositionHost = new TalkCompositionHost();
            compositionHost.AddExecutionDirAssemblies();

            compositionHost.RegisterLocalSharedService<IMySuperService>();
            compositionHost.RegisterRemoteService<IMySupremeRemoteClientService>();

            tcpBackendService = new TcpCommunicationController();
            tcpBackendService.LogDataStream = true;

            compositionHost.InitGenericCommunication(tcpBackendService);

            tcpBackendService.InitService(52478);
```


