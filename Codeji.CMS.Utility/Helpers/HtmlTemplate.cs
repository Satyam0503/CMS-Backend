namespace Codeji.CMS.Utility.Helpers
{
    public class HtmlTemplate
    {
        public static string Render(string htmlTemplate, object values)
        {
            string output = htmlTemplate;
            foreach (System.Reflection.PropertyInfo p in values.GetType().GetProperties())
                output = output.Replace("[" + p.Name + "]", (p.GetValue(values, null) as string) ?? string.Empty);
            return output;
        }
    }

}
