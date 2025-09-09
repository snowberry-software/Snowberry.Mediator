using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests.Common;

/// <summary>
/// Base class for mediator tests that provides isolated pipeline execution tracking.
/// Ensures each test has its own tracking context when running in parallel.
/// </summary>
public abstract class MediatorTestBase
{
    protected MediatorTestBase()
    {
        // Initialize isolated tracking contexts for this test
        PipelineExecutionTracker.InitializeContext();
        StreamPipelineExecutionTracker.InitializeContext();
        NotificationHandlerExecutionTracker.InitializeContext();

        // Clear any existing data
        PipelineExecutionTracker.Clear();
        StreamPipelineExecutionTracker.Clear();
        NotificationHandlerExecutionTracker.Clear();

        // Clear static state from generic handlers
        ClearAllGenericHandlerState();
    }

    /// <summary>
    /// Reinitialize tracking contexts. Call this if you need a fresh context within a test.
    /// </summary>
    protected void ResetTracking()
    {
        PipelineExecutionTracker.InitializeContext();
        StreamPipelineExecutionTracker.InitializeContext();
        NotificationHandlerExecutionTracker.InitializeContext();

        PipelineExecutionTracker.Clear();
        StreamPipelineExecutionTracker.Clear();
        NotificationHandlerExecutionTracker.Clear();

        ClearAllGenericHandlerState();
    }

    private void ClearAllGenericHandlerState()
    {
        // Clear generic handler static state to prevent interference
        try
        {
            GenericValidationHandler<SimpleNotification>.ClearValidationResults();
            TestSpecificValidationHandler<SimpleNotification>.ClearValidationResults();
        }
        catch
        {
            // Ignore if types don't exist yet
        }
    }
}