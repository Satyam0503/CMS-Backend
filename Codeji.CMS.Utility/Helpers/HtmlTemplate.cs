namespace Codeji.CMS.Utility.Helpers
{
    public class HtmlTemplate
    {
        public static string Render(string htmlTemplate, object values)
        {
            string output = htmlTemplate;
            foreach (System.Reflection.PropertyInfo p in values.GetType().GetProperties())
            {
                object? val = p.GetValue(values, null);
                string replacement = val?.ToString() ?? string.Empty;
                output = output.Replace("[" + p.Name + "]", replacement);
            }
            return output;
        }
    }
}
