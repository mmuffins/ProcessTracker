using Autofac;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessTrackerService.Infrastructure
{
    public class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TagSessionRepository>().As<ITagSessionRepository>().InstancePerLifetimeScope();
        }
    }
}
