namespace AdmissionEngine.API.Models.Enums
{
    public enum AdmissionStatus
    {
        Pending = 0,
        AdmittedPrimary = 1,
        OfferedAlternative = 2,
        OfferAccepted = 3,
        OfferDeclined = 4,
        Rejected = 5
    }
}