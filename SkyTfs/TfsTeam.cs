using SkyTfs.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VisualStudioOnline.Api.Rest;
using VisualStudioOnline.Api.Rest.V1.Client;
using VisualStudioOnline.Api.Rest.V1.Model;

namespace SkyTfs
{
    public class TfsTeam
    {
        private static NetworkCredential tfsCredentials = new NetworkCredential(Settings.Default.TfsSwatUsername, Settings.Default.TfsSwatPasskey);

        private IVsoWit _clientRestClient;
        private IVsoWit ClientRestClient
        {
            get
            {
                if (_clientRestClient == null)
                {
                    var client = new VsoClient(Settings.Default.TfsSkyKickAccountName, tfsCredentials);
                    _clientRestClient = client.GetService<IVsoWit>();
                }

                return _clientRestClient;
            }
        }

        private IVsoProject _projectRestClient;
        private IVsoProject ProjectRestClient
        {
            get
            {
                if (_projectRestClient == null)
                    _projectRestClient = new ProjectRestClient(Settings.Default.TfsSkyKickAccountName, tfsCredentials);

                return _projectRestClient;
            }
        }

        internal async Task<IEnumerable<string>> GetAllTeamProjectMembers(string projectName)
        {
            var teams = await ProjectRestClient.GetProjectTeams("SkyKick 1");
            if (teams == null || teams.Count == 0)
                throw new Exception($"No Project Teams could be found for a project named: {projectName}");

            var userIdentities = new List<UserIdentity>();
            for (var t = 0; t < teams.Count; t++)
            {
                var teamMembers = await ProjectRestClient.GetTeamMembers("SkyKick 1", teams[t].Id.ToString());
                if (teamMembers?.Items != null)
                    userIdentities.AddRange(teamMembers.Items);
            }

            return userIdentities != null ? userIdentities.Select(x => x.DisplayName).Distinct() : null;
        }

        public async Task<TfsWorkItem> GetTfsWorkItemByItemId(int tfsId)
        {
            var workItem = await ClientRestClient.GetWorkItem(tfsId, RevisionExpandOptions.all);
            //TODO
            if (workItem != null)// && workItem.Fields["Project Name"] == Settings.Default.TfsSkyKickTeamProjectName)
                return new TfsWorkItem() { WorkItem = workItem };

            return null;
        }

        internal async Task<JsonCollection<HistoryComment>> GetWorkItemHistoryComments(int itemId)
        {
            return await ClientRestClient.GetWorkItemHistory(itemId);

        }

        internal async Task<WorkItem> UpdateWorkItem(WorkItem workItem)
        {
            return await ClientRestClient.UpdateWorkItem(workItem);
        }

        internal async Task<WorkItem> SaveWorkItem(string projectName, string workItemType, WorkItem workItem)
        {
            return await ClientRestClient.CreateWorkItem(projectName, workItemType, workItem);
        }

        internal async Task<int> GetCurrentSwatIterationId(string projectName)
        {
            var classificationNodes = await ClientRestClient.GetClassificationNodes(projectName, 2);
            if (classificationNodes?.Items == null || classificationNodes.Items.Count == 0)
                return 0;

            var interationStructureType = "iteration";
            var iterations = classificationNodes.Items.FirstOrDefault(x => x.StructureType.ToString() == interationStructureType && x.HasChildren);
            if (iterations?.Children == null || iterations.Children.Count == 0)
                return 0;

            var iteration = iterations.Children
                                      .FirstOrDefault(x => !string.IsNullOrEmpty(x.Name) &&
                                                           x.Name.ToLower().StartsWith("swat") && //yucky hard coded magic string... sorry
                                                           x.Attributes != null &&
                                                           DateTime.Now > x.Attributes.StartDate &&
                                                           DateTime.Now < x.Attributes.FinishDate);

            if (iteration != null)
                return iteration.Id;

            return 0;
        }
    }
}
