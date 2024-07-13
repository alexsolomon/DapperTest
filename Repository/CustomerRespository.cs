using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DapperTest.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Text.Json;
using System.Data;

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
        public List<Dictionary<string, object?>> GetDictionaryListForReader(IDataReader reader)
        {
            var items = new List<Dictionary<string, object?>>();

            string[] propertyNames = new string[reader.FieldCount];
            for (int count = 0; count < reader.FieldCount; count++)
            {
                propertyNames[count] = reader.GetName(count);
            }

            while (reader.Read())
            {
                var item = new Dictionary<string, object?>();
                for (int count = 0; count < reader.FieldCount; count++)
                {
                    string fieldName = propertyNames[count];
                    if (!reader.IsDBNull(count))
                    {
                        if (reader.GetFieldType(count) == typeof(string))
                        {
                            string value = reader.GetString(count);
                            if (value.ToString().Trim().StartsWith("{"))
                            {
                                try
                                {
                                    JsonDocument document = JsonDocument.Parse(value.ToString());
                                    item.Add(fieldName, ConvertJsonToDictionary(value));
                                }
                                catch (Exception)
                                {
                                    item.Add(fieldName, value);
                                }
                            }
                            else if (value.ToString().Trim().StartsWith("["))
                            {
                                try
                                {
                                    JsonDocument document = JsonDocument.Parse(value.ToString());
                                    item.Add(fieldName, ConvertJsonArrayToDictionaryList(value));
                                }
                                catch (Exception)
                                {
                                    item.Add(fieldName, value);
                                }
                            }
                            else
                            {
                                item.Add(fieldName, value);
                            }
                        }
                        else
                        {
                            item.Add(fieldName, reader.GetValue(count));
                        }
                    }
                    else
                    {
                        item.Add(fieldName, null);
                    }
                }

                items.Add(item);
            }

            return items;
        }
        private Dictionary<string, object?> ConvertJsonToDictionary(string value)
        {
            Dictionary<string, object?> dictObj = new Dictionary<string, object?>();

            JsonDocument document = JsonDocument.Parse(value.ToString());
            JsonElement rootElement = document.RootElement;
            foreach (var property in rootElement.EnumerateObject())
            {
                var jsonElement = property.Value;
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    dictObj.Add(property.Name, ConvertJsonToDictionary(jsonElement.GetRawText()));
                }
                else if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    dictObj.Add(property.Name, ConvertJsonArrayToDictionaryList(jsonElement.GetRawText()));
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    dictObj.Add(property.Name, jsonElement.GetString());
                }
                else if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    int intVal = 0;
                    double doubleVal = 0;
                    decimal decimalVal = 0;
                    if (jsonElement.TryGetInt32(out intVal))
                    {
                        dictObj.Add(property.Name, intVal);
                    }
                    else if (jsonElement.TryGetDouble(out doubleVal))
                    {
                        dictObj.Add(property.Name, doubleVal);
                    }
                    else if (jsonElement.TryGetDecimal(out decimalVal))
                    {
                        dictObj.Add(property.Name, decimalVal);
                    }
                    else
                    {
                        dictObj.Add(property.Name, jsonElement.GetRawText());
                    }
                }
                else
                {
                    dictObj.Add(property.Name, jsonElement.GetRawText());
                }
            }

            return dictObj;
        }

        private List<Dictionary<string, object?>> ConvertJsonArrayToDictionaryList(string value)
        {
            List<Dictionary<string, object?>> dictList = new List<Dictionary<string, object?>>();
            JsonDocument document = JsonDocument.Parse(value.ToString());
            JsonElement rootElement = document.RootElement;
            foreach (var element in rootElement.EnumerateArray())
            {
                dictList.Add(ConvertJsonToDictionary(element.GetRawText()));
            }

            return dictList;
        }
    }
}