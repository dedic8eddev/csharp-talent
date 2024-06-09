namespace Ikiru.Parsnips.Application.Command.Users.Models
{
    public enum UserActiveInActiveStatusEnum
    {
        UserToChangeStatusDoesNotExist,
        NoAvailableLicenses,
        IncorrectPermission,
        UnableToDisableOwnAccount,
        UnableToDisableThereMustBeGreaterThenOneOwnerAccount,
        InternalError,
        IsActive,
        IsInActive
    }
}
