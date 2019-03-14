using Pdc.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlataformaServerCommandEvent.Denormalizadores
{
    public class WebUser : View
    {
        public virtual string Id { set; get; }
        public virtual string username { set; get; }
        public virtual string usercode { set; get; }
    }
}
