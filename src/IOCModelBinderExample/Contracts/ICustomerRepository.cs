namespace IOCModelBinderExample.Contracts
{
    public interface ICustomerRepository
    {
        ICustomer GetByID(long id);
    }
}