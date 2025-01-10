using System;
namespace Codeji.CMS.Utility.Constraints
{
    public static class ConstraintHelper
    {
        public static readonly HashSet<string> AllowedSanitizerTags = new() { "p", "strong", "em", "u", "i", "s", "h1", "h2", "h3", "h4", "h5", "h6", "ol", "ul", "li", "span", "br" };
        public static readonly HashSet<string> AllowedSanitizerAttributes = new() { "style", "class" };
    }
}

