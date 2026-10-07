namespace GooseWebsite.Api.Modules.Contact.Models;

/// <summary>How far a delivery attempt got, so a retry can resume instead of repeating a step.</summary>
public enum ContactDeliveryOutcome
{
    /// <summary>Nothing reached Gustavo.</summary>
    Failed,

    /// <summary>Gustavo was notified but the sender's receipt failed.</summary>
    OwnerNotifiedReceiptFailed,

    /// <summary>Both emails were accepted.</summary>
    Delivered
}
