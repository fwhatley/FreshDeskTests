using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace FreshDeskTest1
{

    public class Account
    {
        public string Email { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public IList<string> Roles { get; set; }
    }

    public class Organization
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("domains")]
        public IList<string> Domains { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }
    }

    internal class Program
    {
        private static string API_URL;
        public static string USERNAME;
        public static string PASSWORD;

        private static void Main(string[] args)
        {
            initializeFieldsWithTestFD();

            //Test_GetCompanies();
            printJsonStringToFile();
            //Test_PostCompany();
            //Test_PutOnFirstCompanyReturnedFromGetCompanies();
            Test_GetCompanyToMapToMPDistrict();
            //Test_AddUpdateCompany();

            Console.ReadLine();
        }

        private static void printJsonStringToFile()
        {
            string json = Test_GetCompanies();
            
            File.WriteAllText(@"C:\Users\username\Desktop\fdcurltest\tempFromProgram.json", json);
        }

        private static void Test_PutOnFirstCompanyReturnedFromGetCompanies()
        {
            var company = GetFirstCompany();
            long id = company.Id;

            // build json
            Organization dist = new Organization();
            dist.Name = "3fold updated";
            dist.Description = "no description \\r newline description";

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"name\"           : \"{0}\",", dist.Name);
            sb.AppendFormat("\"description\"    : \"{0}\"", dist.Description);
            sb.Append("}");
            String bodyData = sb.ToString();

            // perform request
            var apiPath = "/companies/" + id;
            var client = GetClient();
            client.BaseUrl = new Uri(API_URL + apiPath);
            RestRequest request = new RestRequest(Method.PUT);

            const string contentType = "application/json";
            request.AddParameter(contentType, bodyData, ParameterType.RequestBody);

            var response = client.Execute(request);
            Console.WriteLine(response.Content);

        }

        private static Organization GetFirstCompany()
        {
            string companiesJson = Test_GetCompanies();
            var companies = JsonToCompaniesMapper(companiesJson);
            return companies.First();
        }

        private static List<Organization> JsonToCompaniesMapper(string companiesJson)
        {
            return JsonConvert.DeserializeObject<List<Organization>>(companiesJson);
        }

        # region initializers
        private static void initializeFieldsWithTestFD()
        {
            API_URL = "https://yyy.freshdesk.com/api/v2";
            USERNAME = "yyy@gmail.com";
            PASSWORD = "yyy";
        }

        # endregion

        private static String Test_GetCompanies()
        {
            List<Organization> organizations;

            var apiPath = "/companies";
            var client = GetClient();
            client.BaseUrl = new Uri(API_URL + apiPath);

            var request = new RestRequest();

            IRestResponse response = client.Execute(request);
            string content = response.Content;
            string linkToNextPage = response.Headers.ToList().Find(h => h.Name == "Link").Value.ToString();
            organizations = JsonToCompaniesMapper(content);

            // get all pages
            int page = 0;
            while (linkToNextPage != "no-cache")
            {
                page++;
                client.BaseUrl = new Uri(API_URL + apiPath + "?page=" + page);
                response = client.Execute(request);
                organizations.AddRange(JsonToCompaniesMapper(response.Content));
                linkToNextPage = response.Headers.ElementAt(1).Value.ToString();
            }

            return JsonConvert.SerializeObject(organizations);
        }

        private static void Test_PostCompany()
        {
            Organization dist = new Organization();
            dist.Id = 17000004155;
            dist.Name = "3fold v7";
            dist.Description = "no description";
            dist.Domains = new List<string>();
            dist.Domains.Add("eduorg.com");
            dist.Note = "no notes by fredy 7";

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"name\"           : \"{0}\",", dist.Name);
            sb.AppendFormat("\"description\"    : \"{0}\"", dist.Description);
            sb.Append("}");
            String bodyData = sb.ToString();

            var client = GetClient();
            var apiPath = "/companies";
            client.BaseUrl = new Uri(API_URL + apiPath);
            RestRequest request = new RestRequest(Method.POST);

            const string contentType = "application/json";
            request.AddParameter(contentType, bodyData, ParameterType.RequestBody);

            var response = client.Execute(request);
            Console.WriteLine(response.Content);

        }

        private static async void Test_GetCompanyToMapToMPDistrict()
        {
            var apiPath = "/companies";
            var endpoint = "/17000004155";

            var client = GetClient();
            client.BaseUrl = new Uri(API_URL + apiPath + endpoint);
            var response = await client.ExecuteGetTaskAsync(new RestRequest(Method.GET));
            Organization district = JsonConvert.DeserializeObject<Organization>(response.Content);

            Console.WriteLine("Test_GetCompanyToMapToMPDistrict: id: " + district.Id);
            Console.WriteLine("Test_GetCompanyToMapToMPDistrict: name: " + district.Name);
        }

        private static async Task Test_AddUpdateCompany()
        {
            Organization dist = new Organization();
            dist.Id = 17000004155;
            dist.Name = "3fold v8";
            dist.Description = "no description";

            bool distExists = await DoesDistrictExist(170000041558);
            string requMethod = distExists ? "PUT" : "POST";
            if (requMethod.Equals("PUT"))
            {
                Console.WriteLine("===doing a PUT===");
            }
            else
            {
                Console.WriteLine("===doing a POST===");
            }

            string endpoint = distExists ? "/" + dist.Id : "";
            string apiPath = "/companies";

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"name\"           : \"{0}\",", dist.Name);
            sb.AppendFormat("\"description\"    : \"{0}\"", dist.Description);
            sb.Append("}");
            string bodyData = sb.ToString();

            var client = GetClient();
            client.BaseUrl = new Uri(API_URL + apiPath + endpoint);

            RestRequest request = new RestRequest(Method.POST);
            const string contentType = "application/json";
            request.AddParameter(contentType, bodyData, ParameterType.RequestBody);

            var response = client.Execute(request);
            Console.WriteLine(response.Content);

        }

        private static RestClient GetClient()
        {
            var client = new RestClient();
            client.Authenticator = new HttpBasicAuthenticator(USERNAME, PASSWORD);

            return client;
        }

        private static async Task<String> GetDataString(string resource)
        {
            var client = GetClient();
            client.BaseUrl = new Uri(resource);
            var getResponse =
                await client.ExecuteGetTaskAsync(new RestRequest(Method.GET) {RequestFormat = DataFormat.Xml});

            return getResponse.StatusCode == HttpStatusCode.OK ? getResponse.Content : String.Empty;
        }

        private static async Task<bool> DoesDistrictExist(long id)
        {
            String url = API_URL + "/" + id;
            var companiesJson = await GetDataString(url);
            Console.WriteLine(companiesJson);
            return !String.IsNullOrEmpty(companiesJson.ToString());
        }
    }

}
