using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DapperTest.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Text.Json;

namespace DapperTest.Repository
{
    public class CustomerRespository : ICustomerRespository
    {
        private readonly IConfiguration _configuration;
        public CustomerRespository(IConfiguration configuration)
        {
            _configuration = configuration;

        }
        public async Task<Customer> GetCustomer(string customerId)
        {
            string sql = "select CustomerId,CompanyName from Customers where CustomerId = @customerId";
            using var connection = new SqlConnection(_configuration.GetConnectionString("Northwind"));
            var customer = await connection.QueryFirstAsync<Customer>(sql, new { customerId = customerId });
            return customer;
        }
        public async Task<List<Dictionary<string, object>>> GetCustomers()
        {
            string sql = "select CustomerId,CompanyName from Customers";
            using var connection = new SqlConnection(_configuration.GetConnectionString("Northwind"));
            var customers = await connection.QueryAsync(sql);
            return customers.Select(x => (Dictionary<string, object>)ToDictionaryObject(x)).ToList();
        }
        private Dictionary<string, object?> ToDictionaryObject(object value)
        {
            IDictionary<string, object>? dapperRowProperties = value as IDictionary<string, object>;

            Dictionary<string, object?> obj = new Dictionary<string, object?>();

            if (dapperRowProperties != null)
            {
                foreach (KeyValuePair<string, object> property in dapperRowProperties)
                {
                    bool isPropertyAdded = false;
                    var propertyValueString = property.Value?.ToString();
                    if (property.Value != null &&
                        (property.Value.GetType() == typeof(string) || property.Value.GetType() == typeof(JsonElement)) &&
                        !string.IsNullOrEmpty(property.Value.ToString()) &&
                        propertyValueString != null &&
                        (propertyValueString.Trim().StartsWith("[") ||
                         propertyValueString.Trim().StartsWith("{")))
                    {
                        JsonDocument? json = null;
                        try
                        {
                            json = JsonDocument.Parse(propertyValueString);
                            var jsonType = json.GetType();
                            if (json.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                var originalList =
                                    JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                                        propertyValueString);
                                var listToAdd = new List<Dictionary<string, object?>>();
                                if (originalList != null)
                                {
                                    foreach (var element in originalList)
                                    {
                                        listToAdd.Add(ToDictionaryObject(element));
                                    }
                                }


                                obj.Add(property.Key, listToAdd);
                                isPropertyAdded = true;
                            }
                            else if (json.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                obj.Add(property.Key,
                                    ToDictionaryObject(
                                        JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                            propertyValueString)));
                                isPropertyAdded = true;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (!isPropertyAdded)
                    {
                        obj.Add(property.Key, property.Value);
                    }
                }
            }

            return obj;
        }
    }
}