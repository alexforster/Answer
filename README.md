## Answer.dll
A lightweight .NET 2.0 library for parsing hierarchical configuration files into a key/value tree

## Quick Start
```Ruby
# program.cfg
# Example main configuration file

logfile log.txt

balancer round-robin

@include servers.cfg
```
```Ruby
# servers.cfg
# Example included configuration file

servers
	server 10.10.1.100:80
	server 10.10.2.100:80
	server 10.10.3.100:80
```
```csharp
// Program.cs
// Example usage of program.cfg

using System;
using System.IO;
using System.Linq;

using Answer;

internal static class Program
{
	public static void Main()
	{
		var config = new AnswerParser();

		var configFile = new FileInfo( "program.cfg" );

		using( var configText = configFile.OpenText() )
		{
			config.Parse( configText.ReadToEnd() );
		}

		var logFile =
			config.Answers
			      .First( answer => answer.Name.Equals( "logfile" ) )
			      .Value;

		// logFile == "log.txt"

		var servers =
			config.Answers
			      .First( answer => answer.Name.Equals( "servers" ) )
			      .Children
			      .Where( answer => answer.Name.Equals( "server" ) )
			      .Select( answer => answer.Value )
			      .ToArray();
		
		// servers == [ "10.10.1.100:80", "10.10.2.100:80", "10.10.3.100:80" ]

		var balancer =
			config.Answers
			      .First( answer => answer.Name.Equals( "balancer" ) )
			      .Value;

		// balancer == "round-robin"

		var serializedConfig = config.Serialize();

		// serializedConfig == 
		//     "logfile log.txt\r\n" +
		//     "balancer round-robin\r\n" +
		//     "servers\r\n" +
		//     "	server 10.10.1.100:80\r\n" +
		//     "	server 10.10.2.100:80\r\n" +
		//     "	server 10.10.3.100:80"
	}
}
```
