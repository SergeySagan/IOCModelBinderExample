using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace IOCModelBinderExample.Infrastructure
{
    public static class IOCModelBinderExtensions
    {
        private static readonly Stack<State> _stack = new Stack<State>();
        private static State _state;

        public static NestedScope EnterNestedScope(this ModelBindingContext context, ModelMetadata modelMetadata, string fieldName, string modelName, object model)
        {
            if (modelMetadata == null)
            {
                throw new ArgumentNullException(nameof(modelMetadata));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            var scope = EnterNestedScope(context);

            // Only filter if the new BindingSource affects the value providers. Otherwise we want to preserve the currrent state.
            if (modelMetadata.BindingSource != null && !modelMetadata.BindingSource.IsGreedy)
            {
                context.ValueProvider = FilterValueProvider(context.ValueProvider, modelMetadata.BindingSource);
            }

            context.Model = model;
            context.ModelMetadata = modelMetadata;
            context.ModelName = modelName;
            context.FieldName = fieldName;
            context.BinderModelName = modelMetadata.BinderModelName;
            context.BindingSource = modelMetadata.BindingSource;
            context.PropertyFilter = modelMetadata.PropertyBindingPredicateProvider?.PropertyFilter;

            context.IsTopLevelObject = false;

            return scope;
        }

        public static NestedScope EnterNestedScope(ModelBindingContext context)
        {
            _stack.Push(_state);

            return new NestedScope(context);
        }

        public static void ExitNestedScope(this ModelBindingContext context)
        {
            _state = _stack.Pop();
        }

        private static IValueProvider FilterValueProvider(IValueProvider valueProvider, BindingSource bindingSource)
        {
            if (bindingSource == null || bindingSource.IsGreedy)
            {
                return valueProvider;
            }

            var bindingSourceValueProvider = valueProvider as IBindingSourceValueProvider;
            if (bindingSourceValueProvider == null)
            {
                return valueProvider;
            }

            return bindingSourceValueProvider.Filter(bindingSource) ?? new CompositeValueProvider();
        }

        public struct NestedScope : IDisposable
        {
            private readonly ModelBindingContext context;

            public NestedScope(ModelBindingContext context)
            {
                this.context = context;
            }

            public void Dispose()
            {
                context.ExitNestedScope();
            }
        }

        private struct State
        {
            public string BinderModelName;
            public BindingSource BindingSource;
            public string FieldName;
            public bool IsTopLevelObject;
            public object Model;
            public ModelMetadata ModelMetadata;
            public string ModelName;

            public Func<ModelMetadata, bool> PropertyFilter;
            public ModelBindingResult Result;
            public IValueProvider ValueProvider;
        };
    }

    public class IocModelBinder : IModelBinder
    {
        /* Refs:
            http://stackoverflow.com/questions/27448198/what-is-the-correct-way-to-create-custom-model-binders-in-mvc6/38618708#38618708
            https://github.com/aspnet/Mvc/issues/4703
            https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Abstractions/ModelBinding/ModelBindingContext.cs
            https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Core/Internal/DefaultModelBindingContext.cs
            https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Core/ModelBinding/Binders/ComplexTypeModelBinder.cs

            My issue: https://github.com/aspnet/Mvc/issues/4196
        */

        private IDictionary<ModelMetadata, IModelBinder> propertyBinders;

        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {   // For reference: https://github.com/aspnet/Mvc/issues/4196
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            if (propertyBinders == null && bindingContext.ModelMetadata.IsComplexType && !bindingContext.ModelMetadata.IsCollectionType)
            {
                propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in bindingContext.ModelMetadata.Properties)
                {
                    propertyBinders.Add(property, bindingContext.CreateBinder(property));
                }
            }

            if (bindingContext.IsTopLevelObject && bindingContext.Model == null)
            //if (bindingContext.Model == null && bindingContext.ModelType.Namespace.StartsWith("IOCModelBinderExample.ViewModels") &&
            //    (bindingContext.ModelType.IsInterface || bindingContext.ModelType.IsClass))
            {
                bindingContext.Model = CreateModel(bindingContext);

                foreach (var property in bindingContext.ModelMetadata.Properties)
                {
                    if (!CanBindProperty(bindingContext, property))
                        continue;

                    object propertyModel = null;
                    if (property.PropertyGetter != null && property.IsComplexType && !property.ModelType.IsArray)
                        propertyModel = property.PropertyGetter(bindingContext.Model);

                    var fieldName = property.BinderModelName ?? property.PropertyName;
                    var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

                    ModelBindingResult result;
                    using (bindingContext.EnterNestedScope(modelMetadata: property, fieldName: fieldName, modelName: modelName, model: propertyModel))
                    {
                        result = await BindProperty(bindingContext);
                    }

                    if (result.IsModelSet)
                        SetProperty(bindingContext, modelName, property, result);
                    else if (property.IsBindingRequired)
                    {
                        var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
                        bindingContext.ModelState.TryAddModelError(modelName, message);
                    }
                }

                return ModelBindingResult.Success(bindingContext.ModelName, bindingContext.Model);
            }

            return await ModelBindingResult.NoResultAsync;
        }

        public override IModelBinder CreateBinder(ModelMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            // For non-root nodes we use the ModelMetadata as the cache token. This ensures that all non-root nodes with the same metadata will have the the same binder. This is OK because for an
            // non-root node there's no opportunity to customize binding info like there is for a parameter.
            var token = metadata;

            var nestedContext = new DefaultModelBinderProviderContext(this, metadata);
            return _factory.CreateBinderCoreCached(nestedContext, token);
        }

        public object CreateModel(ModelBindingContext bindingContext)
        {
            var serviceProvider = bindingContext.OperationBindingContext.HttpContext.RequestServices;

            var model = serviceProvider.GetRequiredService(bindingContext.ModelType);

            return model;
        }

        private static void AddModelError(Exception exception, string modelName, ModelBindingContext bindingContext, ModelBindingResult result)
        {
            var targetInvocationException = exception as TargetInvocationException;
            if (targetInvocationException != null && targetInvocationException.InnerException != null)
                exception = targetInvocationException.InnerException;

            var modelState = bindingContext.ModelState;
            var validationState = modelState.GetFieldValidationState(modelName);
            if (validationState == ModelValidationState.Unvalidated)
                modelState.AddModelError(modelName, exception, bindingContext.ModelMetadata);
        }

        private static bool CanUpdatePropertyInternal(ModelMetadata propertyMetadata)
        {
            return !propertyMetadata.IsReadOnly || CanUpdateReadOnlyProperty(propertyMetadata.ModelType);
        }

        private static bool CanUpdateReadOnlyProperty(Type propertyType)
        {
            if (propertyType.GetType().IsValueType)
                return false;

            if (propertyType.IsArray)
                return false;

            if (propertyType == typeof(string))
                return false;

            return true;
        }

        private async Task<ModelBindingResult> BindProperty(ModelBindingContext bindingContext)
        {
            var binder = propertyBinders[bindingContext.ModelMetadata];

            return await binder.BindModelAsync(bindingContext);
        }

        private bool CanBindProperty(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
        {
            var modelMetadataPredicate = bindingContext.ModelMetadata.PropertyBindingPredicateProvider?.PropertyFilter;
            if (modelMetadataPredicate?.Invoke(bindingContext, propertyMetadata.PropertyName) == false)
                return false;

            if (bindingContext.PropertyFilter?.Invoke(bindingContext, propertyMetadata.PropertyName) == false)
                return false;

            if (!propertyMetadata.IsBindingAllowed)
                return false;

            if (!CanUpdatePropertyInternal(propertyMetadata))
                return false;

            return true;
        }

        private void SetProperty(ModelBindingContext bindingContext, string modelName, ModelMetadata propertyMetadata, ModelBindingResult result)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            if (modelName == null)
                throw new ArgumentNullException(nameof(modelName));

            if (propertyMetadata == null)
                throw new ArgumentNullException(nameof(propertyMetadata));

            if (!result.IsModelSet || propertyMetadata.IsReadOnly)
                return;

            try
            {
                propertyMetadata.PropertySetter(bindingContext.Model, result.Model);
            }
            catch (Exception exception)
            {
                AddModelError(exception, modelName, bindingContext, result);
            }
        }

        private class DefaultModelBinderProviderContext
        {   // I should not be recreating all of this!!! Punt on this untill we get to RTM!
            public DefaultModelBinderProviderContext(ModelMetadata metadata)
            {
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = metadata.BinderModelName,
                    BinderType = metadata.BinderType,
                    BindingSource = metadata.BindingSource,
                    PropertyFilterProvider = metadata.PropertyFilterProvider,
                };
            }
        }
    }
}