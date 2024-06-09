using Xunit;

namespace Ikiru.Parsnips.UnitTests
{
    /// <summary>
    /// Base class for Validation Tests class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ValidationTests<T> : IClassFixture<ValidationTests<T>.ValidatorFixture> where T : class, new()
    {
        protected readonly ValidatorFixture Fixture;

        public class ValidatorFixture
        {
            public ValidatorFixture()
            {
                Validator = new T();
            }

            public T Validator { get; }
        }

        protected ValidationTests(ValidatorFixture fixture)
        {
            Fixture = fixture;
        }
    }
}
