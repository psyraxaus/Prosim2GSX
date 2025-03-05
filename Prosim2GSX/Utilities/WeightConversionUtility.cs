namespace Prosim2GSX.Utilities
{
    /// <summary>
    /// Utility class for weight conversions
    /// </summary>
    public static class WeightConversionUtility
    {
        /// <summary>
        /// Conversion factor from kilograms to pounds
        /// </summary>
        public const float KgToLbsConversionFactor = 2.205f;
        
        /// <summary>
        /// Converts kilograms to pounds
        /// </summary>
        /// <param name="kg">Weight in kilograms</param>
        /// <returns>Weight in pounds</returns>
        public static double KgToLbs(double kg)
        {
            return kg * KgToLbsConversionFactor;
        }
        
        /// <summary>
        /// Converts pounds to kilograms
        /// </summary>
        /// <param name="lbs">Weight in pounds</param>
        /// <returns>Weight in kilograms</returns>
        public static double LbsToKg(double lbs)
        {
            return lbs / KgToLbsConversionFactor;
        }
    }
}
