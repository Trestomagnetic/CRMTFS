using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using VisualStudioOnline.Api.Rest.V1.Model;

namespace VisualStudioOnline.Api.Rest.V1.Client
{
    public interface IVsoGit
    {
        Task<JsonCollection<Repository>> GetRepositories();

        Task<Repository> GetRepository(string id);

        Task<Repository> CreateRepository(string name, string projectId);

        Task<Repository> RenameRepository(string id, string newName);

        Task<string> DeleteRepository(string id);

        Task<JsonCollection<GitItem>> GetItemMetadata(string repoId, string scopePath = "/", bool includeContentMetadata = false, bool latestProcessedChange = false);

        Task<byte[]> GetItem(string repoId, string path);

        Task<byte[]> GetFolder(string repoId, string path);

        Task<JsonCollection<GitReference>> GetRefs(string repoId, string filter = null);

        Task<GitDiff> GetDiffs(string repoId, GitVersionType? baseVersionType = null, string baseVersion = null,
            GitVersionType? targetVersionType = null, string targetVersion = null, int? top = null, int? skip = null);

        Task<JsonCollection<GitBranchInfo>> GetBranchStatistics(string repoId);

        Task<GitBranchInfo> GetBranchStatistics(string repoId, string branchName, GitVersionType? type = null, string baseVersion = null);

        Task<string> GetCommits(string repoId, string itemPath = "\\", CommitSearchFilter? filter = null, int? top = null, int? skip = null);
    }

    public struct CommitSearchFilter
    {
        public string Commiter;

        public string Author;

        public DateTime? From;

        public DateTime? To;
    }

    public enum GitVersionType 
    { 
        Branch, 
        Tag, 
        Commit 
    }

    public class GitRestClient : RestClientVersion1, IVsoGit
    {
        protected override string SubSystemName
        {
            get { return "git"; }
        }

        public GitRestClient(string url, NetworkCredential userCredential)
            : base(url, new BasicAuthenticationFilter(userCredential))
        {
        }

        /// <summary>
        /// Get a list of repositories
        /// </summary>
        /// <returns></returns>
        public async Task<JsonCollection<Repository>> GetRepositories()
        {
            string response = await GetResponse("repositories");
            return JsonConvert.DeserializeObject<JsonCollection<Repository>>(response);
        }

        /// <summary>
        /// Get a repository
        /// </summary>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<Repository> GetRepository(string repoId)
        {
            string response = await GetResponse(string.Format("repositories/{0}", repoId));
            return JsonConvert.DeserializeObject<Repository>(response);
        }

        /// <summary>
        /// Create a repository
        /// </summary>
        /// <param name="name"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task<Repository> CreateRepository(string name, string projectId)
        {
            string response = await PostResponse("repositories", new { name = name, project = new { id = projectId } });
            return JsonConvert.DeserializeObject<Repository>(response);
        }

        /// <summary>
        /// Rename a repository
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public async Task<Repository> RenameRepository(string repoId, string newName)
        {
            string response = await PatchResponse(string.Format("repositories/{0}", repoId), new { name = newName }, null, MediaType.JSON_MEDIA_TYPE);
            return JsonConvert.DeserializeObject<Repository>(response);
        }

        /// <summary>
        /// Delete a repository
        /// </summary>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<string> DeleteRepository(string repoId)
        {
            string response = await DeleteResponse(string.Format("repositories/{0}", repoId));
            return response;
        }

        /// <summary>
        /// Get file metadata
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="scopePath"></param>
        /// <param name="includeContentMetadata"></param>
        /// <param name="latestProcessedChange"></param>
        /// <returns></returns>
        public async Task<JsonCollection<GitItem>> GetItemMetadata(string repoId, string scopePath = "/", bool includeContentMetadata = false, bool latestProcessedChange = false)
        {
            string response = await GetResponse(string.Format("repositories/{0}/items", repoId),  
                new Dictionary<string, object>() { 
                    { "scopePath", scopePath },
                    { "includeContentMetadata", includeContentMetadata },
                    { "latestProcessedChange", latestProcessedChange }
                });
            return JsonConvert.DeserializeObject<JsonCollection<GitItem>>(response);
        }

