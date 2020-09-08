using Lunitor.Health.Server.Notification;
using Lunitor.Health.Shared;
using MimeKit;
using MimeKit.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lunitor.Health.Server.UnitTests.Notification
{
    public class EmailBuilderTests
    {
        private EmailBuilder emailBuilder;

        private const string TestSenderName = "testname";
        private const string TestSenderEmail = "test@email.net";
        private readonly string[] TestToEmails = new string[]
            { 
                "testto1@email.net",
                "testto2@email.net",
                "testto3@email.net",
            };

        public EmailBuilderTests()
        {
            emailBuilder = new EmailBuilder();
        }

        [Theory]
        [MemberData(nameof(NullEmptyServiceCheckResultsTestData))]
        public void BuildUnhealthyServicesEmailShouldThrowArgumentExceptionIfNullOrEmptyServiceCheckResultsPassed(IEnumerable<ServiceCheckResultDto> serviceCheckResults)
        {
            Assert.ThrowsAny<ArgumentException>(() => emailBuilder.BuildUnhealthyServicesEmail(serviceCheckResults,
                TestSenderName,
                TestSenderEmail,
                TestToEmails));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void BuildUnhealthyServicesEmailShouldThrowArgumentExceptionIfNullOrEmptySenderNamePassed(string senderName)
        {
            Assert.ThrowsAny<ArgumentException>(() => emailBuilder.BuildUnhealthyServicesEmail(TestDataProvider.TestServiceErrors,
                senderName,
                TestSenderEmail,
                TestToEmails));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void BuildUnhealthyServicesEmailShouldThrowArgumentExceptionIfNullOrEmptySenderEmailPassed(string senderEmail)
        {
            Assert.ThrowsAny<ArgumentException>(() => emailBuilder.BuildUnhealthyServicesEmail(TestDataProvider.TestServiceErrors,
                TestSenderName,
                senderEmail,
                TestToEmails));
        }

        [Theory]
        [MemberData(nameof(NullEmptyToEmailsParameterTestData))]
        public void BuildUnhealthyServicesEmailShouldThrowArgumentExceptionIfNullOrEmptyToEmailPassed(string[] toEmails)
        {
            Assert.ThrowsAny<ArgumentException>(() => emailBuilder.BuildUnhealthyServicesEmail(TestDataProvider.TestServiceErrors,
                TestSenderName,
                TestSenderEmail,
                toEmails));
        }

        [Fact]
        public void BuildUnhealthyServicesEmailShouldReturnMimeMessageThatContainsGivenFromData()
        {
            var message = emailBuilder.BuildUnhealthyServicesEmail(TestDataProvider.TestServiceErrors,
                TestSenderName,
                TestSenderEmail,
                TestToEmails);

            Assert.NotNull(message);

            var from = message.From.First() as MailboxAddress;
            Assert.Equal(TestSenderName, from.Name);
            Assert.Equal(TestSenderEmail, from.Address);
        }

        [Fact]
        public void BuildUnhealthyServicesEmailShouldReturnMimeMessageThatContainsGivenToEmailsInBCC()
        {
            var message = emailBuilder.BuildUnhealthyServicesEmail(TestDataProvider.TestServiceErrors,
                TestSenderName,
                TestSenderEmail,
                TestToEmails);

            Assert.NotNull(message);

            var bccAddresses = message.Bcc.Mailboxes.Select(mbox => mbox.Address);
            foreach (var toEmails in TestToEmails)
            {
                Assert.Contains(toEmails, bccAddresses);
            }
        }

        public static IEnumerable<object[]> NullEmptyServiceCheckResultsTestData => new List<object[]>
        {
            new object[] { null },
            new object[] { new List<ServiceCheckResultDto>() }
        };

        public static IEnumerable<object[]> NullEmptyToEmailsParameterTestData => new List<object[]>
        {
            new object[] { null },
            new object[] { new string[] { } }
        };
    }
}
