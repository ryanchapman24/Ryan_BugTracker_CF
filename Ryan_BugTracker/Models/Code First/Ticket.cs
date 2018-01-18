using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ryan_BugTracker.Models
{
    public class Ticket
    {
        public Ticket()
        {
            this.TicketComments = new HashSet<TicketComment>();
            this.TicketAttachments = new HashSet<TicketAttachment>();
        }

        public int Id { get; set; }     
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Body { get; set; }      
        public string AuthorUserId { get; set; }
        public string AssignedToUserId { get; set; }
        [Required]
        public int ProjectId { get; set; }
        [Required]
        public int TicketTypeId { get; set; }
        [Required]
        public int TicketPriorityId { get; set; }
        public int TicketStatusId { get; set; }


        public virtual ApplicationUser AuthorUser { get; set; }
        public virtual ApplicationUser AssignedToUser { get; set; }
        //public virtual ApplicationUser TicketUsers { get; set; }

        public virtual Project Project { get; set; }
        public virtual TicketType TicketType { get; set; }
        public virtual TicketPriority TicketPriority { get; set; }
        public virtual TicketStatus TicketStatus { get; set; }
        
        public virtual ICollection<TicketComment> TicketComments { get; set; }
        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
        public virtual ICollection<TicketHistory> TicketHistories { get; set; }
    }
}