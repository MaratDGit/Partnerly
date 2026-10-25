using Partnerly.Models;

namespace Partnerly.Descriptors.Messages
{
    public class Constants
    {
        public const string SuperReferralCode = "BRANCH01";
        public const string SuperRoleName = "SUPER";
        public const string SuperUserEmail = "marat.iigservices@gmail.com";
        public const string SuperUserPassword = "MarDan123!";
        public const string SuperUserFirstName = "Marat";
        public const string SuperUserLastName = "Danielyan";
        public const string SuperUserPhone = "+37497111312";
        public const string DefaultUserProfilePhotoPath = "~/assets/img/profiles/default-user.png";

        public const string Admin = "Administrator";
        public const string Employee = "Employee";
        public const string User = "User";

        //public const string View = "View";
        //public const string Update = "Update";
        //public const string Delete = "Delete";
        //public const string Edit = "Edit";
        //public const string DeleteConfirmed = "DeleteConfirmed";
        //public const string Create = "Create";
        //public const string UpdateInline = "UpdateInline";
        //public const string CreateInline = "CreateInline"; 
        //public const string DeleteInline = "DeleteInline"; 

        public const string ClaimTypeRoleId = nameof(Partnerly.Models.User.RoleId);
        public const string ClaimTypeRoleType = "RoleType";
        public const string ClaimTypeFirstName = "FirstName";
        public const string ClaimTypeLastName = "LastName";
        public const string ClaimTypeReffCode = "ReffCode";
        public const string ClaimTypeUserPhotoUrl = "UserPhotoUrl";
        public const string ClaimTypeLoginTime = "LoginTime";
    }
}
