using Microsoft.Extensions.Logging;
using Pdc.Denormalization;
using Pdc.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PlataformaServerCommandEvent.Internals
{
    class WebUser : AggregateRoot, ISaga<WebUserCreated>, ISaga<WebUserDeleted>, ISaga<WebUserUpdated>
    {
        public WebUser(ILogger<AggregateRoot> logger) : base(logger)
        {

        }

        public string username { get; private set; }
        public string usercode { get; private set; }

        public async Task CreateWebUser(CreateWebUser command)
        {
            Console.WriteLine("Creando evento WebUserCreated");
            await RaiseEventAsync(new WebUserCreated(command.AggregateId, command.username, command.usercode, command));
        }

        void ISaga<WebUserCreated>.Apply(WebUserCreated @event)
        {
            Id = @event.Id;
            username = @event.username;
            usercode = @event.usercode;
        }

        public async Task UpdateWebUser(UpdateWebUser command)
        {
            if (command.AggregateId != Id)
            {
                throw new InvalidOperationException("The command was not sended to this aggregate root.");
            }

            Console.WriteLine("Updateando evento WebUserUpdated");
            await RaiseEventAsync(new WebUserUpdated(Id, command.username, command));
        }

        void ISaga<WebUserUpdated>.Apply(WebUserUpdated @event)
        {
            Id = @event.Id;
            username = @event.username;
        }

        public async Task DeleteWebUser(DeleteWebUser command)
        {
            if (command.AggregateId != Id)
            {
                throw new InvalidOperationException("The command was not sended to this aggregate root.");
            }
            Console.WriteLine("Eliminando evento WebUserDeleted");
            await RaiseEventAsync(new WebUserDeleted(Id, command));
        }

        void ISaga<WebUserDeleted>.Apply(WebUserDeleted @event)
        {
            Id = @event.Id;
        }
    }
}
