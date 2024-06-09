using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.ModelBinding
{
    public class ExpandListBinderTests
    {
        private const string _MODEL_NAME = "SomePropName";

        public enum TestExpandValues
        {
            Cheese,
            Sausages
        }

        [Theory]
        [InlineData("cheese,sausages", new [] { TestExpandValues.Cheese, TestExpandValues.Sausages })]
        [InlineData("cheese", new [] { TestExpandValues.Cheese })]
        [InlineData("CHEESE", new [] { TestExpandValues.Cheese })]
        [InlineData("Sausages , Cheese", new [] { TestExpandValues.Cheese, TestExpandValues.Sausages })]
        public async Task BindModelSetsSuccessIfValidValues(string rawValue, TestExpandValues[] expectedMatches)
        {
            // Given
            var context = CreateBindingContext(rawValue);
            var binder = CreateBinder();

            // When
            await binder.BindModelAsync(context);

            // Then
            Assert.True(context.Result.IsModelSet);
            var resultList = Assert.IsType<ExpandList<TestExpandValues>>(context.Result.Model);
            Assert.Equal(expectedMatches.Length, resultList.Count);
            foreach (var expectedMatch in expectedMatches)
                Assert.Single(resultList.Where(v => v == expectedMatch));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task BindModelIgnoresIfEmpty(string rawValue)
        {
            // Given
            var context = CreateBindingContext(rawValue);
            var binder = CreateBinder();

            // When
            await binder.BindModelAsync(context);

            // Then
            Assert.False(context.Result.IsModelSet);
            Assert.Equal(0, context.ModelState.ErrorCount);
        }

        [Theory]
        // ReSharper disable StringLiteralTypo
        [InlineData("chees", "chees")]
        [InlineData("chees,sausages", "chees")]
        [InlineData("chees,sausa", "chees,sausa")]
        // ReSharper restore StringLiteralTypo
        public async Task BindModelSetsModelErrorsIfInvalidValues(string rawValue, string invalidValuesError)
        {
            // Given
            var context = CreateBindingContext(rawValue);
            var binder = CreateBinder();

            // When
            await binder.BindModelAsync(context);

            // Then
            Assert.False(context.Result.IsModelSet);
            Assert.Equal(1, context.ModelState.ErrorCount);
            var (modelStateKey, modelStateEntry) = Assert.Single(context.ModelState);
            Assert.Equal(_MODEL_NAME, modelStateKey);
            var error = Assert.Single(modelStateEntry.Errors);
            Assert.Equal($"Invalid values for {_MODEL_NAME}: {invalidValuesError}", error.ErrorMessage);
        }

        private static ExpandListBinder<TestExpandValues> CreateBinder()
        {
            return new ExpandListBinder<TestExpandValues>();
        }

        private static DefaultModelBindingContext CreateBindingContext(string rawValue)
        {
            return new DefaultModelBindingContext
                   {
                       ModelName = _MODEL_NAME,
                       ValueProvider = Mock.Of<IValueProvider>(p => p.GetValue(It.Is<string>(k => k == _MODEL_NAME)) == new ValueProviderResult(new StringValues(rawValue))),
                       ModelState = new ModelStateDictionary()
                   };
        }
    }
}
