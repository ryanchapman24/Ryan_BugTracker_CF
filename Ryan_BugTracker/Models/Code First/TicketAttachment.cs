using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Ryan_BugTracker.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }
        public DateTimeOffset Created { get; set; }      
        public string Body { get; set; }
        public string FileUrl { get; set; }
        public string AuthorUserId { get; set; }
        public int TicketId { get; set; }


        public virtual ApplicationUser AuthorUser { get; set; }

        public virtual Ticket Ticket { get; set; }
    }
}