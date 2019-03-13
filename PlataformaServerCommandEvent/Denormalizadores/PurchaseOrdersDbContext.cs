using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlataformaServerCommandEvent.Denormalizadores
{
    class PurchaseOrdersDbContext : DbContext
    {
        public PurchaseOrdersDbContext(DbContextOptions<PurchaseOrdersDbContext> options) : base(options)
        {
        }
        
        public DbSet<WebUser> WebUsers { get; set; }
    }
}
