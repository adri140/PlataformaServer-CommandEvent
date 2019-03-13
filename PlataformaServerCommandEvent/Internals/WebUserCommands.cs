using Pdc.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlataformaServerCommandEvent.Internals
{
    public class CreateWebUser : Command
    {
        public CreateWebUser(string aggregateId) : base(aggregateId, null)
        {

        }

        public string username { get; set; }
        public string usercode { set; get; }
    }

    public class UpdateWebUser : Command
    {
        public UpdateWebUser(string aggregateId) : base(aggregateId, null)
        {

        }

        public string username { set; get; }
    }

    public class DeleteWebUser : Command
    {
        public DeleteWebUser(string aggregateId) : base(aggregateId, null)
        {

        }
    }
}
