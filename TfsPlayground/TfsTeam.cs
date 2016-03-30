using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TfsPlayground.Properties;
using VisualStudioOnline.Api.Rest;
using VisualStudioOnline.Api.Rest.V1.Client;
using VisualStudioOnline.Api.Rest.V1.Model;

namespace TfsPlayground
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
                    var client = new VsoClient(Settings.Default.TfsSwatUsername, tfsCredentials); //TODO: account name?
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
                    _projectRestClient = new ProjectRestClient(Settings.Default.TfsSwatUsername, tfsCredentials);

                return _projectRestClient;
            }
        }

        internal async Task<IEnumerable<string>> GetAllTeamProjectMembers(string projectName)
        {
            var teams = await ProjectRestClient.GetProjectTeams("SkyKick 1");
            if (teams == null || teams.Count == 0)
                throw new Exception($"No Project Teams could be found for a project named: {projectName}");

            var allMembers = new JsonCollection<UserIdentity>();
            for (var t = 0; t < teams.Count; t++)
            {
                var teamMembers = await ProjectRestClient.GetTeamMembers("SkyKick 1", teams[t].Id.ToString());
                allMembers.Items.AddRange(teamMembers.Items);
            }

            return allMembers != null ? allMembers.Items.Select(x => x.DisplayName) : null;
        }

        public async Task<TfsWorkItem> GetTfsWorkItemByItemId(int tfsId)
        {
            var workItem = await ClientRestClient.GetWorkItem(tfsId, RevisionExpandOptions.all);
            //TODO
            if (workItem != null)// && workItem.Fields["Project Name"] == Settings.Default.TfsSkyKickTeamProjectName)
                return new TfsWorkItem() { WorkItem = workItem };

            return null;
        }

        internal async Task<WorkItem> UpdateWorkItem(WorkItem workItem)
        {
            return await ClientRestClient.UpdateWorkItem(workItem);
        }
    }
}
