﻿using System;
using Autofac;
using Common.Logging;
using Rebus;

namespace ScriptCs.Rebus.Hosting
{
    public class ScriptHandler : IHandleMessages<Script>
    {
        public void Handle(Script message)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new ScriptModule());

            using (var container = builder.Build())
            {
                using (var scope = container.BeginLifetimeScope())
                {
                    var logger = scope.Resolve<ILog>();
                    var executor = scope.Resolve<ScriptExecutor>();

                    try
                    {
                        Console.WriteLine("Before execute");
                        executor.Execute(message.ScriptContent);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        throw;
                    }
                }
            }
        }
    }
}