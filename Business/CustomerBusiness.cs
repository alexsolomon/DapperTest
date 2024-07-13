using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DapperTest.Models;
using DapperTest.Repository;

namespace DapperTest.Business
{
    public class CustomerBusiness : ICustomerBusiness
    {
        private readonly ICustomerRespository _cusomerRepository;
        public CustomerBusiness(ICustomerRespository cusomerRepository)
        {
            _cusomerRepository = cusomerRepository;
            
        }
        public async Task<Customer> GetCustomer(string customerId)
        {
            return await _cusomerRepository.GetCustomer(customerId);
        }

        public async Task<List<Dictionary<string, object>>> GetCustomers()
        {
            return await _cusomerRepository.GetCustomers();
        }
    }
}