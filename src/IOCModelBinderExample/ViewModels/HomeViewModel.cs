using IOCModelBinderExample.Contracts;
using IOCModelBinderExample.Domain;

namespace IOCModelBinderExample.ViewModels
{
    public class HomeViewModel
    {
        private ICustomer customer;

        public HomeViewModel()
        {
        }

        public long ID { get; set; }

        public ICustomer SelectedCustomer
        {
            get
            {
                if (ID == 0) return null;

                return customer ?? (customer = new Customer { ID = ID, Name = "Customer Number #" + ID });
            }

            set
            {
                customer = value;
            }
        }
    }
}