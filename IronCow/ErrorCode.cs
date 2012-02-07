
namespace IronCow
{
    public static class ErrorCode
    {
        public const int ReflectionMethodNotFound = 1;

        public const int InvalidSignature = 96;
        public const int MissingSignature = 97;
        public const int LoginFailedOrInvalidAuthToken = 98;
        public const int UserNotLoggedInOrInsufficientPermissions = 99;

        public const int InvalidApiKey = 100;
        public const int InvalidFrob = 101;
        public const int ServiceUnavailable = 105;
        public const int FormatNotFound = 111;
        public const int MethodNotFound = 112;
        public const int InvalidSoapEnvelope = 114;
        public const int InvalidXmlRpcMethodCall = 115;

        public const int TimelineInvalidOrNotProvided = 300;
        public const int TransactionIdInvalidOrNotProvided = 310;
        public const int ListIdInvalidOrNotProvided = 320;
        public const int TaskIdInvalidOrNotProvided = 340;
        public const int NoteIdInvalidOrNotProvided = 350;
        public const int ContactIdInvalidOrNotProvided = 360;
        public const int GroupIdInvalidOrNotProvided = 370;
        public const int LocationIdInvalidOrNotProvided = 380;

        public const int InvalidContact = 1000;
        public const int ContactAlreadyExists = 1010;
        public const int ContactDoesNotExist = 1020;
        public const int CantAddYourselfAsAContact = 1030;

        public const int InvalidGroupName = 2000;
        public const int GroupAlreadyExists = 2010;

        public const int InvalidListName = 3000;
        public const int ListIsLocked = 3010;

        public const int InvalidTaskName = 4000;
        public const int CannotMoveTask = 4010;
    }
}
