namespace TradeImportsQuantityMgmt.Exceptions
{
    public class QuantityReleaseFailureException(string mrn, string ched)
        : Exception($"Failed to release quantity items for MRN {mrn} and Ched {ched}")
    {
        public string Mrn => mrn;

        public string Ched => ched;
    }
}
