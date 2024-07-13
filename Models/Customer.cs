using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DapperTest.Models
{
    public record Customer
    {
        public string? CustomerId { get; set; }
        public string? CompanyName { get; set; }
    }
}