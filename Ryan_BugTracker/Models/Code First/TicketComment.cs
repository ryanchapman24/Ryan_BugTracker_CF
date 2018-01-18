using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ryan_BugTracker.Models
{
    public class TicketComment
    {
        public int Id { get; set; }       
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        [Required]
        public string Body { get; set; }
        public string AuthorUserId { get; set; }
        public int TicketId { get; set; }


        public virtual ApplicationUser AuthorUser { get; set; }

        public virtual Ticket Ticket { get; set; }
    }
}