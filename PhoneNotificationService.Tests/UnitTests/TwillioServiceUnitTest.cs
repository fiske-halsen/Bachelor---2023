using EMSuite.PhoneNotification.Models;
using EMSuite.PhoneNotification.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Twilio.Types;

namespace PhoneNotificationService.Tests.UnitTests
{
    [TestFixture]
    public class TwillioServiceUnitTest
    {
        Mock<ITwillioCallHandler> _callHandlerMock;
        Mock<IConfiguration> _configurationMock;
        TwillioService _twillioService;

        [SetUp]
        public void SetUp()
        {
            _callHandlerMock = new Mock<ITwillioCallHandler>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["Twilio:AccountSid"]).Returns("test-account-sid");
            _configurationMock.Setup(x => x["Twilio:AuthToken"]).Returns("test-auth-token");
            _configurationMock.Setup(x => x["Twilio:PhoneNumber"]).Returns("+1234567890");
            _twillioService = new TwillioService(_configurationMock.Object, _callHandlerMock.Object);
        }

        [Test]
        public void MakeCall_ValidData_CallsTwilioCallResourceCreate_Positive()
        {
           //Arrange
            string testUrl = "https://example.com/test.mp3";
            string testPhoneNumber = "+0987654321";


            _callHandlerMock.Setup(c => c.Create(It.IsAny<PhoneNumber>(), It.IsAny<PhoneNumber>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Mock.Of<ICallResource>(x => x.Sid == "TestCallSid"));

            // Act
            var result =  _twillioService.MakeCall(testUrl, testPhoneNumber);
            var expected = "TestCallSid";

            // Assert
            _callHandlerMock.Verify(x => x.Create(It.IsAny<PhoneNumber>(), It.IsAny<PhoneNumber>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            result.Sid.Should().Be(expected);
        }

        [Test]
        public void MakeCall_ValidData_CallsTwilioCallResourceCreate_Negative()
        {
            // Arrange
            string testUrl = "https://example.com/test.mp3";
            string testPhoneNumber = "+45913009";

            _callHandlerMock.Setup(c => c.Create(It.IsAny<PhoneNumber>(), It.IsAny<PhoneNumber>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Failed to create call"));

            // Act & Assert
            var result = _twillioService.MakeCall(testUrl, testPhoneNumber);
             _callHandlerMock.Verify(x => x.Create(It.IsAny<PhoneNumber>(), It.IsAny<PhoneNumber>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            result.Should().Be(null);
        }
    }
}
