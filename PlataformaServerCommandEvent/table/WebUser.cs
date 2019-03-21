using Pdc.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlataformaServerCommandEvent.table
{
    class WebUser : View
    {
        public virtual string Id { get; set; }
        public virtual string Usercode { get; set; }
        public virtual string Username { get; set; }
    }
}
