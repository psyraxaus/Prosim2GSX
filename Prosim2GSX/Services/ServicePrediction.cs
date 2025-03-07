using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Represents a service prediction with confidence level
    /// </summary>
    public class ServicePrediction
    {
        /// <summary>
        /// Gets the type of service
        /// </summary>
        public string ServiceType { get; }
        
        /// <summary>
        /// Gets the predicted status
        /// </summary>
        public string PredictedStatus { get; }
        
        /// <summary>
        /// Gets the confidence level (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; }
        
        /// <summary>
        /// Gets the estimated time until execution
        /// </summary>
        public TimeSpan? EstimatedTimeUntilExecution { get; }
        
        /// <summary>
        /// Creates a new instance of ServicePrediction
        /// </summary>
        public ServicePrediction(string serviceType, string predictedStatus, float confidence, TimeSpan? estimatedTimeUntilExecution = null)
        {
            ServiceType = serviceType;
            PredictedStatus = predictedStatus;
            Confidence = confidence;
            EstimatedTimeUntilExecution = estimatedTimeUntilExecution;
        }
    }
}
