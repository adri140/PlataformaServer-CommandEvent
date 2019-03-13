using Pdc.Denormalization;
using Pdc.Messaging;
using PlataformaServerCommandEvent.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlataformaServerCommandEvent.Denormalizadores
{
    class WebUserDenormalizer : Denormalizer<WebUser>, IEventHandler<WebUserCreated>, IEventHandler<WebUserUpdated>, IEventHandler<WebUserDeleted>
    {
        public WebUserDenormalizer(PurchaseOrdersDbContext dbContext) : base(dbContext) { }

        public async Task HandleAsync(WebUserCreated message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Guardado usuario creado - evento");
            DbContext.Set<WebUser>().Add(new WebUser { Id = message.Id, username = message.username, usercode = message.usercode });
            await DbContext.SaveChangesAsync();
        }

        public async Task HandleAsync(WebUserUpdated message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("actualizando usuario creado - evento");
            var user = await DbContext.Set<WebUser>().FindAsync(message.Id);
            user.username = message.username;
            await DbContext.SaveChangesAsync();
        }

        public async Task HandleAsync(WebUserDeleted message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("eliminando usuario creado - evento");
            var user = await DbContext.Set<WebUser>().FindAsync(message.Id);
            //hemmmmm aqui deveria de haver un booleano que indica si esta eliminado o no?????
            await DbContext.SaveChangesAsync();
        }
    }
}
