using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace IOCModelBinderExample.Infrastructure
{
    public class IocModelBinder : ComplexTypeModelBinder
    {
        public IocModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders) : base(propertyBinders)
        {
        }

        protected override object CreateModel(ModelBindingContext bindingContext)
        {
            return bindingContext.HttpContext.RequestServices.GetRequiredService(bindingContext.ModelType);
        }
    }
}