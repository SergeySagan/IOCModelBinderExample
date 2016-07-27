using IOCModelBinderExample.Contracts;

namespace IOCModelBinderExample.ViewModels
{
    public class HomeViewModel : ICustomerViewModel
    {
        private readonly ICustomerRepository repository;
        private ICustomer customer;

        public HomeViewModel(ICustomerRepository repository)
        {
            this.repository = repository;
        }

        public long ID { get; set; }

        public ICustomer SelectedCustomer
        {
            get
            {
                if (ID == 0) return null;

                return customer ?? (customer = repository.GetByID(ID));
            }
        }
    }
}