# IOC-Talk

Nuget Package: [ioctalk](https://www.nuget.org/packages/ioctalk-standard/)

##Short example

Orchestration:
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

Functional interface assembly:
```
	public interface IMySuperService
	{
		// whatever
	}
```

```
	public interface IMySupremeRemoteClientService
	{
		// whatever
	}
```

How can you react to distributed events (remote endpoint session changes) in your business code without having a dependency to the underlying transfer stack?
The ioctalk solution is "constructor out delegate" injection and a bit of convention:

Functional service implementation assembly:
```
	public class MySuperService : IMySuperService
	{
		public MySuperService(out Action<IMySupremeRemoteClientService> clientServiceCreated, out Action<IMySupremeRemoteClientService> clientServiceTerminated)
		{
			clientServiceCreated = OnClientServiceCreated;
            clientServiceTerminated = OnClientServiceTerminated;
		}

		private void OnClientServiceCreated(IMySupremeRemoteClientService client)
		{
			// my remote (or local - depending on the orchestration) client service instance
		}

		private void OnClientServiceTerminated(IMySupremeRemoteClientService client)
		{
		}
	}
```
By convention the ioctalk dependency injection container needs a "Created" or "Terminated" at the end of the method name.

Now you have separated your business code from any technical dependency. You can use it with ioctalk, within a unit test or some future transfer technology.


More info (old version 1 article): https://www.codeproject.com/Articles/1095181/Invisible-Interprocess-Communication
(Service registration outdated - new examples/article pending!)
