using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DapperTest.Models;

namespace DapperTest.Business
{
    public interface ICustomerBusiness
    {
        Task<Customer> GetCustomer(string customerId);
        Task<List<Dictionary<string, object>>> GetCustomers();
    }
}