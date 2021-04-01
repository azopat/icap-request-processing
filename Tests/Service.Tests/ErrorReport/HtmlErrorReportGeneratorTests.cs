using Moq;
using NUnit.Framework;
using Service.ErrorReport;
using System;

namespace Service.Tests.ErrorReport
{
    public class HtmlErrorReportGeneratorTests
    {
        [Test]
        public void Message_Is_Replaced_With_Custom_Message_When_Present()
        {
            // Arrange
            const string ExpectedMessage = "Default Message should be replaced";

            var mockConfig = new Mock<IFileProcessorConfig>();
            mockConfig.SetupGet(s => s.RebuildReportMessage).Returns(ExpectedMessage);

            var reportGenerator = new HtmlErrorReportGenerator(mockConfig.Object);

            // Act
            var result = reportGenerator.CreateReport("fileId");

            // Assert
            Assert.That(result, Contains.Substring(ExpectedMessage));
        }

        [Test]
        public void Message_Is_Default_When_Custom_Message_Is_Not_Present()
        {
            // Arrange
            const string DefaultMessage = "The file does not comply with the current policy";

            var mockConfig = new Mock<IFileProcessorConfig>();
            mockConfig.SetupGet(s => s.RebuildReportMessage).Returns((string)null);

            var reportGenerator = new HtmlErrorReportGenerator(mockConfig.Object);

            // Act
            var result = reportGenerator.CreateReport("fileId");

            // Assert
            Assert.That(result, Contains.Substring(DefaultMessage));
        }

        [Test]
        public void FileId_Is_Added_To_The_Report()
        {
            // Arrange
            var expectedFileId = Guid.NewGuid().ToString();

            var mockConfig = new Mock<IFileProcessorConfig>();

            var reportGenerator = new HtmlErrorReportGenerator(mockConfig.Object);

            // Act
            var result = reportGenerator.CreateReport(expectedFileId);

            // Assert
            Assert.That(result, Contains.Substring(expectedFileId));
        }
    }
}
