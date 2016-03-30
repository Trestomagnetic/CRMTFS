using System;
using System.Linq;
using System.Net;
using VisualStudioOnline.Api.Rest.V1.Client;
using VisualStudioOnline.Api.Rest.V1.Model;
using TfsPlayground.Properties;
using System.Threading;
using System.Text.RegularExpressions;
using VisualStudioOnline.Api.Rest;
//using Microsoft.TeamFoundation.Client;
//using Microsoft.TeamFoundation.Framework.Common;
//using Microsoft.TeamFoundation.Framework.Client;

namespace TfsPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            var prog = new RealProgram();
            prog.Start();
        }
    }

    public class RealProgram
    {
        bool QueryInProgress;

        public void Start()
        {
            PrintOptions();
            Console.WriteLine("Please enter your selection...");

            while (true)
            {
                var input = Console.ReadLine();
                switch (input.IndexOf(" ") > 0 ? input.Substring(0, input.IndexOf(" ")).ToUpper() : input.ToUpper())
                {
                    case "":
                        Trestintest();
                        break;
                    case "LQ":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_PrintAllQueries();
                        break;
                    case "EXEC":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_ExecuteQuery(input);
                        break;
                    case "GET":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_GetWorkItem(input);
                        break;
                    case "ADD":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_AddWorkItem(input);
                        break;
                    case "HELP":
                        PrintOptions();
                        break;
                    case "EXIT":
                        return;
                    default:
                        Console.WriteLine("For a list of options type HELP");
                        break;
                }
            }
        }

        private async void Trestintest()
        {
            var classificationNodes = ClientRestClient.GetClassificationNodes("SkyKick 1", 2);
            var iterations = classificationNodes.Result.Items.FirstOrDefault(x => x.StructureType.ToString() == "iteration" && x.HasChildren);
            var devopsIterations = iterations.Children.Select(x => x.Attributes != null &&
                                                                   DateTime.Now > x.Attributes.StartDate &&
                                                                   DateTime.Now < x.Attributes.FinishDate);

            foreach (var item in iterations.Children)
            {
                bool testtest;
                if (item.Attributes != null &&
                    DateTime.Now > item.Attributes.StartDate &&
                    DateTime.Now < item.Attributes.FinishDate)
                    testtest = true;
            }
        }

        private async void VsoApi_GetWorkItem(string input)
        {
            var queryId = input.Remove(0, 4).Trim();

            int tfsId = 0;
            if (string.IsNullOrEmpty(queryId) ||
                !int.TryParse(queryId, out tfsId))
            {
                QueryInProgress = false;
                Console.WriteLine("No item returned for that ID");
                Console.WriteLine("   --------------------");
            }

            var workItem = await ClientRestClient.GetWorkItem(tfsId, RevisionExpandOptions.all);
            if (workItem == null)
            {
                QueryInProgress = false;
                Console.WriteLine("No item returned for that ID");
                Console.WriteLine("   --------------------");
            }
            var history = await ClientRestClient.GetWorkItemHistory(tfsId);

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            foreach (var f in workItem.Fields)
            {
                Console.WriteLine($"KEY: {f.Key}");
                Console.WriteLine($"    VALUE: {f.Value}");
            }

            if (workItem.Relations != null && workItem.Relations.Count > 0)
            {
                Console.WriteLine($"KEY: ??????");
                foreach (var f in workItem.Relations)
                {
                    Console.WriteLine($"    VALUE: {f.Url}");
                    //Console.WriteLine($"    REV BY: {f.RevisedBy.Name}");
                }
            }

            if (history != null && history.Count > 0)
            {
                Console.WriteLine($"KEY: System.History");
                foreach (var f in history.Items)
                {
                    Console.WriteLine($"    VALUE: {f.Value}");
                    Console.WriteLine($"    REV BY: {f.RevisedBy.Name}");
                }
            }

            Console.WriteLine("   --------------------");

            WorkItemUpdate(workItem);
        }

        private async void WorkItemUpdate(WorkItem item)
        {
            //item.Fields["System.History"] = "SomeOtherUpdate";
            var rel = new WorkItemRelation();
            rel.Attributes = new RelationAttributes() { Comment = "Test Comment" };
            rel.Rel = "Hyperlink";
            rel.Url = @"http:\\www.Skykick.com";
            item.Relations.Add(rel);

            var returnItem = await ClientRestClient.UpdateWorkItem(item);
        }

        private void StartProgressBar()
        {
            QueryInProgress = true;
            Console.Write("Working");
            while (QueryInProgress)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }
        }

        private async void VsoApi_ExecuteQuery(string input)
        {
            var queryPath = input.Remove(0, 5).Trim();

            var query = await ClientRestClient.GetQuery(Settings.Default.TfsDevOpsTeamProjectName, queryPath, 2);
            if (query == null || string.IsNullOrEmpty(query.Id))
            {
                Console.WriteLine("A query with this name could not be found.");
                QueryInProgress = false;
                return;
            }

            var flatResult = await ClientRestClient.RunFlatQuery(Settings.Default.TfsDevOpsTeamProjectName, query);
            if (flatResult == null || flatResult.WorkItems == null || !flatResult.WorkItems.Any())
            {
                Console.WriteLine("No work items IDs were returned from this query.");
                QueryInProgress = false;
                return;
            }

            var workItemIds = flatResult.WorkItems.Select(t => t.Id).ToArray();
            var workItems = await ClientRestClient.GetWorkItems(workItemIds);
            if (workItems == null || workItems.Items == null || !workItems.Items.Any())
            {
                Console.WriteLine("No work item contents were returned from this query.");
                QueryInProgress = false;
                return;
            }

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            foreach (var workItem in workItems.Items)
            {
                DateTime createDate;
                DateTime.TryParse(workItem.Fields["System.CreatedDate"].ToString(), out createDate);
                string formattedCreateDate = string.Format("{0}/{1}/{2} {3}:{4}",
                    createDate.Year, createDate.Month.ToString("00"), createDate.Day.ToString("00"),
                    createDate.Hour.ToString("00"), createDate.Minute.ToString("00"));
                Console.WriteLine("   {0} {1}    {2}    {3}", "GET", formattedCreateDate, workItem.Id, workItem.Fields["System.Title"]);
            }
            Console.WriteLine("   --------------------");
        }

        private static void PrintOptions()
        {
            Console.WriteLine("   ADD      ADD [title],[type] - Adds a new wok item with the given title and type.");
            Console.WriteLine("   EXEC     EXEC [QueryName] - Executes the given query.");
            Console.WriteLine("   GET      GET [ItemId] - Gets the item for the given Id.");
            Console.WriteLine("   EXIT     Exits the application.");
            Console.WriteLine("   HELP     Returns a list of options.");
            Console.WriteLine("   LQ       Returns a list of all queries.");
        }

        private void VsoApi_PrintAllQueries()
        {
            var queryList = ClientRestClient.GetQueries(Settings.Default.TfsDevOpsTeamProjectName, depth: 2);
            if (queryList == null || queryList.Result == null || queryList.Result.Items == null || !queryList.Result.Items.Any())
            {
                Console.WriteLine("No saved queries were found.");
                return;
            }

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            foreach (var r in queryList.Result.Items)
            {
                if (r.IsFolder)
                    PrintContentTree(r, "   ");
            }
            Console.WriteLine("   --------------------");
        }

        private void PrintContentTree(Query root, string prefix)
        {
            Console.WriteLine("{0}/{1}", prefix, root.Name);

            if (root.HasChildren || root.Children != null)
            {
                var childPrefix = prefix + "   ";
                foreach (var child in root.Children)
                {
                    if (child.IsFolder)
                        PrintContentTree(child, childPrefix);
                    else
                        Console.WriteLine("{0}{1}", childPrefix, child.Name);
                }
            }
        }

        private async void VsoApi_AddWorkItem(string input) //create input object containing all of the data from crm that we want
        {
            var inputParameteString = input.Remove(0, 4);
            var inputParameterArray = inputParameteString.Split(',');
            var title = inputParameterArray[0].Trim();
            var type = inputParameterArray[1].Trim();

            var item = new WorkItem();
            item.Fields["System.Title"] = title; //TODO: add work item fields that we want
            var workItem = await ClientRestClient.CreateWorkItem(Settings.Default.TfsDevOpsTeamProjectName, type, item);

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            // write work item to console
            DateTime createDate;
            DateTime.TryParse(workItem.Fields["System.CreatedDate"].ToString(), out createDate);
            string formattedCreateDate = string.Format("{0}/{1}/{2} {3}:{4}",
                createDate.Year, createDate.Month.ToString("00"), createDate.Day.ToString("00"),
                createDate.Hour.ToString("00"), createDate.Minute.ToString("00"));

            Console.WriteLine("    {0} {1}    {2}    {3}", "ADD", formattedCreateDate, workItem.Id, workItem.Fields["System.Title"]);
            Console.WriteLine("   --------------------");
        }

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
    }
}