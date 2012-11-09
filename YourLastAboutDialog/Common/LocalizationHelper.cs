using System;
using System.Globalization;
using System.IO;

namespace YourLastAboutDialog.Common
{
    /// <summary>
    /// A helper class for localization issues.
    /// </summary>
    internal static class LocalizationHelper
    {
        /// <summary>
        /// Gets the country code localized URI. Simply splits the file name part of the uri
        /// and adds the current culture name to it, e.g. http://localhost/file.ext
        /// and a current culture of "de-DE" is turned into http://localhost/file.de-DE.ext 
        /// </summary>
        /// <param name="uri">The URI to localize.</param>
        /// <returns>The URI, extended by the current culture name between the file name and extension.</returns>
        public static string GetCultureNameLocalizedUri(string uri)
        {
            try
            {
                // get the current culture
                var culture = CultureInfo.CurrentUICulture;

                var result = GetLocalizedUri(uri, culture.Name);
                return result;
            }
            catch (Exception)
            {
                // if something bad happens, simply return the original uri
                return uri;
            }
        }

        /// <summary>
        /// Gets the language code localized URI. Simply splits the file name part of the uri 
        /// and adds the current two-letter ISO language name to it, e.g. http://localhost/file.ext
        /// and a current culture of "de-DE" is turned into http://localhost/file.de.ext
        /// </summary>
        /// <param name="uri">The URI to localize.</param>
        /// <returns>The URI, extended by the current two-letter ISO language name between the file name and extension.</returns>
        public static string GetLanguageCodeLocalizedUri(string uri)
        {
            try
            {
                // get the current language
                var culture = CultureInfo.CurrentUICulture;
                var language = culture.TwoLetterISOLanguageName;

                var result = GetLocalizedUri(uri, language);
                return result;
            }
            catch (Exception)
            {
                // if something bad happens, simply return the original uri
                return uri;
            }
        }

        private static string GetLocalizedUri(string uri, string toInsert)
        {
            // split uri
            var directory = Path.GetDirectoryName(uri);
            var filename = Path.GetFileNameWithoutExtension(uri);
            var extension = Path.GetExtension(uri);

            // build new uri
            var newFilename = string.Format("{0}.{1}{2}", filename, toInsert, extension);
            var newUri = Path.Combine(directory ?? string.Empty, newFilename);

            return newUri;
        }
    }
}
