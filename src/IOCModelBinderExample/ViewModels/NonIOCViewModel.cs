using IOCModelBinderExample.Contracts;
using IOCModelBinderExample.Domain;

namespace IOCModelBinderExample.ViewModels
{
    public class NonIOCViewModel : ICustomerViewModel
    {
        private Customer customer;

        public long ID { get; set; }

        public ICustomer SelectedCustomer
        {
            get
            {
                if (ID == 0) return null;

                return customer ?? (customer = GetCustomer(ID));
            }
        }

        private Customer GetCustomer(long id)
        {
            var c = new Customer();

            c.ID = id;
            c.Name = "Customer #" + id;

            return c;
        }
    }
}