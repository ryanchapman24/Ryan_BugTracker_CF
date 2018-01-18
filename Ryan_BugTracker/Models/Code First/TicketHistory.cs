using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Ryan_BugTracker.Models
{
    public class TicketHistory
    {
        public int Id { get; set; }
        [Required]
        public string Property { get; set; }
        public string Dialogue { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTimeOffset Changed { get; set; }
        public string UserId { get; set; }
        public int TicketId { get; set; }


        public virtual ApplicationUser User { get; set; }

        public virtual Ticket Ticket { get; set; }
    }
}