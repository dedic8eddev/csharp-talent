using System;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly IntegrationTestFixture m_Fixture;
        protected readonly ITestOutputHelper m_Output;

        protected IntegrationTestBase(IntegrationTestFixture fixture, ITestOutputHelper output)
        {
            m_Fixture = fixture;
            m_Output = output;

            m_Fixture.EnsureStartupLogsOutput(m_Output);
        }

        public virtual void Dispose() { }
    }
}