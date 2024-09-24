using Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Extensions
{
    public static class AccessibilityExtensions
    {
        public static string ToAccessibilityString(this TypeAccessibilityLevel accessibility)
        {
            return accessibility switch
            {
                TypeAccessibilityLevel.Public => "public",
                TypeAccessibilityLevel.Internal => "internal",
                TypeAccessibilityLevel.Private => "private",
                _ => "unknown"
            };
        }

        public static string ToAccessibilityString(this MemberAccessibilityLevel accessibility)
        {
            return accessibility switch
            {
                MemberAccessibilityLevel.Public => "public",
                MemberAccessibilityLevel.Private => "private",
                MemberAccessibilityLevel.Protected => "protected",
                MemberAccessibilityLevel.Internal => "internal",
                MemberAccessibilityLevel.ProtectedInternal => "protected internal",
                MemberAccessibilityLevel.PrivateProtected => "private protected",
                _ => "unknown"
            };
        }
    }
}
