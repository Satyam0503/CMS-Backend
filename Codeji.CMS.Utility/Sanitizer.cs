using System;
using System.Reflection;
using Ganss.Xss;

namespace Codeji.CMS.Utility
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SanitizeAttribute : Attribute { }

    public class Sanitizer
    {
        public static void SanitizeProperties(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            IEnumerable<PropertyInfo> properties = obj.GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(SanitizeAttribute)) && prop.CanWrite);
            foreach (PropertyInfo prop in properties)
            {
                try
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        SanitizeStringProperty(obj, prop);
                    }
                    else if (prop.PropertyType.IsGenericType && prop.PropertyType == typeof(List<string>))
                    {
                        SanitizeListProperty(obj, prop);
                    }
                }
                catch (Exception exception)
                {
                    // Do not allow an object with partially sanitized content to continue
                    // through a request. The caller's exception middleware can log this
                    // safely without including the source value.
                    throw new InvalidOperationException(
                        $"Unable to sanitize property '{prop.Name}' on '{obj.GetType().Name}'.",
                        exception);
                }
            }
        }

        private static void SanitizeStringProperty(object obj, PropertyInfo prop)
        {
            string value = (string)prop.GetValue(obj);
            if (!string.IsNullOrEmpty(value))
            {
                prop.SetValue(obj, EncodingHtmlText(value));
            }
        }

        private static void SanitizeListProperty(object obj, PropertyInfo prop)
        {
            List<string> list = (List<string>)prop.GetValue(obj);
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = EncodingHtmlText(list[i]);
                }
            }
        }

        public static string EncodingHtmlText(string text)
        {
            HtmlSanitizer sanitizer = new();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(Constraints.ConstraintHelper.AllowedSanitizerTags);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(Constraints.ConstraintHelper.AllowedSanitizerAttributes);
            return sanitizer.Sanitize(text, "httpS://www.codeji.in");
        }
    }
}

