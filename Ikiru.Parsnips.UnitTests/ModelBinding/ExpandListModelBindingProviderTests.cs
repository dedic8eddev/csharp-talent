using System;
using Ikiru.Parsnips.Api.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.ModelBinding
{
    public class ExpandListModelBindingProviderTests
    {
        public enum TypeOne { One }
        public enum TypeTwo { Two }

        [Theory]
        [InlineData(TypeOne.One)]
        [InlineData(TypeTwo.Two)]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters - used to specify generic type
        public void GetBinderReturnsCorrectType<T>(T _) where T : struct, Enum
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            // Given
            var context = CreateProviderContext<T>();
            var provider = CreateProvider();

            // When
            var binder = provider.GetBinder(context);

            // Then
            Assert.IsType<ExpandListBinder<T>>(binder);
        }

        private static ExpandListModelBindingProvider CreateProvider()
        {
            return new ExpandListModelBindingProvider();
        }

        private static ModelBinderProviderContext CreateProviderContext<T>() where T : struct, Enum
        {
            var meta = new Mock<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(ExpandList<T>)));

            var context = new Mock<ModelBinderProviderContext>();
            context.Setup(c => c.Metadata)
                   .Returns(meta.Object);

            return context.Object;
        }
    }
}