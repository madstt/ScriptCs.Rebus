using System;
using System.IO;
using Newtonsoft.Json;
using Rebus;
using Rebus.AzureServiceBus;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Rebus.Transports.Msmq;
using ScriptCs.Rebus.Logging;
using ScriptCs.Rebus.Scripts;

namespace ScriptCs.Rebus.Hosting.ScriptHandlers.WebApi
{
    public class ReceiveWebApiControllerHandler : IHandleMessages<WebApiControllerScript>
    {
	    public void Handle(WebApiControllerScript message)
	    {
		    var connectionString =
		    MessageContext.GetCurrent().Headers.ContainsKey("connectionString")
			    ? MessageContext.GetCurrent().Headers["connectionString"].ToString()
			    : string.Empty;
		    
			var bus =
			    CreateReplyBus(
				    MessageContext.GetCurrent().Headers["transport"].ToString(),
				    connectionString);
				    
					bus.Advanced.Routing.Send(MessageContext.GetCurrent().ReturnAddress, new ScriptExecutionConsoleOutput("Script received..."));

		    if (message == null) throw new ArgumentNullException("message");

		    if (message.ControllerName != null && message.ControllerName.ToLower().EndsWith("controller"))
	        {
		        message.ControllerName = message.ControllerName.Substring(0,
			        message.ControllerName.IndexOf("controller"));
	        }

		    if (string.IsNullOrWhiteSpace(message.ControllerName))
		    {
			    message.ControllerName = "Scripted";
		    }

		    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Scripts"));
		    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("bin\\Scripts\\{0}Controller.csx", message.ControllerName));
		    File.WriteAllText(path, message.ScriptContent);

		    var metaDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
			    string.Format("bin\\Scripts\\{0}Controller.csx.metadata", message.ControllerName));

		    File.WriteAllText(metaDataPath,
				string.Format("{0} | {1} | {2} | {3}",
				    MessageContext.GetCurrent().ReturnAddress,
				    MessageContext.GetCurrent().Headers["transport"],
				    JsonConvert.SerializeObject(message),
				    connectionString));

			bus.Advanced.Routing.Send(MessageContext.GetCurrent().ReturnAddress, new ScriptExecutionConsoleOutput("... script saved."));
        }

		private IBus CreateReplyBus(string transport, string connectionString = null)
		{
			Action<RebusTransportConfigurer> transportConfig;
			switch (transport)
			{
				case "AZURE":
					transportConfig =
						configurer => configurer.UseAzureServiceBusInOneWayClientMode(connectionString);
					break;
				case "RABBIT":
					transportConfig = configurer => configurer.UseRabbitMqInOneWayMode(connectionString);
					break;
				default:
					transportConfig = configurer => configurer.UseMsmqInOneWayClientMode();
					break;
			}

			return Configure.With(new BuiltinContainerAdapter())
				.Transport(transportConfig)
				.CreateBus()
				.Start();
		}

    }
}