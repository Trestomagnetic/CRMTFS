using System;
using System.Linq;
using System.Net;
using VisualStudioOnline.Api.Rest.V1.Client;
using VisualStudioOnline.Api.Rest.V1.Model;
using TfsPlayground.Properties;
using System.Threading;

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

            while (true) {
                var input = Console.ReadLine();
                switch (input.IndexOf(" ") > 0 ? input.Substring(0,input.IndexOf(" ")).ToUpper() : input.ToUpper()) {
                    case "LQ":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_PrintAllQueries();
                        break;
                    case "EXEC":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        VsoApi_ExecuteQuery(input);
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

        private void StartProgressBar()
        {
            QueryInProgress = true;
            Console.Write("Working");
            while (QueryInProgress) {
                Console.Write(".");
                Thread.Sleep(1000);
            }
        }

        private async void VsoApi_ExecuteQuery(string input)
        {
            var queryPath = input.Remove(0, 5).Trim();
            var client = VsoApi_GetClient();

            
            var query = await client.GetQuery(Settings.Default.TeamProject_DevOps, queryPath, 2);
            if (query == null || string.IsNullOrEmpty(query.Id)) {
                Console.WriteLine("A query with this name could not be found.");
                QueryInProgress = false;
                return;
            }

            var flatResult = await client.RunFlatQuery(Settings.Default.TeamProject_DevOps, query);
            if (flatResult == null || flatResult.WorkItems == null || !flatResult.WorkItems.Any()) {
                Console.WriteLine("No work items IDs were returned from this query.");
                QueryInProgress = false;
                return;
            }

            var workItemIds = flatResult.WorkItems.Select(t => t.Id).ToArray();
            var workItems = await client.GetWorkItems(workItemIds);
            if (workItems == null || workItems.Items == null || !workItems.Items.Any()) {
                Console.WriteLine("No work item contents were returned from this query.");
                QueryInProgress = false;
                return;
            }

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            foreach (var workItem in workItems.Items) {
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
            Console.WriteLine("   ADD      ADD [title],[type] - adds a new wok item with the given title and type.");
            Console.WriteLine("   EXEC     EXEC [QueryName] - Executes the given query.");
            Console.WriteLine("   EXIT     Exits the application.");
            Console.WriteLine("   HELP     Returns a list of options.");
            Console.WriteLine("   LQ       Returns a list of all queries.");
        }   

        private void VsoApi_PrintAllQueries()
        {
            var client = VsoApi_GetClient();

            var queryList = client.GetQueries(Settings.Default.TeamProject_DevOps, depth: 2);
            if (queryList == null || queryList.Result == null || queryList.Result.Items == null || !queryList.Result.Items.Any()) {
                Console.WriteLine("No saved queries were found.");
                return;
            }

            QueryInProgress = false;
            Console.WriteLine();
            Console.WriteLine("   --------------------");
            foreach (var r in queryList.Result.Items) {
                if (r.IsFolder)
                    PrintContentTree(r, "   ");
            }
            Console.WriteLine("   --------------------");
        }

        private void PrintContentTree(Query root, string prefix)
        {
            Console.WriteLine("{0}/{1}", prefix, root.Name);

            if (root.HasChildren || root.Children != null) {
                var childPrefix = prefix + "   ";
                foreach (var child in root.Children) {
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

            var client = VsoApi_GetClient();

            var item = new WorkItem();
            item.Fields["System.Title"] = title; //TODO: add work item fields that we want
            var workItem = await client.CreateWorkItem(Settings.Default.TeamProject_DevOps, type, item);

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

        private IVsoWit VsoApi_GetClient()
        {
            var netCred = new NetworkCredential(Settings.Default.PersonalAccessDescription, Settings.Default.PersonalAccessToken); //TODO - Make TFS credentials not Trestin's account
            var vsoClient = new VsoClient(Settings.Default.AccountName, netCred);
            var client = vsoClient.GetService<IVsoWit>();
            return client;
        }
    }
}