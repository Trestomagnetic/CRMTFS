using CrmPlayground.Properties;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceModel.Description;
using System.Threading;
using System.Xml.Serialization;
using TfsPlayground;

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

        private IOrganizationService _service { get; set; }
        private IOrganizationService Service
        {
            get
            {
                if (_service == null)
                    _service = CreateTestCrmConnection();

                return _service;
            }
            set { _service = value; }
        }

        public void Start()
        {
            Service = CreateTestCrmConnection();

            PrintOptions();

            while (true) {
                var input = Console.ReadLine();
                switch (input.IndexOf(" ") > 0 ? input.Substring(0, input.IndexOf(" ")).ToUpper() : input.ToUpper()) {
                    case "GETID":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        GetOrderIdFromOrderName(input);
                        break;
                    case "GET":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        GetSwatTicket(input);
                        break;
                    case "HELP":
                        PrintOptions();
                        break;
                    case "LINK":
                        ThreadPool.QueueUserWorkItem(x => { StartProgressBar(); });
                        GetDirectLinks(input);
                        break;
                    case "EXIT":
                        return;
                    default:
                        Console.WriteLine("For a list of options type HELP");
                        break;
                }
            }
        }

        public SecureString GetPassword()
        {
            var password = new SecureString();
            ConsoleKeyInfo keyInfo;

            do {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Backspace) {
                    if (password.Length > 0) {
                        password.RemoveAt(password.Length - 1);
                        Console.Write("\b \b");
                    }
                } else if (keyInfo.Key != ConsoleKey.Enter) {
                    password.AppendChar(keyInfo.KeyChar);
                    Console.Write("*");
                }
            } while (keyInfo.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return password;
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
            Console.WriteLine("   GET      GET [TicketId] - Returns the SWAT ticket for the specified Id.");
            Console.WriteLine("   LINK     LINK [OrderNumber] - Returns the specified order and migration source link(s).");
            Console.WriteLine("   EXIT     Exits the application.");
            Console.WriteLine("   HELP     Returns a list of options.");
            Console.WriteLine("Please enter your selection...");
        }

        private IOrganizationService CreateTestCrmConnection()
        {
            var clientCredentials = new ClientCredentials();

            //TODO - Read CRM credentials from AD or maybe logged in user?
            if (!string.IsNullOrEmpty(Settings.Default.OverrideUsername))
                clientCredentials.UserName.UserName = Settings.Default.OverrideUsername;
            else {
                Console.Write("Enter Username: ");
                clientCredentials.UserName.UserName = Console.ReadLine();
            }

            Console.Write("[{0}] Password: ", clientCredentials.UserName.UserName);
            clientCredentials.UserName.Password = GetPassword().ToUnsecuredString();

            if (string.IsNullOrEmpty(clientCredentials.UserName.UserName))
                throw new Exception("Invalid username.");

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
                } catch (Exception ex) {
                    return null;
                }
            }

            return lDevice.User.ToClientCredentials();
        }

        private void GetDirectLinks(string input)
        {
            //ORD-90091-w2c9n6
            var orderNumber = input.Remove(0, 5).Trim();

            try {
                var orderIds = GetOrderIdFromOrderName(orderNumber);
                if (orderIds == null || !orderIds.Any())
                    return;

                var migrationSourceId = GetMigrationSourceIdFromOrderId(orderIds[0]);
                if (migrationSourceId == null || !migrationSourceId.Any())
                    return;

                QueryInProgress = false;
                Console.WriteLine();

                PrintCrmUrl(orderIds[0], 1088);
                migrationSourceId.ForEach(x => PrintCrmUrl(x, (int)CustomEnumerations.enumEntityTypeCode.MigrationSource));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private void GetSwatTicket(string input)
        {
            var guidId = input.Remove(0, 4).Trim();

            var query = new QueryExpression();
            query.EntityName = "cv_swatticket";
            query.ColumnSet.AllColumns = true;
            //query.Criteria.AddCondition(new ConditionExpression("objecttypecode", ConditionOperator.Equal, guidId));
            var ticketResults = Service.RetrieveMultiple(query);

            for (int i = 0; i < ticketResults.Entities.Count; i++)
            {
                if (ticketResults.Entities[i].Attributes["ownerid"] == null)
                    continue;

                var ownerId = (EntityReference)ticketResults.Entities[i].Attributes["ownerid"];
                Console.WriteLine(ownerId.Name);
            }

            ////query = new QueryExpression();
            ////query.EntityName = "annotation";
            ////query.ColumnSet.AllColumns = true;
            ////query.Criteria.AddCondition(new ConditionExpression("objecttypecode", ConditionOperator.Equal, "cv_swatticket"));
            ////query.Criteria.AddCondition(new ConditionExpression("createdon", ConditionOperator.GreaterThan, DateTime.Now.AddYears(-1)));
            ////var annotations = Service.RetrieveMultiple(query);

            ////List<Entity> ticketAnnotations = new List<Entity>();
            ////for (int i = 0; i < annotations.Entities.Count; i++)
            ////{
            ////    try
            ////    {
            ////        var objId = (EntityReference)annotations.Entities[i].Attributes["objectid"];
            ////        if (objId.Id == ticketEntity.Id)
            ////            ticketAnnotations.Add(annotations.Entities[i]);
            ////    }
            ////    catch (Exception ex)
            ////    { }
            ////}
        }

        private void PrintCrmUrl(Guid id, int etc)
        {
            var prefix = @"https://cloudvisors.crm.dynamics.com/main.aspx";
            var postfix = @"&pagetype=entityrecord";

            var crmUrl = string.Format("{0}?etc={1}&id=%7b{2}%7d{3}", prefix, etc, id, postfix);
            Console.WriteLine(crmUrl);
        }

        private List<Guid> GetOrderIdFromOrderName(string orderNumber)
        {
            var condition = new ConditionExpression("name", ConditionOperator.Equal, orderNumber);
            var filteredEntityCollection = GetEntityCollection("salesorder", condition);

            if (filteredEntityCollection == null || !filteredEntityCollection.Entities.Any())
                return null;

            try {
                return GetGuidsFromAttribute(filteredEntityCollection.Entities, "salesorderid");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Order Id not found for OrderNumber " + orderNumber + "...");
            return null;
        }

        private List<Guid> GetMigrationSourceIdFromOrderId(Guid orderId)
        {
            var condition = new ConditionExpression("cv_orderid", ConditionOperator.Equal, orderId);
            var filteredEntityCollection = GetEntityCollection("cv_migrationsource", condition);

            if (filteredEntityCollection == null || !filteredEntityCollection.Entities.Any())
                return null;

            try {
                return GetGuidsFromAttribute(filteredEntityCollection.Entities, "cv_migrationsourceid");
            }catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            
            Console.WriteLine("Migration Sources not found for OrderId " + orderId + "...");
            return null;
        }

        private List<Guid> GetGuidsFromAttribute(DataCollection<Entity> entityCollection, string attributeName)
        {
            var guids = new List<Guid>();

            var guidStrings = entityCollection.Select(e => e.Attributes[attributeName].ToString());
            foreach (var gs in guidStrings) {
                Guid g;
                if (Guid.TryParse(gs, out g)) {
                    //Console.WriteLine(attributeName + " - " + gs);
                    guids.Add(g);
                }
            }

            return guids;
        }

        private EntityCollection GetEntityCollection(string entityName, ConditionExpression condition = null, int? topCount = null)
        {
            try {
                //IOrganizationService service = CreateTestCrmConnection();

                QueryExpression queryExpression = new QueryExpression();
                queryExpression.EntityName = entityName;
                queryExpression.ColumnSet.AllColumns = true;
                queryExpression.TopCount = topCount;

                if (condition != null)
                    queryExpression.Criteria.AddCondition(condition);

                return Service.RetrieveMultiple(queryExpression);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}