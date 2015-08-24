using System.Text;

// This is an egregious kludge
// The DiffMatchPatch code references System.Web to get the URL encode/decode methods
// System.Web is unavailable in WP7, but for our purposes the versions in System.Net will work
// So we're doing some ugly aliasing here to make it work
// Someday when I have the time I may just create proper versioned nuget packages for DiffMatchPatch
// so people won't have to do this sort of thing
namespace System.Web
{
    public class HttpUtility
    {
        public static string UrlEncode(string str, Encoding e)
        {
            return Net.HttpUtility.UrlEncode(str);
        }

        public static string UrlDecode(string str, Encoding e)
        {
            return Net.HttpUtility.UrlDecode(str);
        }
    }
}
