using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Globalization;
using System.IO;
using System.ServiceModel.Description;
using System.Threading;
using System.Xml.Serialization;

namespace CrmPlayground
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
                switch (input.IndexOf(" ") > 0 ? input.Substring(0, input.IndexOf(" ")).ToUpper() : input.ToUpper()) {
                    case "GETID":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        GetOrderIdFromName(input);
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

        private static void PrintOptions()
        {
            Console.WriteLine("   GETID    GETID [OrderNumber] - Returns the specified order's Id.");
            Console.WriteLine("   EXIT     Exits the application.");
            Console.WriteLine("   HELP     Returns a list of options.");
        }

        private static IOrganizationService CreateTestCrmConnection()
        {
            var clientCredentials = new ClientCredentials();
            clientCredentials.UserName.UserName = @""; //TODO - Make CRM credentials not hard coded
            clientCredentials.UserName.Password = "";

            if (string.IsNullOrEmpty(clientCredentials.UserName.UserName))
                throw new Exception("Invalid user name.");

            if (string.IsNullOrEmpty(clientCredentials.UserName.Password))
                throw new Exception("Invalid password.");

            var crmUri = new Uri(@"https://cloudvisors.api.crm.dynamics.com/XRMServices/2011/Organization.svc");

            var deviceCredentials = GetDeviceCredentials();

            OrganizationServiceProxy connection = new OrganizationServiceProxy(
                crmUri,
                null,
                clientCredentials,
                deviceCredentials);

            // This is required to convert CRM query results to typed objects, otherwise it returns key-value pairs.
            connection.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());

            return connection;
        }

        private static ClientCredentials GetDeviceCredentials()
        {
            var liveDeviceFileNameFormat = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "LiveDeviceID"), "LiveDevice{0}.xml");

            var environmentFileInfo = new FileInfo(liveDeviceFileNameFormat);

            if (!environmentFileInfo.Exists) {
                return null;
            }

            LiveDevice lDevice;
            using (var stream = environmentFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
                XmlSerializer serializer = new XmlSerializer(typeof(LiveDevice), string.Empty);

                try {
                    lDevice = (LiveDevice)serializer.Deserialize(stream);
                }
                catch(Exception ex) {
                    return null;
                }
            }

            return lDevice.User.ToClientCredentials();
        }

        private void GetOrderIdFromName(string input)
        {
            //ORD-56043-Z5B6R1
            var orderNumber = input.Remove(0, 5).Trim();

            try {
                IOrganizationService service = CreateTestCrmConnection();
                //OrganizationServiceDecorator serviceHook = new OrganizationServiceDecorator(service);
                //OrganizationServiceContext context = new OrganizationServiceContext(serviceHook);


                QueryExpression queryExpression = new QueryExpression();
                queryExpression.EntityName = "salesorder";
                queryExpression.ColumnSet.AllColumns = true;
                //queryExpression.TopCount = 100; //no need to limit this at this point
                queryExpression.Criteria.AddCondition(new ConditionExpression("name", ConditionOperator.Equal, orderNumber));
                var something = service.RetrieveMultiple(queryExpression);

                if (something == null)
                    return;

                QueryInProgress = false;
                Console.WriteLine();
                foreach (var item in something.Entities) {
                    if (item.Attributes["name"].ToString() == "ORD-56043-Z5B6R1") {
                        Console.WriteLine(item.Attributes["salesorderid"]);
                        return;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }
            Console.WriteLine("Order Number not found...");
        }
    }
}
