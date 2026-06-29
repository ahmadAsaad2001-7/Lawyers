using FluentAssertions;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using NUnit.Framework;


namespace Lawyers.Tests.Domain;

[TestFixture]
public class ConsultationTests
{
    [Test]
    public void Cancel_WhenStatusIsCompleted_ShouldThrowException()
    {
        // Arrange
        var consultation = new Consultation 
        { 
            Status = ConsultationStatus.Completed 
        };

        // Act
        var act = () => consultation.Cancel();

        // Assert (Using FluentAssertions for readability)
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel a completed consultation.");
    }

    [Test]
    public void MarkAsCompleted_WhenStatusIsInProgress_ShouldChangeStatus()
    {
        // Arrange
        var consultation = new Consultation 
        { 
            Status = ConsultationStatus.InProgress 
        };

        // Act
        consultation.MarkAsCompleted();

        // Assert
        consultation.Status.Should().Be(ConsultationStatus.Completed);
    }

    [Test]
    public void MarkAsCompleted_WhenStatusIsPending_ShouldThrowException()
    {
        // Arrange
        var consultation = new Consultation 
        { 
            Status = ConsultationStatus.Pending 
        };

        // Act
        Action act = () => consultation.Cancel();
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only in-progress consultations can be completed.");
    }
}