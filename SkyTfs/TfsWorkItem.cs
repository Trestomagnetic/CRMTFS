using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisualStudioOnline.Api.Rest.V1.Model;

namespace SkyTfs
{
    public class TfsWorkItem
    {
        internal WorkItem WorkItem { get; set; }
        public int? Id
        {
            get
            {
                if (WorkItem == null || WorkItem.Id == 0) //TODO make sure 0 is the default value
                    return null;
                else
                    return WorkItem.Id;
            }
        }

        public List<string> Annotations { get; set; }
        public int AreaId { get; set; }
        public string AssignedTo { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public string RootCause { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public List<TfsHyperlink> Hyperlinks { get; set; }
        public string ItemType { get; set; }
        public int? Priority { get; set; }
        public string Title { get; set; }

        public async Task<bool> Save()
        {
            if (WorkItem != null && WorkItem.Id > 0)
                return await UpdateWorkItem();
            else
                return await CreateWorkItem();
        }

        private async Task<bool> UpdateWorkItem()
        {
            var tfsTeam = new TfsTeam();
            var validUsers = await tfsTeam.GetAllTeamProjectMembers(Properties.Settings.Default.TfsSkyKickTeamProjectName);

            if (!string.IsNullOrEmpty(this.AssignedTo) && validUsers.Contains(this.AssignedTo))
                UpdateFieldIfDirty("System.AssignedTo", this.AssignedTo, true);

            if (Priority.HasValue)
                UpdateFieldIfDirty("Microsoft.VSTS.Common.Priority", Priority.HasValue ? Priority.Value.ToString() : null);

            UpdateFieldIfDirty("Microsoft.VSTS.CMMI.RootCause", RootCause);

            UpdateFieldIfDirty("Microsoft.VSTS.Scheduling.DueDate", DueDate);

            var history = await CreateWorkItemDiscussionHistoryFromNewAnnotations(tfsTeam, Annotations);
            if (!string.IsNullOrEmpty(history))
                UpdateFieldIfDirty("System.History", history);

            var relationalHyperlinks = CreateRelationHyperlinksFromNewTfsHyperlinks(Hyperlinks);
            foreach (var rh in relationalHyperlinks)
                WorkItem.Relations.Add(rh);

            if (WorkItem.IsDirty)
            {
                if (!string.IsNullOrEmpty(this.ChangedBy) && validUsers.Contains(this.ChangedBy))
                    WorkItem.Fields["System.ChangedBy"] = this.ChangedBy;

                WorkItem.Fields["System.ChangedDate"] = this.ChangedDate;

                WorkItem = await tfsTeam.UpdateWorkItem(WorkItem);
            }

            return true;
        }

        private void UpdateFieldIfDirty(string fieldName, string newValue, bool removeHtmlTags = false)
        {
            var hasKey = WorkItem.Fields.Keys?.Contains(fieldName);
            var currentValue = hasKey.HasValue && hasKey.Value ? WorkItem.Fields[fieldName].ToString() : null;

            if (removeHtmlTags)
            {
                var _htmlTagRegex = new Regex("<.*?>", RegexOptions.Compiled);
                currentValue = _htmlTagRegex.Replace(currentValue, string.Empty).Trim();
            }

            if (currentValue != newValue)
                WorkItem.Fields[fieldName] = newValue;
        }

        private void UpdateFieldIfDirty(string fieldName, DateTime? newValue)
        {
            DateTime currentValue = new DateTime();
            var hasKey = WorkItem.Fields.Keys?.Contains(fieldName);
            if (hasKey.HasValue && hasKey.Value)
                DateTime.TryParse(WorkItem.Fields[fieldName].ToString(), out currentValue);

            //use GetLocalizedDateTime if there's an issue with crm vs tfs time
            if (currentValue != newValue)
                WorkItem.Fields[fieldName] = newValue;
        }

        /// <summary>
        /// Localizing datetime because the incoming dates (CRM) are adjusted to server time (UTC), and outgoing dates (TFS) are adjusting for server time.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTime? GetLocalizedDateTime(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone.CurrentTimeZone.StandardName);
            return TimeZoneInfo.ConvertTimeFromUtc(value.Value, localTimeZone);
        }

        private async Task<bool> CreateWorkItem()
        {
            var tfsTeam = new TfsTeam();
            var validUsers = await tfsTeam.GetAllTeamProjectMembers(Properties.Settings.Default.TfsSkyKickTeamProjectName);

            if (string.IsNullOrEmpty(ItemType))
                throw new ArgumentException("Required field is missing or invalid.", "ItemType");

            WorkItem = new WorkItem();
            WorkItem.Fields["System.Title"] = this.Title;
            WorkItem.Fields["Microsoft.VSTS.TCM.ReproSteps"] = this.Description;
            WorkItem.Fields["System.AreaId"] = this.AreaId;
            WorkItem.Fields["System.CreatedBy"] = Properties.Settings.Default.TfsSwatUsername;
            WorkItem.Fields["System.CreatedDate"] = this.CreatedDate;
            WorkItem.Fields["System.Tags"] = "SWAT";

            if (!string.IsNullOrEmpty(this.AssignedTo) && validUsers.Contains(this.AssignedTo))
                WorkItem.Fields["System.AssignedTo"] = this.AssignedTo;

            if (!string.IsNullOrEmpty(this.ChangedBy) && validUsers.Contains(this.ChangedBy))
                WorkItem.Fields["System.ChangedBy"] = this.ChangedBy;

            WorkItem.Fields["System.ChangedDate"] = this.ChangedDate;

            var iteration = await tfsTeam.GetCurrentSwatIterationId(Properties.Settings.Default.TfsSkyKickTeamProjectName);
            if (iteration > 0)
                WorkItem.Fields["System.IterationId"] = iteration;

            if (this.Priority.HasValue)
                WorkItem.Fields["Microsoft.VSTS.Common.Priority"] = this.Priority.Value;

            if (!string.IsNullOrEmpty(this.RootCause))
                WorkItem.Fields["Microsoft.VSTS.CMMI.RootCause"] = this.RootCause;

            if (this.DueDate.HasValue)
                WorkItem.Fields["Microsoft.VSTS.Scheduling.DueDate"] = this.DueDate.Value;

            var history = await CreateWorkItemDiscussionHistoryFromNewAnnotations(tfsTeam, Annotations);
            if (!string.IsNullOrEmpty(history))
                WorkItem.Fields["System.History"] = history;

            var relationalHyperlinks = CreateRelationHyperlinksFromNewTfsHyperlinks(Hyperlinks);
            foreach (var rh in relationalHyperlinks)
                WorkItem.Relations.Add(rh);

            WorkItem = await tfsTeam.SaveWorkItem(Properties.Settings.Default.TfsSkyKickTeamProjectName, ItemType, WorkItem);

            return true;
        }

        private async Task<string> CreateWorkItemDiscussionHistoryFromNewAnnotations(TfsTeam tfsTeam, IEnumerable<string> annotations)
        {
            if (annotations == null)
                return null;

            var discussionHistory = await tfsTeam.GetWorkItemHistoryComments(Id.Value);
            if (discussionHistory?.Items == null)
                return null;

            var history = string.Empty;
            var htmlLineBreak = "<br>";
            foreach (var ann in annotations)
            {
                var alreadyExists = discussionHistory.Items.Where(x => x.Value == ann ||
                                                                       x.Value.Contains(ann + htmlLineBreak) ||
                                                                       x.Value.Contains(htmlLineBreak + ann)).Any();

                if (alreadyExists) continue;

                history += string.IsNullOrEmpty(history) ? ann : $"{htmlLineBreak}{ann}";
            }

            return history;
        }

        private IEnumerable<WorkItemRelation> CreateRelationHyperlinksFromNewTfsHyperlinks(IEnumerable<TfsHyperlink> tfsHyperlinks)
        {
            var hyperlinkRelType = "Hyperlink";
            var incomingHyperlinks = tfsHyperlinks?.Select(x => new WorkItemRelation()
            {
                Attributes = new RelationAttributes() { Comment = x.Comment },
                Rel = hyperlinkRelType,
                Url = x.Location
            });

            if (tfsHyperlinks == null || tfsHyperlinks.Count() == 0)
                return new List<WorkItemRelation>();

            var existingHyperlinks = WorkItem.Relations?.Where(x => x.Rel == hyperlinkRelType);
            if (existingHyperlinks == null || existingHyperlinks.Count() == 0)
                return incomingHyperlinks;

            return incomingHyperlinks.Where(i => !existingHyperlinks.Any(e => e.Url == i.Url && e.Attributes.Comment == i.Attributes.Comment));
        }
    }
}
