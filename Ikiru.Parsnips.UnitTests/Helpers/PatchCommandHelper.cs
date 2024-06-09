using Morcatko.AspNetCore.JsonMergePatch;
using Morcatko.AspNetCore.JsonMergePatch.SystemText.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class PatchCommandHelper
    {
        /// <summary>
        /// calls internal methods via reflection.
        /// Might need changing if implementation of Morcatko.AspNetCore.JsonMergePatch.JsonMergePatchDocument changes
        /// I could not find how to do it differently as mocking would require to look into internal implementation as well.
        /// </summary>
        /// <param name="command">name-object dictionary to form command</param>
        /// <typeparam name="T">Parch document command type</typeparam>
        /// <returns></returns>
        public static JsonMergePatchDocument<T> CreatePatchDocument<T>(Dictionary<string, object> command) where T : class
        {
            AssertAllPropertiesArePresent<T>(command);

            var bytes = JsonSerializer.SerializeToUtf8Bytes(command);
            using var jsonDocument = JsonDocument.Parse(bytes);
            var jsonElement = jsonDocument.RootElement.Clone();

            var document = (JsonMergePatchDocument<T>)typeof(PatchBuilder).GetMethod("CreatePatchDocument", BindingFlags.NonPublic | BindingFlags.Static)
                                                                             .Invoke(null, new object[] { typeof(T), jsonElement, new JsonSerializerOptions(), new JsonMergePatchOptions() });

            return document;
        }

        /// <summary>
        /// we need to make sure we have all properties from the dictionary in the T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        private static void AssertAllPropertiesArePresent<T>(Dictionary<string, object> command) where T : class
        {
            var properties = typeof(T).GetProperties().Select(p => new { p.Name, PropertyType = p.PropertyType, PropertyTypeName = GetPropertyTypeName(p.PropertyType) }).ToArray();
            var commandProperties = command.Select(p => new { Name = p.Key, PropertyType = p.Value?.GetType(), PropertyTypeName = GetPropertyTypeName(p.Value?.GetType()) });

            var missing = command.Keys.Where(k => !properties.Any(p => p.Name == k)).ToArray();
            if (missing.Any())
            {
                var missingFields = string.Join(",", missing);
                Assert.False(missing.Any(), $"Not expected field(s) found: {missingFields}");
            }

            var wrongType = commandProperties
                .Join(properties, p => p.Name, cp => cp.Name, (cp, p) => new
                {
                    p.Name,
                    p.PropertyTypeName,
                    p.PropertyType,
                    CommandName = cp.Name,
                    CommandPropertyTypeName = cp.PropertyTypeName,
                    CommandPropertyType = cp.PropertyType,
                })
                .Where(p => 
                {
                    if (Nullable.GetUnderlyingType(p.PropertyType) != null && p.CommandPropertyType == null)
                        return false;

                    var typesMatch = p.PropertyType.IsAssignableFrom(p.CommandPropertyType)
                        || (!p.PropertyType.IsValueType && p.CommandPropertyType == null)
                        || (p.PropertyTypeName.EndsWith("?") && p.CommandPropertyTypeName == null);
                    return !typesMatch;
                })
                .Select(p => $"'{p.Name}': expected '{p.PropertyTypeName}', provided '{p.CommandPropertyTypeName}'")
                .ToArray();

            if (wrongType.Any())
            {
                var wrongTypeFields = string.Join("\n", wrongType);
                Assert.False(wrongType.Any(), $"Not expected field type(s) found: {wrongTypeFields}");
            }
        }

        private static string GetPropertyTypeName(Type propertyType)
        {
            if (propertyType == null)
                return null;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return $"{propertyType.GetGenericArguments()[0]}?";

            return propertyType.FullName;
        }
    }
}