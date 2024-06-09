using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels
{
    public class PortalUserTests
    {
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly PortalUser _user;
        private readonly string _validEmail = "valid@email.id";
        private readonly string _validUserName = "validUserName";
        private readonly Guid _validAssignment1Id = Guid.NewGuid();
        private readonly Guid _validAssignment2Id = Guid.NewGuid();
        private readonly Guid _validAddedBy1 = Guid.NewGuid();
        private readonly Guid _validAddedBy2 = Guid.NewGuid();

        public PortalUserTests()
        {
            _user = new PortalUser(_searchFirmId)
            {
                Email = _validEmail,
                UserName = _validUserName,
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(_validAssignment1Id, _validAddedBy1),
                    new PortalSharedAssignment(_validAssignment2Id, _validAddedBy2)
                }
            };
        }
        
        [Fact]
        public void CorrectPortalUserPassesValidation()
        {
            var validationErrors = _user.Validate();
            
            Assert.Empty(validationErrors);
        }

        public static IEnumerable<object[]> IncorrectPortalUserTestData()
        {
            yield return new object[] { new Action<PortalUser>(u => u.Email = "") };
            yield return new object[] { new Action<PortalUser>(u => u.Email = null) };
            yield return new object[] { new Action<PortalUser>(u => u.Email = "wrongemail.com") };
            yield return new object[] { new Action<PortalUser>(u => u.UserName = "") };
            yield return new object[] { new Action<PortalUser>(u => u.UserName = null) };
            yield return new object[] { new Action<PortalUser>(u => u.SharedAssignments.Add(new PortalSharedAssignment(Guid.Empty, Guid.NewGuid()))) };
            yield return new object[] { new Action<PortalUser>(u => u.SharedAssignments.Add(new PortalSharedAssignment(Guid.NewGuid(), Guid.Empty))) };
            yield return new object[] { new Action<PortalUser>(u => u.SharedAssignments.Add(new PortalSharedAssignment(Guid.Empty, Guid.Empty))) };
        }

        [Theory]
        [MemberData(nameof(IncorrectPortalUserTestData))]
        public void IncorrectPortalUserDoesNotPassValidation(Action<PortalUser> userModifier)
        {
            userModifier(_user);
            var validationErrors = _user.Validate();
            
            Assert.NotEmpty(validationErrors);
        }
    }
}