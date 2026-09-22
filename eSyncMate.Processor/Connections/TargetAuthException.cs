namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Raised when Target's stored OAuth credentials cannot produce an access token — the refresh
    /// grant is expired or revoked, the credentials are wrong, or the customer is not set up.
    /// It is kept apart from a plain Exception on purpose: the token belongs to the customer, not to
    /// any one item, so a route must never read this as "this item's data is bad". Every item in the
    /// run would fail the same way, and marking them ERROR would take the whole catalogue out of the
    /// queue (Sp_SCS_GetProductCatalog never picks an ERROR item up again).
    /// </summary>
    public class TargetAuthException : Exception
    {
        public TargetAuthException(string message) : base(message)
        {
        }
    }
}
