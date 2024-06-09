using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;

namespace Ikiru.Parsnips.UnitTests.Helpers.Validation
{
    public static class ValidatorTestExtensions
    {
        #region Child Property Assertions

        /// <summary>
        /// Asserts that there is not a validation error on a child property of a property on the item under test.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="propertyValue">The value to assign the property on the item under test. Should be seeded with the child property value to be tested.</param>
        /// <param name="childExpression">The expression of the child property - used to capture the name only.</param>
        public static void ShouldNotHaveChildValidationErrorFor<T, TProperty, TChild>(this IValidator<T> validator, Expression<Func<T, TProperty>> propertyExpression, TProperty propertyValue,  Expression<Func<TProperty, TChild>> childExpression) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, propertyValue);

            var result = validator.TestValidate(itemUnderTest);
            result.ShouldNotHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}.{GetPropertyName(childExpression)}");
        }

        /// <summary>
        /// Asserts that there is a validation error on a child property of a property on the item under test.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="propertyValue">The value to assign the property on the item under test. Should be seeded with the child property value to be tested.</param>
        /// <param name="childExpression">The expression of the child property - used to capture the name only.</param>
        public static void ShouldHaveChildValidationErrorFor<T, TProperty, TChild>(this IValidator<T> validator, Expression<Func<T, TProperty>> propertyExpression, TProperty propertyValue,  Expression<Func<TProperty, TChild>> childExpression) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, propertyValue);

            var result = validator.TestValidate(itemUnderTest);
            result.ShouldHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}.{GetPropertyName(childExpression)}");
        }

        
        /// <summary>
        /// Asserts that there is not a validation error on a child property of a property on the item under test.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="propertyValue">The value to assign the property on the item under test. Should be seeded with the child property value to be tested.</param>
        /// <param name="childExpression">The expression of the child property - used to capture the name only.</param>
        public static void ShouldNotHaveChildCollectionValidationErrorFor<T, TProperty, TChild>(this IValidator<T> validator, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, IEnumerable<TProperty> propertyValue, Expression<Func<TProperty, TChild>> childExpression) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, propertyValue);

            var result = validator.TestValidate(itemUnderTest);
            // Note: We don't include an index as the Fluent Validation assertion removes the collection indexer before checking - i.e. allows anywhere in the collection - see ValidationTestExtension.NormalizePropertyName
            result.ShouldNotHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}.{GetPropertyName(childExpression)}");
        }

        /// <summary>
        /// Asserts that there is a validation error on a child property of a property on the item under test.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="propertyValue">The value to assign the property on the item under test. Should be seeded with the child property value to be tested.</param>
        /// <param name="childExpression">The expression of the child property - used to capture the name only.</param>
        public static void ShouldHaveChildCollectionValidationErrorFor<T, TProperty, TChild>(this IValidator<T> validator, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression, IEnumerable<TProperty> propertyValue,  Expression<Func<TProperty, TChild>> childExpression, int expectedIndex) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, propertyValue);

            var result = validator.TestValidate(itemUnderTest);
            // Note: We don't include an index as the Fluent Validation assertion removes the collection indexer before checking - i.e. allows anywhere in the collection - see ValidationTestExtension.NormalizePropertyName
            result.ShouldHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}[{expectedIndex}].{GetPropertyName(childExpression)}");
            //result.ShouldHaveValidationErrorFor(propertyExpression);
        }
        
        #endregion

        #region String Length Assertions
        
        /// <summary>
        /// Asserts that there is a validation error if the string value is longer than the given max length.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="expression">The expression of the string property on the item under test.</param>
        /// <param name="maxLength">The max length for the property.</param>
        public static void ShouldHaveValidationErrorForExceedingMaxLength<T>(this IValidator<T> validator, Expression<Func<T, string>> expression, int maxLength) where T : class, new()
        {
            var value = 'a'.Repeat(maxLength + 1);
            validator.ShouldHaveValidationErrorFor(expression, value);
        }
        
        /// <summary>
        /// Asserts that there is not a validation error if the string value is the same than the given max length.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="expression">The expression of the string property on the item under test.</param>
        /// <param name="maxLength">The max length for the property.</param>
        public static void ShouldNotHaveValidationErrorForNotExceedingMaxLength<T>(this IValidator<T> validator, Expression<Func<T, string>> expression, int maxLength) where T : class, new()
        {
            validator.ShouldNotHaveValidationErrorForLength(expression, maxLength);
        }
        
        /// <summary>
        /// Asserts that there is a validation error if the string value is shorter than the given min length.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="expression">The expression of the string property on the item under test.</param>
        /// <param name="minLength">The min length for the property.</param>
        public static void ShouldHaveValidationErrorForUnderMinLength<T>(this IValidator<T> validator, Expression<Func<T, string>> expression, int minLength) where T : class, new()
        {
            var value = 'a'.Repeat(minLength - 1);
            validator.ShouldHaveValidationErrorFor(expression, value);
        }
        
        /// <summary>
        /// Asserts that there is not a validation error if the string value is the same than the given min length.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="expression">The expression of the string property on the item under test.</param>
        /// <param name="minLength">The min length for the property.</param>
        public static void ShouldNotHaveValidationErrorForMinLength<T>(this IValidator<T> validator, Expression<Func<T, string>> expression, int minLength) where T : class, new()
        {
            validator.ShouldNotHaveValidationErrorForLength(expression, minLength);
        }
        
        /// <summary>
        /// Asserts that there is not a validation error if the string value is the same as the given length.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="expression">The expression of the string property on the item under test.</param>
        /// <param name="length">The length of the string value to use for the property.</param>
        public static void ShouldNotHaveValidationErrorForLength<T>(this IValidator<T> validator, Expression<Func<T, string>> expression, int length) where T : class, new()
        {
            var value = 'a'.Repeat(length);
            validator.ShouldNotHaveValidationErrorFor(expression, value);
        }

        #endregion

        #region File Assertions

        /// <summary>
        /// Asserts that there is not a validation error on a allowed file.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="file">The value to assign the property on the item under test.</param>
        public static void ShouldNotHaveFileValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, IFormFile>> propertyExpression, IFormFile file) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, file);

            var result = validator.TestValidate(itemUnderTest);
            result.ShouldNotHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}");
        }

        /// <summary>
        /// Asserts that there is not a validation error on a file with a wrong parameter.
        /// </summary>
        /// <param name="validator">The validator of the item under test.</param>
        /// <param name="propertyExpression">The expression of the property on the item under test.</param>
        /// <param name="file">The value to assign the property on the item under test.</param>
        public static void ShouldHaveFileValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, IFormFile>> propertyExpression, IFormFile file) where T : class, new()
        {
            var itemUnderTest = new T();
            SetExpressionValue(itemUnderTest, propertyExpression, file);

            var result = validator.TestValidate(itemUnderTest);
            result.ShouldHaveValidationErrorFor($"{GetPropertyName(propertyExpression)}");
        }

        #endregion

        #region Expression Helpers

        private static void SetExpressionValue<T, TValue>(T itemUnderTest, Expression<Func<T, TValue>> propertyExpression, TValue propertyValue) 
        {
            var memberExpression = (MemberExpression)propertyExpression.Body;
            var property = (PropertyInfo)memberExpression.Member;

            property.SetValue(itemUnderTest, propertyValue, null);
        }

        private static string GetPropertyName<T, TValue>(Expression<Func<T, TValue>> propertyExpression) 
        {
            var memberExpression = (MemberExpression)propertyExpression.Body;
            var property = (PropertyInfo)memberExpression.Member;

            return property.Name;
        }

        #endregion
    }
}