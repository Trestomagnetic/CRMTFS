using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisualStudioOnline.Api.Rest.V1.Model;

namespace TfsPlayground
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
                UpdateFieldIfDirty("System.AssignedTo", this.AssignedTo);
            
            if (Priority.HasValue)
                UpdateFieldIfDirty("Microsoft.VSTS.Common.Priority", Priority.HasValue ? Priority.Value.ToString() : null);

            UpdateFieldIfDirty("Microsoft.VSTS.CMMI.RootCause", RootCause);

            UpdateFieldIfDirty("Microsoft.VSTS.Scheduling.DueDate", DueDate);

            var history = CreateWorkItemHistoryFromNewAnnotations(Annotations);
            if (!string.IsNullOrEmpty(history))
                UpdateFieldIfDirty("System.History", history);

            var relationalHyperlinks = CreateRelationHyperlinksFromNewTfsHyperlinks(Hyperlinks);
            foreach (var rh in relationalHyperlinks)
                WorkItem.Relations.Add(rh);

            //TODO: halp
            //if (WorkItem.IsDirty)
            //{
                if (!string.IsNullOrEmpty(this.ChangedBy) && validUsers.Contains(this.ChangedBy))
                    WorkItem.Fields["System.ChangedBy"] = this.ChangedBy;

                WorkItem.Fields["System.ChangedDate"] = this.ChangedDate;

                WorkItem = await tfsTeam.UpdateWorkItem(WorkItem);
            //}

            return true;
        }

        private void UpdateFieldIfDirty(string fieldName, string newValue)
        {
            var currentValue = WorkItem.Fields[fieldName]?.ToString();
            if (currentValue != newValue)
                WorkItem.Fields[fieldName] = newValue;
        }

        private void UpdateFieldIfDirty(string fieldName, DateTime? newValue)
        {
            DateTime currentValue;
            //if (DateTime.TryParse(WorkItem[fieldName]?.ToString(), out currentValue) &&
            //    currentValue != GetLocalizedDateTime(newValue))
            //    WorkItem[fieldName] = newValue;
        }

        private static DateTime? GetLocalizedDateTime(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone.CurrentTimeZone.StandardName);
            return TimeZoneInfo.ConvertTimeFromUtc(value.Value, localTimeZone);
        }

        private async Task<bool> CreateWorkItem()
        {
            //var team = new Team();
            //var teamProject = team.GetProject(Properties.Settings.Default.TfsSkyKickTeamProjectName);
            //var validUsers = team.GetProjectUsers(Properties.Settings.Default.TfsSkyKickTeamProjectName, Properties.Settings.Default.TfsUri);
            //var workItemType = GetWorkItemType(teamProject, ItemType);

            //if (string.IsNullOrEmpty(ItemType) ||
            //    workItemType == null)
            //    throw new ArgumentException("Required field is missing or invalid.", "ItemType");

            //WorkItem = new WorkItem(workItemType)
            //{
            //    Title = this.Title,
            //    Description = this.Description,
            //    AreaId = AreaId,
            //};

            //WorkItem["System.CreatedBy"] = Properties.Settings.Default.TfsSwatUsername;

            //WorkItem["System.CreatedDate"] = this.CreatedDate;

            //if (!string.IsNullOrEmpty(this.AssignedTo) && validUsers.Contains(this.AssignedTo))
            //    WorkItem["System.AssignedTo"] = this.AssignedTo;

            //if (!string.IsNullOrEmpty(this.ChangedBy) && validUsers.Contains(this.ChangedBy))
            //    WorkItem["System.ChangedBy"] = this.ChangedBy;

            //WorkItem["System.ChangedDate"] = this.ChangedDate;

            //WorkItem.IterationPath = team.GetCurrentIterationPath(Properties.Settings.Default.TfsSkyKickTeamProjectName);

            //if (this.Priority.HasValue)
            //    WorkItem["Microsoft.VSTS.Common.Priority"] = this.Priority.Value;

            //if (!string.IsNullOrEmpty(this.RootCause))
            //    WorkItem["Microsoft.VSTS.CMMI.RootCause"] = this.RootCause;

            //if (this.DueDate.HasValue)
            //    WorkItem["Microsoft.VSTS.Scheduling.DueDate"] = this.DueDate.Value;

            //var history = CreateWorkItemHistoryFromNewAnnotations(this.Annotations);
            //if (!string.IsNullOrEmpty(history))
            //    WorkItem.History = history;

            //foreach (var h in this.Hyperlinks)
            //    WorkItem.Links.Add(new Hyperlink(h.Location) { Comment = h.Comment });

            //WorkItem.Save();

            return true;
        }

        private WorkItemType GetWorkItemType(/*Project teamProject,*/ string itemType)
        {
            //foreach (WorkItemType wit in teamProject.WorkItemTypes)
            //{
            //    if (wit.Name == itemType)
            //        return wit;
            //}

            return null;
        }

        private string CreateWorkItemHistoryFromNewAnnotations(IEnumerable<string> annotations)
        {
            if (annotations == null)
                return null;

            var discussionHistory = new List<string>();// GetDiscussionHistoryFromRevisionCollection(WorkItem.Revisions);
            if (discussionHistory == null)
                return null;

            var history = string.Empty;
            var htmlLineBreak = "<br>";
            foreach (var ann in annotations)
            {
                var alreadyExists = discussionHistory.Where(x => x == ann ||
                                                                 x.Contains(ann + htmlLineBreak) ||
                                                                 x.Contains(htmlLineBreak + ann)).Any();

                if (alreadyExists) continue;

                
                //var _htmlTagRegex = new Regex("<.*?>", RegexOptions.Compiled);
                //var discussion = _htmlTagRegex.Replace(ann, string.Empty);
                history += string.IsNullOrEmpty(history) ? ann : $"{htmlLineBreak}{ann}";
            }

            return history;
        }

        //private IEnumerable<string> GetDiscussionHistoryFromRevisionCollection(RevisionCollection revisions)
        //{
        //    if (revisions == null)
        //        return null;

        //    var discussionHistory = new List<string>();
        //    foreach (Revision rev in revisions)
        //    {
        //        if (!string.IsNullOrEmpty(rev.Fields["System.History"]?.Value?.ToString()))
        //            discussionHistory.Add(rev.Fields["System.History"].Value.ToString());
        //    }

        //    return discussionHistory;
        //}

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