        /// <summary>
        /// Get file content
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<byte[]> GetItem(string repoId, string path)
        {
            return await GetByteResponse(string.Format("repositories/{0}/items", repoId),
                new Dictionary<string, object>() { 
                    { "scopePath", path },
                }, null, MediaType.OCTET_STREAM);
        }

        /// <summary>
        /// Get folder content as zip
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<byte[]> GetFolder(string repoId, string path)
        {
            return await GetByteResponse(string.Format("repositories/{0}/items", repoId),
                new Dictionary<string, object>() { 
                    { "scopePath", path },
                }, null, MediaType.ZIP);
        }

        /// <summary>
        /// Get a list of references
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<JsonCollection<GitReference>> GetRefs(string repoId, string filter = null)
        {
            string response = await GetResponse(string.IsNullOrEmpty(filter) ? 
                string.Format("repositories/{0}/refs", repoId) : string.Format("repositories/{0}/refs/{1}", repoId, filter));
            return JsonConvert.DeserializeObject<JsonCollection<GitReference>>(response);
        }

        /// <summary>
        /// Get branch statistics
        /// </summary>
        /// <param name="repoId"></param>
        /// <returns></returns>
        public async Task<JsonCollection<GitBranchInfo>> GetBranchStatistics(string repoId)
        {
            string response = await GetResponse(string.Format("repositories/{0}/stats/branches", repoId));
            return JsonConvert.DeserializeObject<JsonCollection<GitBranchInfo>>(response);
        }

        /// <summary>
        /// A version of a branch
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="branchName"></param>
        /// <param name="type"></param>
        /// <param name="baseVersion"></param>
        /// <returns></returns>
        public async Task<GitBranchInfo> GetBranchStatistics(string repoId, string branchName, GitVersionType? type = null, string baseVersion = null)
        {
            string response = await GetResponse(string.Format("repositories/{0}/stats/branches/{1}", repoId, branchName),
                 new Dictionary<string, object>() { 
                    { "baseVersionType", type != null ? type.Value.ToString() :  null},
                    { "baseVersion", baseVersion}});
            return JsonConvert.DeserializeObject<GitBranchInfo>(response);
        }

        /// <summary>
        /// Get a list of differences
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="baseVersionType"></param>
        /// <param name="baseVersion"></param>
        /// <param name="targetVersionType"></param>
        /// <param name="targetVersion"></param>
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<GitDiff> GetDiffs(string repoId, 
            GitVersionType? baseVersionType = null, string baseVersion = null,
            GitVersionType? targetVersionType = null, string targetVersion = null,
            int? top = null, int? skip = null)
        {
            string response = await GetResponse(string.Format("repositories/{0}/diffs/commits", repoId),
                 new Dictionary<string, object>() { 
                    { "baseVersionType", baseVersionType != null ? baseVersionType.Value.ToString() :  null},
                    { "baseVersion", baseVersion},
                    { "targetVersionType", targetVersionType != null ? targetVersionType.Value.ToString() :  null},
                    { "targetVersion", targetVersion},
                    { "$top", top }, { "$skip", skip }
                 });
            return JsonConvert.DeserializeObject<GitDiff>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="objectId"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public async Task<string> GetTreeMetadata(string repoId, string objectId, bool? recursive = null)
        {
            string response = await GetResponse(string.Format("repositories/{0}/trees/{1}", repoId, objectId));
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="objectId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> DownloadTree(string repoId, string objectId, string fileName = null)
        {
            string response = await GetResponse(string.Format("repositories/{0}/trees/{1}", repoId, objectId), 
                new Dictionary<string, object>() { { "fileName", fileName}, { "$format", "zip"} });
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repoId"></param>
        /// <param name="itemPath"></param>
        /// <param name="filter"></param>
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<string> GetCommits(string repoId, string itemPath = "\\", CommitSearchFilter? filter = null, int? top = null, int? skip = null)
        {
            var arguments = new Dictionary<string, object>() { { "itempath", itemPath }, { "$top", top }, { "$skip", skip } };
            if(filter != null)
            {
                arguments.Add("committer", filter.Value.Commiter);
                arguments.Add("author", filter.Value.Author);
                arguments.Add("fromDate", filter.Value.From);
                arguments.Add("toDate", filter.Value.To);
            }

            string response = await GetResponse(string.Format("repositories/{0}/commits", repoId), arguments);
            return response;
        }
    }
}
