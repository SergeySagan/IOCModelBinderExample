using IOCModelBinderExample.Contracts;

namespace IOCModelBinderExample.Domain
{
    public class Customer : ICustomer
    {
        public long ID { get; set; }

        public string Name { get; set; }
    }
}