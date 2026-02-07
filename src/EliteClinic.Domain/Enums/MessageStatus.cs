namespace EliteClinic.Domain.Enums;

public enum MessageStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Delivered = 3,
    Read = 4,
    Failed = 5,
    Retrying = 6
}
