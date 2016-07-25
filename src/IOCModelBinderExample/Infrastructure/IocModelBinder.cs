using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace IOCModelBinderExample.Infrastructure
{
    public class IocModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {   // For reference: https://github.com/aspnet/Mvc/issues/4196
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            if (bindingContext.Model == null && bindingContext.ModelType.Namespace.StartsWith("IOCModelBinderExample.ViewModels") &&
                (bindingContext.ModelType.IsInterface || bindingContext.ModelType.IsClass))
            {
                var serviceProvider = bindingContext.OperationBindingContext.HttpContext.RequestServices;
                var model = serviceProvider.GetRequiredService(bindingContext.ModelType);

                // Call model binding recursively to set properties
                bindingContext.Model = model;
                var result = await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(bindingContext);

                bindingContext.ValidationState[model] = new ValidationStateEntry() { SuppressValidation = true };

                return result;
            }

            return await ModelBindingResult.NoResultAsync;
        }
    }
}