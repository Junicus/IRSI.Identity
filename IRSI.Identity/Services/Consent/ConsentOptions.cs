using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IRSI.Identity.Services.Consent
{
    public class ConsentOptions
    {
        public static bool EnableOfflineAccess = true;
        public static string OfflineAccessDisplayName = "Offline Access";
        public static string OfflineAccessDescription = "Access to your application and resources, even when you are offline";

        public static readonly string MustChooseOneErrorMessage = "You most pick at least one permission";
        public static readonly string InvalidSelectionErrorMessage = "Invalid selection";
    }
}
