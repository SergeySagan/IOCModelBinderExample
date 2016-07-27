using IOCModelBinderExample.Contracts;

namespace IOCModelBinderExample.ViewModels
{
    public interface ICustomerViewModel
    {
        long ID { get; set; }

        ICustomer SelectedCustomer { get; }
    }
}