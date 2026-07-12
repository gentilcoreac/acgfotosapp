using Microsoft.AspNetCore.Identity;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Localization.PublicResources.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcgFotos.Core.Security.Culture {
    public class SpanishIdentityErrorDescriber : IdentityErrorDescriber {

        public override IdentityError DefaultError() { return new IdentityError { Code = nameof(DefaultError), Description = MessagesAPI.ErrorGeneric }; }
        public override IdentityError ConcurrencyFailure() { return new IdentityError { Code = nameof(ConcurrencyFailure), Description = MessagesAPI.ErrorObjectHasBeenUpdated }; }
        public override IdentityError PasswordMismatch() { return new IdentityError { Code = nameof(PasswordMismatch), Description = MessagesAPI.ErrorPasswordIncorrect }; }
        public override IdentityError InvalidToken() { return new IdentityError { Code = nameof(InvalidToken), Description = MessagesAPI.ErrorCodeInvalid }; }
        public override IdentityError LoginAlreadyAssociated() { return new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = MessagesAPI.ErrorUserNameExist }; }
        public override IdentityError InvalidUserName(string userName) { return new IdentityError { Code = nameof(InvalidUserName), Description = string.Format(MessagesAPI.ErrorUserNameInvalid, userName)}; }
        public override IdentityError InvalidEmail(string email) { return new IdentityError { Code = nameof(InvalidEmail), Description = string.Format(MessagesAPI.ErrorEmailIncorrect, email) }; }
        public override IdentityError DuplicateUserName(string userName) { return new IdentityError { Code = nameof(DuplicateUserName), Description = string.Format(MessagesAPI.ErrorUserNameExistUseNewOne, userName) }; }
        public override IdentityError DuplicateEmail(string email) { return new IdentityError { Code = nameof(DuplicateEmail), Description = string.Format(MessagesAPI.ErrorEmailCurrentRegistered, email) }; }
        public override IdentityError InvalidRoleName(string role) { return new IdentityError { Code = nameof(InvalidRoleName), Description = string.Format(MessagesAPI.ErrorRoleNameInvalid, role) }; }
        public override IdentityError DuplicateRoleName(string role) { return new IdentityError { Code = nameof(DuplicateRoleName), Description = string.Format(MessagesAPI.ErrorRoleNameExists, role) }; }
        public override IdentityError UserAlreadyHasPassword() { return new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = MessagesAPI.ErrorUserHavePassword }; }
        public override IdentityError UserLockoutNotEnabled() { return new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = MessagesAPI.ErrorUserBlockingNotEnable }; }
        public override IdentityError UserAlreadyInRole(string role) { return new IdentityError { Code = nameof(UserAlreadyInRole), Description = string.Format(MessagesAPI.ErrorUserBelongsRole, role) }; }
        public override IdentityError UserNotInRole(string role) { return new IdentityError { Code = nameof(UserNotInRole), Description = string.Format(MessagesAPI.ErrorUserNotBelongsRole, role) }; }
        public override IdentityError PasswordTooShort(int length) { return new IdentityError { Code = nameof(PasswordTooShort), Description = string.Format(MessagesAPI.ErrorPasswordMinLength2, length) }; }
        public override IdentityError PasswordRequiresNonAlphanumeric() { return new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = MessagesAPI.ErrorPasswordMustHaveAnNonAlphanumericCharacter}; }
        public override IdentityError PasswordRequiresDigit() { return new IdentityError { Code = nameof(PasswordRequiresDigit), Description = MessagesAPI.ErrorPasswordMustHaveDigits }; }
        public override IdentityError PasswordRequiresLower() { return new IdentityError { Code = nameof(PasswordRequiresLower), Description = MessagesAPI.ErrorPassworMustHaveLowercaseCharacter }; }
        public override IdentityError PasswordRequiresUpper() { return new IdentityError { Code = nameof(PasswordRequiresUpper), Description = MessagesAPI.ErrorPassworMustHaveUppercaseCharacter }; }
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) { return new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = MessagesAPI.ErrorPassworMustHaveDiferentsCharacters }; }
    }
}
