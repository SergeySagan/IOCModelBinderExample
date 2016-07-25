using IOCModelBinderExample.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace IOCModelBinderExample.Domain
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IServiceProvider serviceProvider;

        public CustomerRepository(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ICustomer GetByID(long id)
        {
            var customer = serviceProvider.GetService<ICustomer>();

            customer.ID = id;
            customer.Name = "Customer Number #" + id;

            return customer;
        }
    }
}