using CV.GLL.CRM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkyTfs;

namespace CrmPlayground
{
    public class SwatTicketSync
    {
        public async System.Threading.Tasks.Task SyncDirty()
        {
            var tfsTeam = new TfsTeam();
            var ticketsToUpdate = CVSwatTicket.GetDirtySwatTickets();
            foreach (var ticket in ticketsToUpdate)
            {
                var swatWorkItem = new TfsWorkItem();

                int tfsId;
                if (ticket.Attributes.ContainsKey("cv_tfsitem") &&
                    ticket.Attributes["cv_tfsitem"] != null &&
                    int.TryParse(ticket.Attributes["cv_tfsitem"].ToString(), out tfsId))
                {
                    swatWorkItem = await tfsTeam.GetTfsWorkItemByItemId(tfsId);
                }

                if (swatWorkItem?.Id != null)
                    await UpdateWorkItemFromSwatTicket(swatWorkItem, ticket);
                else
                    swatWorkItem = await CreateNewWorkItemFromSwatTicket("Bug", ticket);

                if (swatWorkItem?.Id != null)
                    CVSwatTicket.ClearDirtyFlag(ticket.Id, swatWorkItem.Id.Value);
                else
                    Console.WriteLine("WHY DID WE FAIL TO GET AN ID!?!?!?"); //TODO: meh?
            }
        }

        public async System.Threading.Tasks.Task SyncSingle(Guid ticketGuid)
        {
            var tfsTeam = new TfsTeam();
            var ticket = CVSwatTicket.GetSwatTicketById(ticketGuid);
            var swatWorkItem = new TfsWorkItem();

            //TODO: update to cv_tfsitem instead of cv_tfsitem
            int tfsId;
            if (ticket.Attributes.ContainsKey("cv_tfsitem") &&
                ticket.Attributes["cv_tfsitem"] != null &&
                int.TryParse(ticket.Attributes["cv_tfsitem"].ToString(), out tfsId))
            {
                swatWorkItem = await tfsTeam.GetTfsWorkItemByItemId(tfsId);
            }

            if (swatWorkItem?.Id != null)
                await UpdateWorkItemFromSwatTicket(swatWorkItem, ticket);
            else
                swatWorkItem = await CreateNewWorkItemFromSwatTicket("Bug", ticket);

            if (swatWorkItem?.Id != null)
                CVSwatTicket.ClearDirtyFlag(ticket.Id, swatWorkItem.Id.Value);
            else
                Console.WriteLine("WHY DID WE FAIL TO GET AN ID!?!?!?"); //TODO: meh?
        }

        public async static Task<TfsWorkItem> CreateNewWorkItemFromSwatTicket(string workItemType, cv_swatticket ticket)
        {
            var workItem = new TfsWorkItem()
            {
                AreaId = 202,
                ItemType = workItemType,
                Title = ticket.Subject,
                Description = CreateSwatTicketDescription(ticket),
                CreatedDate = ticket.CreatedOn.HasValue ? ticket.CreatedOn.Value : DateTime.Now,
                AssignedTo = ticket.OwnerId?.Name,
                ChangedBy = ticket.ModifiedBy?.Name,
                ChangedDate = ticket.ModifiedOn.HasValue ? ticket.ModifiedOn.Value : DateTime.Now,
                Priority = ticket.PriorityCode.Value,
                RootCause = CreateSwatTicketFinalResolution(ticket),
                DueDate = ticket.cv_InitialResponseNeededBy,
                Annotations = CreateSwatTicketAnnotations(ticket),
                Hyperlinks = CreateSwatTicketHyperlinks(ticket),
            };

            await workItem.Save();

            return workItem;
        }

        private async static System.Threading.Tasks.Task UpdateWorkItemFromSwatTicket(TfsWorkItem workItem, cv_swatticket ticket)
        {
            workItem.AssignedTo = ticket.OwnerId?.Name;
            workItem.ChangedBy = ticket.ModifiedBy?.Name;
            workItem.ChangedDate = ticket.ModifiedOn.HasValue ? ticket.ModifiedOn.Value : DateTime.Now;
            workItem.Priority = ticket.PriorityCode.Value;
            workItem.RootCause = CreateSwatTicketFinalResolution(ticket);
            workItem.DueDate = ticket.cv_InitialResponseNeededBy;
            workItem.Annotations = CreateSwatTicketAnnotations(ticket);
            workItem.Hyperlinks = CreateSwatTicketHyperlinks(ticket);

            await workItem.Save();
        }

        private static List<TfsHyperlink> CreateSwatTicketHyperlinks(cv_swatticket ticket)
        {
            var hyperlinks = new List<TfsHyperlink>();

            var ticketLink = $@"https://cloudvisors.crm.dynamics.com/main.aspx?etc=10069&extraqs=formid%3d05e33d47-0149-473d-b04d-e2f7b45ed1c0&id=%7b{ticket.Id}%7d&pagetype=entityrecord";
            hyperlinks.Add(new TfsHyperlink("SWAT Ticket", ticketLink));

            if (ticket.RegardingObjectId != null)
            {
                var link = $"https://cloudvisors.crm.dynamics.com/main.aspx?etc=1088&extraqs=formid%3d0e9fc35b-20e3-46c2-90af-27ba94b22c32&id=%7b{ticket.RegardingObjectId.Id}%7d&pagetype=entityrecord";
                hyperlinks.Add(new TfsHyperlink(ticket.RegardingObjectId.Name, link));
            }

            if (!string.IsNullOrEmpty(ticket.cv_PullRequestURI))
                hyperlinks.Add(new TfsHyperlink("Pull Request", ticket.cv_PullRequestURI));

            return hyperlinks;
        }

        private static string CreateSwatTicketFinalResolution(cv_swatticket ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket.cv_FinalResolution))
                return null;

            if (ticket.cv_FinalResolution.Length < 200)
            {
                return ticket.cv_FinalResolution;
            }
            else
            {
                var maxLengthAllowed = 200;
                var toBeContinuedMessage = "...[continued in SWAT ticket]";
                return $"{ticket.cv_FinalResolution.Substring(0, maxLengthAllowed - toBeContinuedMessage.Length)}{toBeContinuedMessage}";
            }
        }

        private static string CreateSwatTicketDescription(cv_swatticket ticket)
        {
            var createdBy = ticket.CreatedBy.Name;
            var ticketDescription = (ticket.Attributes["cv_problemdescription"] as string).Replace("\n", "<br />");
            return $"<strong>Created By: {createdBy}</strong><br /><br />Description: {ticketDescription}";
        }

        private static List<string> CreateSwatTicketAnnotations(cv_swatticket ticket)
        {
            var ticketAnnotations = CVAnnotation.GetAnnotations(ticket.Id);
            if (ticketAnnotations == null || ticketAnnotations.Count == 0)
                return null;

            var annotations = ticketAnnotations.Select(x => x.Attributes["notetext"].ToString());
            return annotations.ToList();
        }
    }
}
