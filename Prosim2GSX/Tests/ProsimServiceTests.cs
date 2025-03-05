using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using System;

namespace Prosim2GSX.Tests
{
    [TestClass]
    public class ProsimServiceTests
    {
        private Mock<ServiceModel> _mockModel;
        private ProsimService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockModel = new Mock<ServiceModel>();
            _mockModel.Setup(m => m.ProsimHostname).Returns("localhost");
            
            _service = new ProsimService(_mockModel.Object);
        }

        [TestMethod]
        public void Constructor_WithNullModel_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new ProsimService(null));
        }

        [TestMethod]
        public void Connect_RaisesConnectionChangedEvent()
        {
            bool eventRaised = false;
            bool connectionStatus = false;
            string connectionMessage = string.Empty;

            _service.ConnectionChanged += (sender, args) =>
            {
                eventRaised = true;
                connectionStatus = args.IsConnected;
                connectionMessage = args.Message;
            };

            try
            {
                _service.Connect("localhost");
            }
            catch
            {
                // Connection will likely fail in test environment, but we're testing the event
            }

            Assert.IsTrue(eventRaised, "ConnectionChanged event should be raised");
        }
    }
}
