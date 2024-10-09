using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace GetUserDataServiceGUI
{
  [DebuggerNonUserCode]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
  [CompilerGenerated]
  internal class Resource1
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    internal Resource1()
    {
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) Resource1.resourceMan, (object) null))
          Resource1.resourceMan = new ResourceManager("getUserDataService.Resource1", typeof (Resource1).Assembly);
        return Resource1.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get => Resource1.resourceCulture;
      set => Resource1.resourceCulture = value;
    }

    internal static Bitmap refresh => (Bitmap) Resource1.ResourceManager.GetObject(nameof (refresh), Resource1.resourceCulture);

    internal static Icon serialtoip => (Icon) Resource1.ResourceManager.GetObject(nameof (serialtoip), Resource1.resourceCulture);
  }
}
