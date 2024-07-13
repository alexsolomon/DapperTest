using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DapperTest.Business;
using Microsoft.AspNetCore.Mvc;

namespace DapperTest.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerBusiness _customerBusiness;

        public CustomerController(ICustomerBusiness customerBusiness)
        {
            _customerBusiness = customerBusiness;
        }
        [HttpGet, Route("/")]
        public async Task<IActionResult> GetCustomers()
        {
            return Ok(await _customerBusiness.GetCustomers());
        }
        [HttpGet, Route("/{customerId}")]
        public async Task<IActionResult> GetCustomer(string customerId)
        {
            return Ok(await _customerBusiness.GetCustomer(customerId));
        }
    }
}