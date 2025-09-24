namespace Codeji.CMS.API.App_Start
{
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
  public class ModulePermissionAttribute : Attribute
  {
    public string Module { get; }
    public string[] Permissions { get; }

    public ModulePermissionAttribute(string module, params string[] permissions)
    {
      Module = module;
      Permissions = permissions;
    }
  }
}