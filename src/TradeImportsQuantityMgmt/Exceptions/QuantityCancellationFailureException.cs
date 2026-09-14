namespace TradeImportsQuantityMgmt.Exceptions
{
    public class QuantityCancellationFailureException(string mrn, string ched)
        : Exception($"Failed to cancel quantity items for MRN {mrn} and Ched {ched}")
    {
        public string Mrn => mrn;

        public string Ched => ched;
    }
}
